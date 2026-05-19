using TriviaBackend.Models.Enums;

namespace TriviaBackend.Models.Records
{
    public record TriviaQuestionDTO(
        string QuestionText,
        string Answer1,
        string Answer2,
        string Answer3,
        string Answer4,
        int CorrectAnswerIndex,
        QuestionCategory Category,
        DifficultyLevel Difficulty,
        int TimeLimit = 30
    );
}
