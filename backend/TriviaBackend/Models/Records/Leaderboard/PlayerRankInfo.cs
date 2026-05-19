namespace TriviaBackend.Models.Records.Leaderboard
{
    public record PlayerRankInfo(
        string Username,
        int Rank,
        int Elo,
        int GamesPlayed,
        int TotalPlayers
    );
}
