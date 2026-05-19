namespace TriviaBackend.Models.Records.Leaderboard
{
    public record LeaderboardStatistics(
        double AveragePoints,
        int TotalPoints,
        string? TopPlayer,
        int PlayerCount
    );
}
