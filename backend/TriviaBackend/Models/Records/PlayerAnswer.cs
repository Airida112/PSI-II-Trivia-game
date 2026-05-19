using TriviaBackend.Models.Enums;

namespace TriviaBackend.Models.Records
{
    public record PlayerAnswer(
        int QuestionId,
        int SelectedOptionIndex,
        AnswerResult Status,
        int PointsEarned
    );
}
