using TriviaBackend.Models.Entities;
using TriviaBackend.Models.Records;

namespace TriviaBackend.Services.Interfaces.DB
{
    public interface IFriendshipService
    {
        /// <summary>
        /// Send a friend request from requester to the user with the given username.
        /// Returns the created Friendship or null if it could not be created
        /// (duplicate, self-request, user not found, etc.).
        /// </summary>
        Task<FriendshipRequest?> SendRequestAsync(string requesterId, string addresseeUsername);

        /// <summary>
        /// Accept a pending friend request. Returns false if the friendship
        /// does not exist or the caller is not the addressee.
        /// </summary>
        Task<bool> AcceptRequestAsync(string addresseeId, int friendshipId);

        /// <summary>
        /// Decline (and delete) a pending friend request. Returns false when
        /// the friendship does not exist or the caller is not the addressee.
        /// </summary>
        Task<bool> DeclineRequestAsync(string addresseeId, int friendshipId);

        /// <summary>
        /// Remove an existing accepted friendship.
        /// </summary>
        Task<bool> RemoveFriendAsync(string userId, string friendId);

        /// <summary>
        /// Get all accepted friends for a user together with their current
        /// online/in-game status and active game id (if any).
        /// </summary>
        Task<List<FriendEntry>> GetFriendsAsync(string userId);

        /// <summary>
        /// Get all pending friend requests addressed to the given user.
        /// </summary>
        Task<List<FriendRequestEntry>> GetPendingRequestsAsync(string userId);

        /// <summary>
        /// Get all pending friend requests sent by the given user (outgoing requests).
        /// </summary>
        Task<List<FriendRequestEntry>> GetOutgoingRequestsAsync(string userId);
    }
}
