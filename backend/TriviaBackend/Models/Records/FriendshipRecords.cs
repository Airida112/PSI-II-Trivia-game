using TriviaBackend.Models.Enums;

namespace TriviaBackend.Models.Records
{
    /// <summary>
    /// Represents a friend entry in the friend list with live status info
    /// </summary>
    public record FriendEntry(
        string UserId,
        string Username,
        PlayerStatus Status,
        string? ActiveGameId
    );

    /// <summary>
    /// Represents a pending friendship request shown to the addressee
    /// </summary>
    public record FriendRequestEntry(
        int FriendshipId,
        string RequesterId,
        string RequesterUsername,
        DateTime SentAt
    );

    /// <summary>
    /// Payload sent by the client to send a friend request
    /// </summary>
    public record SendFriendRequestDTO(
        string RequesterId,
        string AddresseeUsername
    );

    /// <summary>
    /// Payload sent by the client to respond to a friend request
    /// </summary>
    public record RespondFriendRequestDTO(
        string AddresseeId,
        int FriendshipId,
        bool Accept
    );

    /// <summary>
    /// Payload for a game invite sent over SignalR
    /// </summary>
    public record GameInvitePayload(
        string InviterUsername,
        string GameId
    );
}
