using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using TriviaBackend.Hubs;
using TriviaBackend.Models.Records;
using TriviaBackend.Services.Interfaces;
using TriviaBackend.Services.Interfaces.DB;

namespace TriviaBackend.Controllers
{
    /// <summary>
    /// HTTP endpoints for the friendship system.
    /// All mutating operations identify the acting user via a userId query/body
    /// parameter (swap this for ClaimTypes.NameIdentifier once auth middleware
    /// is wired up to the controllers).
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class FriendshipController(
        IFriendshipService _friendshipService,
        IPresenceService _presenceService,
        IHubContext<GameHub> _hubContext) : ControllerBase
    {

        /// <summary>Get accepted friends with live status for the requesting user.</summary>
        [HttpGet("friends/{userId}")]
        public async Task<ActionResult<List<FriendEntry>>> GetFriends(string userId)
        {
            var friends = await _friendshipService.GetFriendsAsync(userId);
            return Ok(friends);
        }

        /// <summary>Get all pending friend requests addressed to the given user.</summary>
        [HttpGet("requests/{userId}")]
        public async Task<ActionResult<List<FriendRequestEntry>>> GetPendingRequests(string userId)
        {
            var requests = await _friendshipService.GetPendingRequestsAsync(userId);
            return Ok(requests);
        }

        /// <summary>Get all pending friend requests created by the given user.</summary>
        [HttpGet("outgoing/{userId}")]
        public async Task<ActionResult<List<FriendRequestEntry>>> GetOutgoingRequests(string userId)
        {
            var requests = await _friendshipService.GetOutgoingRequestsAsync(userId);
            return Ok(requests);
        }

        /// <summary>Get the current friendship status between a user and a target username.</summary>
        [HttpGet("status/{userId}/{targetUsername}")]
        public async Task<ActionResult<FriendRelationshipEntry>> GetRelationshipStatus(string userId, string targetUsername)
        {
            var relationship = await _friendshipService.GetRelationshipStatusAsync(userId, targetUsername);
            return Ok(relationship);
        }

        /// <summary>
        /// Refreshes a user's presence heartbeat while the app is open.
        /// </summary>
        [HttpPost("presence/ping")]
        public ActionResult PingPresence([FromBody] UserPresenceDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.UserId))
                return BadRequest("UserId is required.");

            _presenceService.Touch(dto.UserId);
            return Ok();
        }

        /// <summary>
        /// Forces a user's presence offline and notifies their friends.
        /// </summary>
        [HttpPost("presence/logout")]
        public async Task<ActionResult> LogoutPresence([FromBody] UserPresenceDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.UserId))
                return BadRequest("UserId is required.");

            _presenceService.ForceOffline(dto.UserId);

            var friends = await _friendshipService.GetFriendsAsync(dto.UserId);
            foreach (var friend in friends)
            {
                await _hubContext.Clients.User(friend.UserId)
                    .SendAsync("FriendStatusChanged", new
                    {
                        userId = dto.UserId,
                        status = "Offline",
                        gameId = (string?)null
                    });
            }

            return Ok();
        }

       

        /// <summary>
        /// Send a friend request.
        /// Body: { requesterId, addresseeUsername }
        /// </summary>
        [HttpPost("send")]
        public async Task<ActionResult> SendRequest([FromBody] SendFriendRequestDTO dto)
        {
            var friendship = await _friendshipService.SendRequestAsync(dto.RequesterId, dto.AddresseeUsername);

            if (friendship == null)
                return Conflict("Friend request could not be sent. The user may not exist, you may already be friends, or a request is already pending.");

           
            await _hubContext.Clients.User(friendship.AddresseeId)
                .SendAsync("FriendRequestReceived", new
                {
                    friendshipId = friendship.Id,
                    requesterId = friendship.RequesterId
                });

            return Ok(new { friendshipId = friendship.Id });
        }

        /// <summary>
        /// Accept a pending friend request.
        /// Body: { addresseeId, friendshipId, accept: true }
        /// </summary>
        [HttpPost("respond")]
        public async Task<ActionResult> RespondToRequest([FromBody] RespondFriendRequestDTO dto)
        {
            var pendingRequest = (await _friendshipService.GetPendingRequestsAsync(dto.AddresseeId))
                .FirstOrDefault(r => r.FriendshipId == dto.FriendshipId);

            if (dto.Accept)
            {
                var ok = await _friendshipService.AcceptRequestAsync(dto.AddresseeId, dto.FriendshipId);
                if (!ok)
                    return BadRequest("Could not accept the request. It may not exist or you are not the addressee.");

                
                await _hubContext.Clients.User(dto.AddresseeId)
                    .SendAsync("FriendRequestAccepted", new { friendshipId = dto.FriendshipId });

                if (pendingRequest != null)
                {
                    await _hubContext.Clients.User(pendingRequest.RequesterId)
                        .SendAsync("FriendRequestResponded", new { friendshipId = dto.FriendshipId, accepted = true });
                }

                return Ok("Friend request accepted.");
            }
            else
            {
                var ok = await _friendshipService.DeclineRequestAsync(dto.AddresseeId, dto.FriendshipId);
                if (!ok)
                    return BadRequest("Could not decline the request. It may not exist or you are not the addressee.");

                if (pendingRequest != null)
                {
                    await _hubContext.Clients.User(pendingRequest.RequesterId)
                        .SendAsync("FriendRequestResponded", new { friendshipId = dto.FriendshipId, accepted = false });
                }

                return Ok("Friend request declined.");
            }
        }

        /// <summary>
        /// Remove an accepted friendship.
        /// </summary>
        [HttpDelete("remove")]
        public async Task<ActionResult> RemoveFriend([FromQuery] string userId, [FromQuery] string friendId)
        {
            var ok = await _friendshipService.RemoveFriendAsync(userId, friendId);
            if (!ok)
                return NotFound("Friendship not found.");

            return NoContent();
        }

        

        /// <summary>
        /// Invite an online friend to an active game lobby.
        /// The friend must be Online (not InGame) for the invite to be sent.
        /// </summary>
        [HttpPost("invite")]
        public async Task<ActionResult> InviteToGame(
            [FromQuery] string inviterId,
            [FromQuery] string friendId,
            [FromQuery] string gameId)
        {
            
            var friends = await _friendshipService.GetFriendsAsync(inviterId);
            var target = friends.FirstOrDefault(f => f.UserId == friendId);

            if (target == null)
                return BadRequest("You are not friends with this user.");

            if (target.Status != Models.Enums.PlayerStatus.Online)
                return BadRequest("Friend is not online or is already in a game.");

            
            await _hubContext.Clients.User(friendId)
                .SendAsync("GameInviteReceived", new GameInvitePayload(inviterId, gameId));

            return Ok("Invite sent.");
        }
    }
}
