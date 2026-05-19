using Microsoft.EntityFrameworkCore;
using TriviaBackend.Data;
using TriviaBackend.Models.Entities;
using TriviaBackend.Models.Enums;
using TriviaBackend.Services.Implementations;
using Microsoft.Extensions.DependencyInjection;

namespace TriviaBackendTests.Integration
{
    [TestFixture]
    public class QuestionsDbTests
    {
        private ServiceProvider _provider = null!;
        private IServiceScope _scope = null!;
        private TriviaDbContext _context = null!;
        private QuestionService _service = null!;

        [SetUp]
        public void Setup()
        {
            var services = new ServiceCollection();

            // Unique DB name for each test
            var dbName = "TestDb_" + Guid.NewGuid();
            services.AddDbContext<TriviaDbContext>(options =>
                options.UseInMemoryDatabase(dbName));

            services.AddScoped<ITriviaDbContext, TriviaDbContext>();
            services.AddScoped<QuestionService>();

            _provider = services.BuildServiceProvider();

            _scope = _provider.CreateScope();
            _context = _scope.ServiceProvider.GetRequiredService<TriviaDbContext>();
            _service = _scope.ServiceProvider.GetRequiredService<QuestionService>();
        }

        [TearDown]
        public void TearDown()
        {
            _context.Dispose();
            _scope.Dispose();
            _provider.Dispose();
        }

        [Test]
        public async Task AddQuestion_ThenRetrieve_ShouldReturnSame()
        {
            _context.Questions.Add(
                new TriviaQuestion { Id = 1, Category = QuestionCategory.History }
            );

            await _context.SaveChangesAsync();

            var result = _service.GetQuestionCountByCategory();

            Assert.That(result[QuestionCategory.History], Is.EqualTo(1));
        }
    }
}