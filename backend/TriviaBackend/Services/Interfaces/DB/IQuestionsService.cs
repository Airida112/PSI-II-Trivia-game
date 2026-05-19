using TriviaBackend.Models.Entities;
using TriviaBackend.Models.Records;

namespace TriviaBackend.Services.Interfaces.DB
{
    public interface IQuestionsService
    {
        Task<TriviaQuestion?> GetQuestionByIdAsync(int id);

        Task<TriviaQuestion> AddQuestionAsync(TriviaQuestionDTO question);

        Task DeleteQuestionByIdAsync(int id);

    }
}
