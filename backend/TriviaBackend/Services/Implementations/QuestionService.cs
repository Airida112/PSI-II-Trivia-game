using TriviaBackend.Data;
using TriviaBackend.Models.Entities;
using TriviaBackend.Models.Enums;
using TriviaBackend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace TriviaBackend.Services.Implementations
{
    /// <summary>
    /// Service for working with questions in the ongoing trivia match 
    /// </summary>
    public class QuestionService : IQuestionService
    {
        private readonly IServiceProvider _serviceProvider;

        public QuestionService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Get filtered questions
        /// </summary>
        /// <param name="categories"></param>
        /// <param name="maxDifficulty"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public List<TriviaQuestion> GetQuestions(QuestionCategory[]? categories = null,
                                               DifficultyLevel? maxDifficulty = null,
                                               int count = 6)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ITriviaDbContext>();

            var query = dbContext.Questions.AsEnumerable();

            if (categories != null && categories.Length > 0)
            {
                query = query.Where(q => categories.Contains(q.Category));
            }

            if (maxDifficulty.HasValue)
            {
                query = query.Where(q => q.Difficulty <= maxDifficulty.Value);
            }

            return query.OrderBy(q => Guid.NewGuid())
                       .Take(count)
                       .ToList();
        }

        /// <summary>
        /// Get the amount of questions that have the category in question
        /// </summary>
        /// <returns></returns>
        public Dictionary<QuestionCategory, int> GetQuestionCountByCategory()
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ITriviaDbContext>();

            var categoryCounts = new Dictionary<QuestionCategory, int>();

            foreach (var category in Enum.GetValues<QuestionCategory>())
            {
                categoryCounts[category] = 0;
            }

            var query = dbContext.Questions.ToList();

            foreach (var question in query)
            {
                categoryCounts[question.Category]++;
            }

            return categoryCounts;
        }
    }
}