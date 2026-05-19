using Moq;
using TriviaBackend.Models.Entities;
using TriviaBackend.Models.Enums;
using TriviaBackend.Services.Implementations;
using TriviaBackend.Data;
using Microsoft.Extensions.DependencyInjection;
using TriviaBackendTests.TestEnvSetup;

namespace TriviaBackendTests.Unit
{
    [TestFixture]
    public class QuestionServiceTests
    {
        private Mock<IServiceProvider> _providerMock = null!;
        private Mock<IServiceScopeFactory> _scopeFactoryMock = null!;
        private Mock<IServiceScope> _scopeMock = null!;
        private Mock<IServiceProvider> _scopedProviderMock = null!;

        private QuestionService _service = null!;
        private List<TriviaQuestion> _questions = null!;

        [SetUp]
        public void Setup()
        {
            _questions =
            [
                new() { Id = 1, Category = QuestionCategory.Geography, Difficulty = DifficultyLevel.Easy },
                new() { Id = 2, Category = QuestionCategory.Science, Difficulty = DifficultyLevel.Hard },
                new() { Id = 3, Category = QuestionCategory.Geography, Difficulty = DifficultyLevel.Medium }
            ];

            //create IServiceScope then inside the scope mock ITriviaDbContext with DbSet<TriviaQuestion>
            var mockDbSet = MockDbSet.CreateMockDbSet(_questions);

            var dbContextMock = new Mock<ITriviaDbContext>();
            dbContextMock.Setup(x => x.Questions).Returns(mockDbSet.Object);

            _scopedProviderMock = new Mock<IServiceProvider>();
            _scopedProviderMock
                .Setup(sp => sp.GetService(typeof(ITriviaDbContext)))
                .Returns(dbContextMock.Object);

            _scopeMock = new Mock<IServiceScope>();
            _scopeMock
                .Setup(s => s.ServiceProvider)
                .Returns(_scopedProviderMock.Object);
            _scopeFactoryMock = new Mock<IServiceScopeFactory>();
            _scopeFactoryMock
                .Setup(sf => sf.CreateScope())
                .Returns(_scopeMock.Object);

            _providerMock = new Mock<IServiceProvider>();
            _providerMock
                .Setup(sp => sp.GetService(typeof(ITriviaDbContext)))
                .Returns(dbContextMock.Object);
            _providerMock
                .Setup(sp => sp.GetService(typeof(IServiceScopeFactory)))
                .Returns(_scopeFactoryMock.Object);

            _service = new QuestionService(_providerMock.Object);
        }


        [Test]
        public void GetQuestions_FiltersByCategory()
        {
            var result = _service.GetQuestions([QuestionCategory.Geography], DifficultyLevel.Medium, 10);

            Assert.That(result.All(q => q.Category == QuestionCategory.Geography));
        }

        [Test]
        public void GetQuestionCountByCategory_ReturnsCounts()
        {
            var result = _service.GetQuestionCountByCategory();

            Assert.That(result[QuestionCategory.Geography], Is.EqualTo(2));
            Assert.That(result[QuestionCategory.Science], Is.EqualTo(1));
        }
    }
}