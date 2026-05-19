using TriviaBackend.Models.Enums;

namespace TriviaBackend.Models.Entities
{
    public struct GameSettings(int MaxPlayers = 10, int QuestionsPerGame = 10, int DefaultTimeLimit = 30)
    {
        public int MaxPlayers { get; set; } = MaxPlayers;
        public int QuestionsPerGame { get; set; } = QuestionsPerGame;
        public int DefaultTimeLimit { get; set; } = DefaultTimeLimit;
        public bool AllowLateJoining { get; set; } = false;
        public QuestionCategory[] QuestionCategories { get; set; } = Enum.GetValues<QuestionCategory>();
        public DifficultyLevel MaxDifficulty { get; set; } = DifficultyLevel.Hard;

        // team mode settings
        public bool IsTeamMode { get; set; } = false;
        public int NumberOfTeams { get; set; } = 2;
    }
}