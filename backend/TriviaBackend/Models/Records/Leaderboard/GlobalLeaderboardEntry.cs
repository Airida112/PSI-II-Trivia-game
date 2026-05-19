namespace TriviaBackend.Models.Records.Leaderboard
{
    public record GlobalLeaderboardEntry(
        int Rank,
        string Username,
        int Elo,
        int TotalPoints,
        int GamesPlayed,
        DateTime JoinDate
    );
}
