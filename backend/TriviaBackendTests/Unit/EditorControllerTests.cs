using Moq;
using Microsoft.AspNetCore.Mvc;
using TriviaBackend.Controllers;
using TriviaBackend.Models.Entities;
using TriviaBackend.Models.Enums;
using TriviaBackend.Services.Interfaces.DB;

namespace TriviaBackendTests.Unit
{
    public class EditorControllerTests
    {
        private Mock<IQuestionsService> _mockService = null!;
        private EditorController _controller = null!;

        [SetUp]
        public void Setup()
        {
            _mockService = new Mock<IQuestionsService>();
            _controller = new EditorController(_mockService.Object);
        }

        [Test]
        public async Task GetQuestion_ReturnsOk_WhenExists()
        {
            var q = new TriviaQuestion
            {
                Id = 5,
                Category = QuestionCategory.Geography
            };

            _mockService
                .Setup(s => s.GetQuestionByIdAsync(5))
                .ReturnsAsync(q);

            var result = await _controller.GetQuestion(5);

            var ok = result.Result as OkObjectResult;
            Assert.That(ok, Is.Not.Null);

            var value = ok!.Value as TriviaQuestion;
            Assert.That(value!.Id, Is.EqualTo(5));
        }

        [Test]
        public async Task GetQuestion_ReturnsNotFound_WhenMissing()
        {
            _mockService
                .Setup(s => s.GetQuestionByIdAsync(0))
                .ReturnsAsync((TriviaQuestion?)null);

            var result = await _controller.GetQuestion(0);

            Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
        }

        [Test]
        public async Task DeleteQuestion_CallsService()
        {
            _mockService
                .Setup(s => s.DeleteQuestionByIdAsync(7))
                .Returns(Task.CompletedTask);

            await _controller.DeleteQuestion(7);

            var shouldEmpty = await _controller.GetQuestion(7);

            Assert.That(shouldEmpty.Result, Is.InstanceOf<NotFoundResult>());
        }
    }
}
