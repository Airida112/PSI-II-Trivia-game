using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using TriviaBackend;
using TriviaBackend.Models.Entities;
using TriviaBackend.Models.Enums;
using TriviaBackend.Models.Records;

namespace TriviaBackendTests.Integration
{
    public class EditorControllerTests : WebApplicationFactory<Program>
    {
        private TestWebApplicationFactory _factory = null!;
        private HttpClient _client = null!;

        [SetUp]
        public void Setup()
        {
            _factory = new TestWebApplicationFactory();
            _client = _factory.CreateClient();
        }

        [TearDown]
        public void TearDown()
        {
            _client.Dispose();
            _factory.Dispose();
        }

        [Test]
        public async Task GetQuestion_WhenExists_ReturnsOk()
        {
            var create = await _client.PostAsJsonAsync("/api/editor/addquestion", new TriviaQuestionDTO
            (
                QuestionText: "Test question",
                Answer1: "A0",
                Answer2: "B",
                Answer3: "C",
                Answer4: "D",
                CorrectAnswerIndex: 3,
                Category: QuestionCategory.History,
                Difficulty: DifficultyLevel.Easy,
                TimeLimit: 30
            ));

            var created = await create.Content.ReadFromJsonAsync<TriviaQuestion>();
            var id = created!.Id;
            Console.WriteLine($"Created question with ID: {id}");

            var response = await _client.GetAsync($"/api/editor/getquestion/{id}");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Test]
        public async Task GetQuestion_WrongIdReturnsNotFound()
        {
            var response = await _client.GetAsync("/api/editor/getquestion/0");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        [Test]
        public async Task AddQuestion_ReturnsOk()
        {
            var response = await _client.PostAsJsonAsync("/api/editor/addquestion", new TriviaQuestionDTO
            (
                QuestionText: "Test question",
                Answer1: "A1",
                Answer2: "B",
                Answer3: "C",
                Answer4: "D",
                CorrectAnswerIndex: 3,
                Category: QuestionCategory.Sports,
                Difficulty: DifficultyLevel.Easy,
                TimeLimit: 30
            ));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Test]
        public async Task AddQuestion_WrongIndex_RetrnsBadRequest()
        {
            var response = await _client.PostAsJsonAsync("/api/editor/addquestion", new TriviaQuestionDTO
            (
                QuestionText: "Test question",
                Answer1: "A2",
                Answer2: "B",
                Answer3: "C",
                Answer4: "D",
                CorrectAnswerIndex: 5,
                Category: QuestionCategory.Science,
                Difficulty: DifficultyLevel.Easy,
                TimeLimit: 30
            ));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }
        [Test]
        public async Task AddQuestion_WrongTimelimit_RetrnsBadRequest()
        {
            var response = await _client.PostAsJsonAsync("/api/editor/addquestion", new TriviaQuestionDTO
            (
                QuestionText: "Test question",
                Answer1: "A3",
                Answer2: "B",
                Answer3: "C",
                Answer4: "D",
                CorrectAnswerIndex: 0,
                Category: QuestionCategory.Science,
                Difficulty: DifficultyLevel.Easy,
                TimeLimit: 2
            ));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test]
        public async Task DeleteQuestion_RemovesQuestion()
        {
            var response = await _client.PostAsJsonAsync("/api/editor/addquestion", new TriviaQuestionDTO
            (
                QuestionText: "Test question",
                Answer1: "A4",
                Answer2: "B",
                Answer3: "C",
                Answer4: "D",
                CorrectAnswerIndex: 2,
                Category: QuestionCategory.Science,
                Difficulty: DifficultyLevel.Easy,
                TimeLimit: 30
            ));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            await _client.DeleteAsync($"/api/editor/deletequestion/1");

            response = await _client.GetAsync($"/api/editor/getquestion/1");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }
    }
}