using TriviaBackend.Models.Entities;
using TriviaBackend.Models.Enums;

namespace TriviaBackend.Services.Interfaces
{
    public interface IQuestionService
    {
        List<TriviaQuestion> GetQuestions(QuestionCategory[]? categories, DifficultyLevel? maxDifficulty, int count);
        Dictionary<QuestionCategory, int> GetQuestionCountByCategory();

    }
}
