using System.Collections.Concurrent;
using TriviaBackend.Models.Enums;
using TriviaBackend.Services.Interfaces;

namespace TriviaBackend.Services.Implementations
{
    /// <summary>
    /// Thread-safe, in-memory presence tracker registered as a Singleton.
    /// Supports multiple concurrent connections per user (e.g. two browser tabs).
    /// </summary>
    public class PresenceService : IPresenceService
    {
        private readonly ConcurrentDictionary<string, HashSet<string>> _userConnections = new();

        private readonly ConcurrentDictionary<string, string> _connectionUser = new();

        private readonly ConcurrentDictionary<string, string> _userGame = new();

        private readonly object _lock = new();

        public void SetOnline(string userId, string connectionId)
        {
            lock (_lock)
            {
                _userConnections.AddOrUpdate(
                    userId,
                    _ => new HashSet<string> { connectionId },
                    (_, existing) => { existing.Add(connectionId); return existing; }
                );
                _connectionUser[connectionId] = userId;
            }
        }

        public void SetOffline(string connectionId)
        {
            lock (_lock)
            {
                if (!_connectionUser.TryRemove(connectionId, out var userId))
                    return;

                if (_userConnections.TryGetValue(userId, out var conns))
                {
                    conns.Remove(connectionId);

                    if (conns.Count == 0)
                    {
                        _userConnections.TryRemove(userId, out _);
                        _userGame.TryRemove(userId, out _);
                    }
                }
            }
        }

        public void SetInGame(string userId, string gameId)
        {
            _userGame[userId] = gameId;
        }

        public void ClearGame(string userId)
        {
            _userGame.TryRemove(userId, out _);
        }

        public PlayerStatus GetStatus(string userId)
        {
            if (!_userConnections.TryGetValue(userId, out var conns) || conns.Count == 0)
                return PlayerStatus.Offline;

            return _userGame.ContainsKey(userId) ? PlayerStatus.InGame : PlayerStatus.Online;
        }

        public string? GetActiveGameId(string userId)
        {
            _userGame.TryGetValue(userId, out var gameId);
            return gameId;
        }

        public string? GetUserIdByConnection(string connectionId)
        {
            _connectionUser.TryGetValue(connectionId, out var userId);
            return userId;
        }
    }
}
