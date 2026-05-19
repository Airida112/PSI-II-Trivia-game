using System.Reflection;
using Moq;
using TriviaBackend.Models.Entities;
using TriviaBackend.Models.Enums;
using TriviaBackend.Exceptions;
using TriviaBackend.Services.Implementations;
using Microsoft.Extensions.Logging;

namespace TriviaBackendTests.Unit
{
    [TestFixture]
    public class GameEngineServiceTests
    {
        private Mock<IServiceProvider> _serviceProviderMock = null!;
        private Mock<ILogger<ExceptionHandler>> _loggerMock = null!;
        private GameEngineService _engine = null!;

        [SetUp]
        public void Setup()
        {
            _serviceProviderMock = new Mock<IServiceProvider>();
            _loggerMock = new Mock<ILogger<ExceptionHandler>>();

            _engine = new GameEngineService(_serviceProviderMock.Object, _loggerMock.Object, new GameSettings
            {
                MaxPlayers = 2,
                AllowLateJoining = true
            });
        }

        [Test]
        public void AddPlayer_ShouldAddPlayer_WhenUnderMaxPlayers()
        {
            var result = _engine.AddPlayer("A");

            Assert.That(result, Is.True);
            Assert.That(_engine.GetSettings().MaxPlayers, Is.EqualTo(2));
            Assert.That(_engine.CurrentQuestionNumber, Is.EqualTo(0));
        }

        [Test]
        public void AddPlayer_ShouldReturnFalse_WhenMaxPlayersReached()
        {
            _engine.AddPlayer("A");
            _engine.AddPlayer("B");

            var result = _engine.AddPlayer("C");

            Assert.That(result, Is.False);
        }

        [Test]
        public void AddPlayer_ShouldInitializePlayerCorrectly()
        {
            _engine.AddPlayer("A", 42);

            var field = typeof(GameEngineService)
                .GetField("_players", BindingFlags.NonPublic | BindingFlags.Instance);
            var players = (List<GamePlayer>)field!.GetValue(_engine)!;

            Assert.That(players.Count, Is.EqualTo(1));
            Assert.That(players[0].Id, Is.EqualTo(42));
            Assert.That(players[0].Name, Is.EqualTo("A"));
            Assert.That(players[0].CurrentGameScore, Is.EqualTo(0));
            Assert.That(players[0].IsActive, Is.True);
        }

        [Test]
        public void EndGame_SetsStatusToFinishedAndClearsQuestion()
        {
            _engine.StartGame();
            _engine.EndGame();

            Assert.That(_engine.Status, Is.EqualTo(GameStatus.Finished));
            Assert.That(_engine.NextQuestion(), Is.False);
        }

        [Test]
        public void GetPlayers_ReturnsCopyNotReference()
        {
            _engine.AddPlayer("A");
            _engine.AddPlayer("B");

            var list1 = _engine.GetPlayers();
            var list2 = _engine.GetPlayers();

            Assert.That(list1, Is.Not.SameAs(list2));
            Assert.That(list1.Count, Is.EqualTo(2));
            Assert.That(list2.Count, Is.EqualTo(2));
        }

        [Test]
        public void AllPlayersAnswered_ReturnsFalse_WhenCurrentQuestionIsNull()
        {
            _engine.AddPlayer("A", playerId: 1);

            Assert.That(_engine.AllPlayersAnswered(), Is.False);
        }

        [Test]
        public void SubmitAnswer_RecordsAnswerForCorrectPlayer()
        {
            _engine.AddPlayer("Player1", playerId: 1);
            _engine.AddPlayer("Player2", playerId: 2);

            var question = new TriviaQuestion
            {
                Id = 10,
                CorrectAnswerIndex = 3,
                TimeLimit = 30
            };

            var engineType = typeof(GameEngineService);
            engineType.GetField("_currentQuestion", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(_engine, question);
            engineType.GetField("_questionStartTime", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(_engine, DateTime.Now);
            engineType.GetProperty("Status", BindingFlags.Instance | BindingFlags.Public)!.SetValue(_engine, GameStatus.InProgress);

            var result = _engine.SubmitAnswer(2, 3);

            var answers = _engine.GetGameAnswers()[2];
            Assert.That(answers.Count, Is.EqualTo(1));
            Assert.That(answers[0].SelectedOptionIndex, Is.EqualTo(3));
            Assert.That(result, Is.EqualTo(AnswerResult.Correct));
        }
    }
}
