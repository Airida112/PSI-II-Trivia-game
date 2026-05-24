using TriviaBackend.Models.Enums;

namespace TriviaBackend.Services.Interfaces
{
    /// <summary>
    /// Tracks which users are connected via SignalR and whether they are
    /// currently inside an active game lobby.
    /// This is an in-memory service; no persistence is needed between restarts.
    /// </summary>
    public interface IPresenceService
    {
        /// <summary>Mark a user as online (connected to the hub).</summary>
        void SetOnline(string userId, string connectionId);

        /// <summary>Mark a user as offline and remove all their connection ids.</summary>
        void SetOffline(string connectionId);

        /// <summary>Mark a user as offline and remove all tracked presence for them.</summary>
        void ForceOffline(string userId);

        /// <summary>Associate a user with an active game (sets status to InGame).</summary>
        void SetInGame(string userId, string gameId);

        /// <summary>Remove the in-game association for a user (reverts to Online).</summary>
        void ClearGame(string userId);

        /// <summary>Refresh the user's last-seen timestamp while the client is active.</summary>
        void Touch(string userId);

        /// <summary>Get the current status of a user.</summary>
        PlayerStatus GetStatus(string userId);

        /// <summary>Get the game id the user is currently in, or null.</summary>
        string? GetActiveGameId(string userId);

        /// <summary>Resolve a connectionId to the userId that owns it.</summary>
        string? GetUserIdByConnection(string connectionId);
    }
}
