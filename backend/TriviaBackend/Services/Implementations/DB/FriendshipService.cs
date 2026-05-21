using Microsoft.EntityFrameworkCore;
using TriviaBackend.Data;
using TriviaBackend.Models.Entities;
using TriviaBackend.Models.Enums;
using TriviaBackend.Models.Records;
using TriviaBackend.Services.Interfaces;
using TriviaBackend.Services.Interfaces.DB;

namespace TriviaBackend.Services.Implementations.DB
{
    public class FriendshipService(ITriviaDbContext context, IPresenceService presenceService) : IFriendshipService
    {
        private readonly ITriviaDbContext _context = context;
        private readonly IPresenceService _presence = presenceService;

        public async Task<FriendshipRequest?> SendRequestAsync(string requesterId, string addresseeUsername)
        {
            var requesterExists = await _context.Users.AnyAsync(u => u.Id == requesterId);
            if (!requesterExists)
            {
                throw new KeyNotFoundException($"The requester user with ID '{requesterId}' does not exist.");
            }

            var addressee = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == addresseeUsername);

            if (addressee == null)
                return null;

            if (addressee.Id == requesterId)
                return null;

            var existing = await _context.Friendships
                .FirstOrDefaultAsync(f =>
                    (f.RequesterId == requesterId && f.AddresseeId == addressee.Id) ||
                    (f.RequesterId == addressee.Id && f.AddresseeId == requesterId));

            if (existing != null)
                return null;

            var friendship = new FriendshipRequest
            {
                RequesterId = requesterId,
                AddresseeId = addressee.Id,
                Status = FriendshipStatus.Pending
            };

            _context.Friendships.Add(friendship);
            await _context.SaveChangesAsync();
            return friendship;
        }

        public async Task<bool> AcceptRequestAsync(string addresseeId, int friendshipId)
        {
            var friendship = await _context.Friendships.FindAsync(friendshipId);

            if (friendship == null
                || friendship.AddresseeId != addresseeId
                || friendship.Status != FriendshipStatus.Pending)
                return false;

            friendship.Status = FriendshipStatus.Accepted;
            friendship.RespondedAt = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Utc);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeclineRequestAsync(string addresseeId, int friendshipId)
        {
            var friendship = await _context.Friendships.FindAsync(friendshipId);

            if (friendship == null
                || friendship.AddresseeId != addresseeId
                || friendship.Status != FriendshipStatus.Pending)
                return false;

            _context.Friendships.Remove(friendship);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemoveFriendAsync(string userId, string friendId)
        {
            var friendship = await _context.Friendships
                .FirstOrDefaultAsync(f =>
                    f.Status == FriendshipStatus.Accepted &&
                    ((f.RequesterId == userId && f.AddresseeId == friendId) ||
                     (f.RequesterId == friendId && f.AddresseeId == userId)));

            if (friendship == null)
                return false;

            _context.Friendships.Remove(friendship);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<FriendEntry>> GetFriendsAsync(string userId)
        {
            var friendships = await _context.Friendships
                .Include(f => f.Requester)
                .Include(f => f.Addressee)
                .Where(f =>
                    f.Status == FriendshipStatus.Accepted &&
                    (f.RequesterId == userId || f.AddresseeId == userId))
                .ToListAsync();

            return friendships.Select(f =>
            {
                var friend = f.RequesterId == userId ? f.Addressee! : f.Requester!;
                var status = _presence.GetStatus(friend.Id);
                var activeGameId = status == PlayerStatus.InGame
                    ? _presence.GetActiveGameId(friend.Id)
                    : null;

                return new FriendEntry(friend.Id, friend.Username, status, activeGameId);
            }).ToList();
        }

        public async Task<List<FriendRequestEntry>> GetPendingRequestsAsync(string userId)
        {
            return await _context.Friendships
                .Include(f => f.Requester)
                .Where(f => f.AddresseeId == userId && f.Status == FriendshipStatus.Pending)
                .Select(f => new FriendRequestEntry(
                    f.Id,
                    f.RequesterId,
                    f.Requester!.Username,
                    f.CreatedAt))
                .ToListAsync();
        }

        public async Task<List<FriendRequestEntry>> GetOutgoingRequestsAsync(string userId)
        {
            return await _context.Friendships
                .Include(f => f.Addressee)
                .Where(f => f.RequesterId == userId && f.Status == FriendshipStatus.Pending)
                .Select(f => new FriendRequestEntry(
                    f.Id,
                    f.AddresseeId,
                    f.Addressee!.Username,
                    f.CreatedAt))
                .ToListAsync();
        }
    }
}