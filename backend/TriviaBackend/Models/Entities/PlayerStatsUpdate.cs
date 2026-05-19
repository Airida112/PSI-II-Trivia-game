namespace TriviaBackend.Models.Entities
{
    public struct PlayerStatsUpdate(string Username, int EloChange, int PointsEarned)
    {
        public string Username { get; set; } = Username;
        public int EloChange { get; set; } = EloChange;
        public int PointsEarned { get; set; } = PointsEarned;
    }
}
