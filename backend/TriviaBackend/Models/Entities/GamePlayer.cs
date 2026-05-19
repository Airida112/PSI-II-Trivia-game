namespace TriviaBackend.Models.Entities
{
    public class GamePlayer : IComparable<GamePlayer>
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int CurrentScore { get; set; } = 0;
        public int CorrectAnswers { get; set; } = 0;
        public bool IsActive { get; set; } = true;
        public DateTime JoinedGameAt { get; set; }
        public int CurrentGameScore { get; set; } = 0;
        public int CorrectAnswersInGame { get; set; } = 0;

        public int CompareTo(GamePlayer? other)
        {
            if (other == null) return 1;

            int scoreComparison = other.CurrentGameScore.CompareTo(this.CurrentGameScore);
            if (scoreComparison != 0)
                return scoreComparison;

            int correctAnswersComparison = other.CorrectAnswersInGame.CompareTo(this.CorrectAnswersInGame);
            if (correctAnswersComparison != 0)
                return correctAnswersComparison;

            return this.JoinedGameAt.CompareTo(other.JoinedGameAt);
        }

        public override string ToString()
        {
            return $"{Name} (ID: {Id}) - Score: {CurrentGameScore}, Correct: {CorrectAnswersInGame}";
        }
    }
}