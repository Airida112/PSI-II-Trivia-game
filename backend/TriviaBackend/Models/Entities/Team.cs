namespace TriviaBackend.Models.Entities
{
    public class Team
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<GamePlayer> Members { get; set; } = new();
        public int TotalScore { get; set; } = 0;
        public int CorrectAnswers { get; set; } = 0;

        public void AddMember(GamePlayer player)
        {
            Members.Add(player);
        }

        public void RemoveMember(GamePlayer player)
        {
            Members.Remove(player);
        }

        public void UpdateScore(int points)
        {
            TotalScore += points;
        }
    }
}