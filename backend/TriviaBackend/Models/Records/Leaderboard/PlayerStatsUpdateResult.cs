namespace TriviaBackend.Models.Records.Leaderboard
{
    public record PlayerStatsUpdateResult(
        string Username,
        int NewElo,
        int TotalGames
    );
}
