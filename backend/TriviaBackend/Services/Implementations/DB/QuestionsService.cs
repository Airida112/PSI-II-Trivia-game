using TriviaBackend.Data;
using TriviaBackend.Models.Entities;
using TriviaBackend.Models.Records;
using TriviaBackend.Services.Interfaces.DB;

namespace TriviaBackend.Services.Implementations.DB
{
    public class QuestionsService(ITriviaDbContext context) : IQuestionsService
    {
        private readonly ITriviaDbContext _context = context;

        public async Task<TriviaQuestion?> GetQuestionByIdAsync(int id) =>
            await _context.Questions.FindAsync(id);

        public async Task<TriviaQuestion> AddQuestionAsync(TriviaQuestionDTO question)
        {
            var entity = new TriviaQuestion
            {
                QuestionText = question.QuestionText,
                CorrectAnswerIndex = question.CorrectAnswerIndex,
                AnswerOptions = [question.Answer1, question.Answer2, question.Answer3, question.Answer4],
                Category = question.Category,
                Difficulty = question.Difficulty,
                TimeLimit = question.TimeLimit
            };

            _context.Questions.Add(entity);
            await _context.SaveChangesAsync();

            return entity;
        }

        public async Task DeleteQuestionByIdAsync(int id)
        {
            var question = await _context.Questions.FindAsync(id);
            if (question != null)
            {
                _context.Questions.Remove(question);
                await _context.SaveChangesAsync();
            }
        }
    }
}
