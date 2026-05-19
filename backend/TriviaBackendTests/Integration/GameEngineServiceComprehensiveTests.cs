using System.Reflection;
using Moq;
using TriviaBackend.Models.Entities;
using TriviaBackend.Models.Enums;
using TriviaBackend.Services.Implementations;
using TriviaBackend.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using TriviaBackend.Exceptions;

namespace TriviaBackendTests.Unit
{
    [TestFixture]
    public class GameEngineServiceComprehensiveTests
    {
        private Mock<IServiceProvider> _serviceProviderMock = null!;
        private Mock<ILogger<ExceptionHandler>> _loggerMock = null!;
        private Mock<IQuestionService> _questionServiceMock = null!;
        private GameEngineService _engine = null!;

        [SetUp]
        public void Setup()
        {
            _serviceProviderMock = new Mock<IServiceProvider>();
            _loggerMock = new Mock<ILogger<ExceptionHandler>>();
            _questionServiceMock = new Mock<IQuestionService>();

            // Create scoped service provider that returns IQuestionService
            var scopedServiceProviderMock = new Mock<IServiceProvider>();
            
            // CRITICAL: Use GetService for both GetService and GetRequiredService
            scopedServiceProviderMock.Setup(sp => sp.GetService(typeof(IQuestionService)))
                .Returns(_questionServiceMock.Object);
            
            // Mock GetRequiredService by setting up the method directly
            scopedServiceProviderMock.Setup(sp => sp.GetService(It.IsAny<Type>()))
                .Returns((Type t) => t == typeof(IQuestionService) ? _questionServiceMock.Object : null);

            var scopeMock = new Mock<IServiceScope>();
            scopeMock.Setup(s => s.ServiceProvider).Returns(scopedServiceProviderMock.Object);
            scopeMock.Setup(s => s.Dispose());

            var scopeFactoryMock = new Mock<IServiceScopeFactory>();
            scopeFactoryMock.Setup(f => f.CreateScope()).Returns(scopeMock.Object);

            _serviceProviderMock.Setup(sp => sp.GetService(typeof(IServiceScopeFactory)))
                .Returns(scopeFactoryMock.Object);

            _engine = new GameEngineService(_serviceProviderMock.Object, _loggerMock.Object, new GameSettings
            {
                MaxPlayers = 4,
                AllowLateJoining = true,
                QuestionsPerGame = 5
            });
        }

        //[Test]
        //public void Constructor_WithDefaultSettings_InitializesCorrectly()
        //{
        //    var engine = new GameEngineService(_serviceProviderMock.Object, _loggerMock.Object);
            
        //    Assert.That(engine.Status, Is.EqualTo(GameStatus.Waiting));
        //    Assert.That(engine.CurrentQuestionNumber, Is.EqualTo(0));
        //    Assert.That(engine.GameId, Is.Not.Null.And.Not.Empty);
        //}

        //[Test]
        //public void Constructor_WithCustomGameId_UsesProvidedId()
        //{
        //    var customId = "custom-game-123";
        //    var engine = new GameEngineService(_serviceProviderMock.Object, _loggerMock.Object, 
        //        new GameSettings(), customId);
            
        //    Assert.That(engine.GameId, Is.EqualTo(customId));
        //}

        //[Test]
        //public void Constructor_NullServiceProvider_ThrowsArgumentNullException()
        //{
        //    Assert.Throws<ArgumentNullException>(() =>
        //        new GameEngineService(null!, _loggerMock.Object));
        //}

        //[Test]
        //public void Constructor_NullLogger_ThrowsArgumentNullException()
        //{
        //    Assert.Throws<ArgumentNullException>(() =>
        //        new GameEngineService(_serviceProviderMock.Object, null!));
        //}

        //[Test]
        //public void AddPlayer_WithCustomPlayerId_UsesProvidedId()
        //{
        //    var result = _engine.AddPlayer("Player1", playerId: 999);

        //    Assert.That(result, Is.True);
        //    var players = _engine.GetPlayers();
        //    Assert.That(players[0].Id, Is.EqualTo(999));
        //}

        //[Test]
        //public void AddPlayer_WithCustomJoinTime_UsesProvidedTime()
        //{
        //    var joinTime = new DateTime(2024, 1, 1, 12, 0, 0);
        //    _engine.AddPlayer("Player1", joinTime: joinTime);

        //    var players = _engine.GetPlayers();
        //    Assert.That(players[0].JoinedGameAt, Is.EqualTo(joinTime));
        //}

        //[Test]
        //public void AddPlayer_WhenGameInProgress_AndLateJoiningDisabled_ReturnsFalse()
        //{
        //    var engine = new GameEngineService(_serviceProviderMock.Object, _loggerMock.Object, 
        //        new GameSettings { MaxPlayers = 4, AllowLateJoining = false });

        //    engine.AddPlayer("Player1");
            
        //    // Start game
        //    var questions = new List<TriviaQuestion>
        //    {
        //        new() { Id = 1, CorrectAnswerIndex = 0, TimeLimit = 30, Category = QuestionCategory.Science, Difficulty = DifficultyLevel.Easy }
        //    };
        //    _questionServiceMock.Setup(q => q.GetQuestions(It.IsAny<QuestionCategory[]>(), It.IsAny<DifficultyLevel?>(), It.IsAny<int>()))
        //        .Returns(questions);
            
        //    engine.StartGame();

        //    // Try to add player after game started
        //    var result = engine.AddPlayer("Player2");
        //    Assert.That(result, Is.False);
        //}

        //[Test]
        //public void AddPlayer_WhenGameInProgress_AndLateJoiningEnabled_ReturnsTrue()
        //{
        //    _engine.AddPlayer("Player1");
            
        //    var questions = new List<TriviaQuestion>
        //    {
        //        new() { Id = 1, CorrectAnswerIndex = 0, TimeLimit = 30, Category = QuestionCategory.Science, Difficulty = DifficultyLevel.Easy }
        //    };
        //    _questionServiceMock.Setup(q => q.GetQuestions(It.IsAny<QuestionCategory[]>(), It.IsAny<DifficultyLevel?>(), It.IsAny<int>()))
        //        .Returns(questions);
            
        //    _engine.StartGame();

        //    var result = _engine.AddPlayer("Player2");
        //    Assert.That(result, Is.True);
        //}

        //[Test]
        //public void StartGame_WithNoPlayers_ReturnsFalse()
        //{
        //    var result = _engine.StartGame();
        //    Assert.That(result, Is.False);
        //}

        //[Test]
        //public void StartGame_WhenNotInWaitingStatus_ReturnsFalse()
        //{
        //    _engine.AddPlayer("Player1");
            
        //    var questions = new List<TriviaQuestion>
        //    {
        //        new() { Id = 1, CorrectAnswerIndex = 0, TimeLimit = 30, Category = QuestionCategory.Science, Difficulty = DifficultyLevel.Easy }
        //    };
        //    _questionServiceMock.Setup(q => q.GetQuestions(It.IsAny<QuestionCategory[]>(), It.IsAny<DifficultyLevel?>(), It.IsAny<int>()))
        //        .Returns(questions);
            
        //    _engine.StartGame();
            
        //    var result = _engine.StartGame(); // Try to start again
        //    Assert.That(result, Is.False);
        //}

        //[Test]
        //public void StartGame_WithNoQuestions_ReturnsFalse()
        //{
        //    _engine.AddPlayer("Player1");
            
        //    _questionServiceMock.Setup(q => q.GetQuestions(It.IsAny<QuestionCategory[]>(), It.IsAny<DifficultyLevel?>(), It.IsAny<int>()))
        //        .Returns(new List<TriviaQuestion>());

        //    var result = _engine.StartGame();
        //    Assert.That(result, Is.False);
        //}

        //[Test]
        //public void StartGame_WithException_ThrowsStartGameException()
        //{
        //    _engine.AddPlayer("Player1");
            
        //    _questionServiceMock.Setup(q => q.GetQuestions(It.IsAny<QuestionCategory[]>(), It.IsAny<DifficultyLevel?>(), It.IsAny<int>()))
        //        .Throws(new Exception("Database error"));

        //    Assert.Throws<StartGameException>(() => _engine.StartGame());
        //}

        //[Test]
        //public void StartGame_WithCustomCategories_UsesProvidedCategories()
        //{
        //    _engine.AddPlayer("Player1");
            
        //    var customCategories = new[] { QuestionCategory.History, QuestionCategory.Geography };
        //    var questions = new List<TriviaQuestion>
        //    {
        //        new() { Id = 1, CorrectAnswerIndex = 0, TimeLimit = 30, Category = QuestionCategory.History, Difficulty = DifficultyLevel.Easy }
        //    };
            
        //    _questionServiceMock.Setup(q => q.GetQuestions(
        //        It.Is<QuestionCategory[]>(c => c.SequenceEqual(customCategories)),
        //        It.IsAny<DifficultyLevel?>(),
        //        It.IsAny<int>()))
        //        .Returns(questions);

        //    var result = _engine.StartGame(customCategories);
        //    Assert.That(result, Is.True);
        //}

        //[Test]
        //public void StartGame_WithCustomDifficulty_UsesProvidedDifficulty()
        //{
        //    _engine.AddPlayer("Player1");
            
        //    var questions = new List<TriviaQuestion>
        //    {
        //        new() { Id = 1, CorrectAnswerIndex = 0, TimeLimit = 30, Category = QuestionCategory.Science, Difficulty = DifficultyLevel.Easy }
        //    };
            
        //    _questionServiceMock.Setup(q => q.GetQuestions(
        //        It.IsAny<QuestionCategory[]>(),
        //        DifficultyLevel.Medium,
        //        It.IsAny<int>()))
        //        .Returns(questions);

        //    var result = _engine.StartGame(maxDifficulty: DifficultyLevel.Medium);
        //    Assert.That(result, Is.True);
        //}

        //[Test]
        //public void NextQuestion_WhenNoQuestionsLeft_EndsGameAndReturnsFalse()
        //{
        //    _engine.AddPlayer("Player1");
            
        //    var questions = new List<TriviaQuestion>
        //    {
        //        new() { Id = 1, CorrectAnswerIndex = 0, TimeLimit = 30, Category = QuestionCategory.Science, Difficulty = DifficultyLevel.Easy }
        //    };
        //    _questionServiceMock.Setup(q => q.GetQuestions(It.IsAny<QuestionCategory[]>(), It.IsAny<DifficultyLevel?>(), It.IsAny<int>()))
        //        .Returns(questions);
            
        //    _engine.StartGame();
        //    var result = _engine.NextQuestion(); // Move to second question (but there is none)

        //    Assert.That(result, Is.False);
        //    Assert.That(_engine.Status, Is.EqualTo(GameStatus.Finished));
        //}

        //[Test]
        //public void SubmitAnswer_WhenNoCurrentQuestion_ReturnsTimeUp()
        //{
        //    _engine.AddPlayer("Player1", playerId: 1);
            
        //    var result = _engine.SubmitAnswer(1, 0);
        //    Assert.That(result, Is.EqualTo(AnswerResult.TimeUp));
        //}

        //[Test]
        //public void SubmitAnswer_WhenGameNotInProgress_ReturnsTimeUp()
        //{
        //    _engine.AddPlayer("Player1", playerId: 1);
        //    _engine.EndGame();
            
        //    var result = _engine.SubmitAnswer(1, 0);
        //    Assert.That(result, Is.EqualTo(AnswerResult.TimeUp));
        //}

        //[Test]
        //public void SubmitAnswer_NonExistentPlayer_ReturnsTimeUp()
        //{
        //    _engine.AddPlayer("Player1", playerId: 1);
            
        //    var questions = new List<TriviaQuestion>
        //    {
        //        new() { Id = 1, CorrectAnswerIndex = 2, TimeLimit = 30, Category = QuestionCategory.Science, Difficulty = DifficultyLevel.Easy }
        //    };
        //    _questionServiceMock.Setup(q => q.GetQuestions(It.IsAny<QuestionCategory[]>(), It.IsAny<DifficultyLevel?>(), It.IsAny<int>()))
        //        .Returns(questions);
            
        //    _engine.StartGame();
            
        //    var result = _engine.SubmitAnswer(999, 2);
        //    Assert.That(result, Is.EqualTo(AnswerResult.TimeUp));
        //}

        //[Test]
        //public void SubmitAnswer_IncorrectAnswer_ReturnsIncorrect()
        //{
        //    _engine.AddPlayer("Player1", playerId: 1);
            
        //    var questions = new List<TriviaQuestion>
        //    {
        //        new() { Id = 1, CorrectAnswerIndex = 2, TimeLimit = 30, Category = QuestionCategory.Science, Difficulty = DifficultyLevel.Easy }
        //    };
        //    _questionServiceMock.Setup(q => q.GetQuestions(It.IsAny<QuestionCategory[]>(), It.IsAny<DifficultyLevel?>(), It.IsAny<int>()))
        //        .Returns(questions);
            
        //    _engine.StartGame();
            
        //    var result = _engine.SubmitAnswer(1, 0); // Wrong answer
        //    Assert.That(result, Is.EqualTo(AnswerResult.Incorrect));
            
        //    var players = _engine.GetPlayers();
        //    Assert.That(players[0].CurrentGameScore, Is.EqualTo(0));
        //    Assert.That(players[0].CorrectAnswersInGame, Is.EqualTo(0));
        //}

        //[Test]
        //public void SubmitAnswer_CorrectAnswer_IncreasesScore()
        //{
        //    _engine.AddPlayer("Player1", playerId: 1);
            
        //    var questions = new List<TriviaQuestion>
        //    {
        //        new() { Id = 1, CorrectAnswerIndex = 2, TimeLimit = 30, Category = QuestionCategory.Science, Difficulty = DifficultyLevel.Easy }
        //    };
        //    _questionServiceMock.Setup(q => q.GetQuestions(It.IsAny<QuestionCategory[]>(), It.IsAny<DifficultyLevel?>(), It.IsAny<int>()))
        //        .Returns(questions);
            
        //    _engine.StartGame();
            
        //    var result = _engine.SubmitAnswer(1, 2); // Correct answer
        //    Assert.That(result, Is.EqualTo(AnswerResult.Correct));
            
        //    var players = _engine.GetPlayers();
        //    Assert.That(players[0].CurrentGameScore, Is.GreaterThan(0));
        //    Assert.That(players[0].CorrectAnswersInGame, Is.EqualTo(1));
        //}

        //[Test]
        //public void GetCurrentGameLeaderboard_ReturnsSortedPlayers()
        //{
        //    _engine.AddPlayer("Alice", playerId: 1);
        //    _engine.AddPlayer("Bob", playerId: 2);
        //    _engine.AddPlayer("Charlie", playerId: 3);

        //    var questions = new List<TriviaQuestion>
        //    {
        //        new() { Id = 1, CorrectAnswerIndex = 0, TimeLimit = 30, Category = QuestionCategory.Science, Difficulty = DifficultyLevel.Easy }
        //    };
        //    _questionServiceMock.Setup(q => q.GetQuestions(It.IsAny<QuestionCategory[]>(), It.IsAny<DifficultyLevel?>(), It.IsAny<int>()))
        //        .Returns(questions);
            
        //    _engine.StartGame();

        //    _engine.SubmitAnswer(1, 0); // Alice answers correctly
        //    _engine.SubmitAnswer(3, 0); // Charlie answers correctly

        //    var leaderboard = _engine.GetCurrentGameLeaderboard();
            
        //    Assert.That(leaderboard.Count, Is.EqualTo(3));
        //    Assert.That(leaderboard[0].CurrentGameScore, Is.GreaterThanOrEqualTo(leaderboard[1].CurrentGameScore));
        //    Assert.That(leaderboard[1].CurrentGameScore, Is.GreaterThanOrEqualTo(leaderboard[2].CurrentGameScore));
        //}

        //[Test]
        //public void GetGameAnswers_ReturnsAllAnswers()
        //{
        //    _engine.AddPlayer("Player1", playerId: 1);
        //    _engine.AddPlayer("Player2", playerId: 2);

        //    var questions = new List<TriviaQuestion>
        //    {
        //        new() { Id = 1, CorrectAnswerIndex = 0, TimeLimit = 30, Category = QuestionCategory.Science, Difficulty = DifficultyLevel.Easy }
        //    };
        //    _questionServiceMock.Setup(q => q.GetQuestions(It.IsAny<QuestionCategory[]>(), It.IsAny<DifficultyLevel?>(), It.IsAny<int>()))
        //        .Returns(questions);
            
        //    _engine.StartGame();
        //    _engine.SubmitAnswer(1, 0);
        //    _engine.SubmitAnswer(2, 1);

        //    var answers = _engine.GetGameAnswers();
            
        //    Assert.That(answers.ContainsKey(1), Is.True);
        //    Assert.That(answers.ContainsKey(2), Is.True);
        //    Assert.That(answers[1].Count, Is.EqualTo(1));
        //    Assert.That(answers[2].Count, Is.EqualTo(1));
        //}

        //[Test]
        //public void CurrentQuestion_BeforeGameStart_ReturnsNull()
        //{
        //    Assert.That(_engine.CurrentQuestion, Is.Null);
        //}

        //[Test]
        //public void CurrentQuestion_AfterGameStart_ReturnsQuestion()
        //{
        //    _engine.AddPlayer("Player1");
            
        //    var questions = new List<TriviaQuestion>
        //    {
        //        new() { Id = 1, CorrectAnswerIndex = 0, TimeLimit = 30, Category = QuestionCategory.Science, Difficulty = DifficultyLevel.Easy }
        //    };
        //    _questionServiceMock.Setup(q => q.GetQuestions(It.IsAny<QuestionCategory[]>(), It.IsAny<DifficultyLevel?>(), It.IsAny<int>()))
        //        .Returns(questions);
            
        //    _engine.StartGame();
            
        //    Assert.That(_engine.CurrentQuestion, Is.Not.Null);
        //    Assert.That(_engine.CurrentQuestion!.Id, Is.EqualTo(1));
        //}

        //[Test]
        //public void TimeRemaining_WhenNoCurrentQuestion_ReturnsZero()
        //{
        //    var timeRemaining = _engine.TimeRemaining;
        //    Assert.That(timeRemaining, Is.EqualTo(TimeSpan.Zero));
        //}

        //[Test]
        //public void GetSettings_ReturnsCorrectSettings()
        //{
        //    var settings = _engine.GetSettings();
        //    Assert.That(settings.MaxPlayers, Is.EqualTo(4));
        //    Assert.That(settings.AllowLateJoining, Is.True);
        //    Assert.That(settings.QuestionsPerGame, Is.EqualTo(5));
        //}

        //[Test]
        //public void AllPlayersAnswered_WithAllAnswers_ReturnsTrue()
        //{
        //    _engine.AddPlayer("Player1", playerId: 1);
        //    _engine.AddPlayer("Player2", playerId: 2);

        //    var questions = new List<TriviaQuestion>
        //    {
        //        new() { Id = 1, CorrectAnswerIndex = 0, TimeLimit = 30, Category = QuestionCategory.Science, Difficulty = DifficultyLevel.Easy }
        //    };
        //    _questionServiceMock.Setup(q => q.GetQuestions(It.IsAny<QuestionCategory[]>(), It.IsAny<DifficultyLevel?>(), It.IsAny<int>()))
        //        .Returns(questions);
            
        //    _engine.StartGame();
        //    _engine.SubmitAnswer(1, 0);
        //    _engine.SubmitAnswer(2, 0);

        //    Assert.That(_engine.AllPlayersAnswered(), Is.True);
        //}

        //[Test]
        //public void AllPlayersAnswered_WithSomeAnswers_ReturnsFalse()
        //{
        //    _engine.AddPlayer("Player1", playerId: 1);
        //    _engine.AddPlayer("Player2", playerId: 2);

        //    var questions = new List<TriviaQuestion>
        //    {
        //        new() { Id = 1, CorrectAnswerIndex = 0, TimeLimit = 30, Category = QuestionCategory.Science, Difficulty = DifficultyLevel.Easy }
        //    };
        //    _questionServiceMock.Setup(q => q.GetQuestions(It.IsAny<QuestionCategory[]>(), It.IsAny<DifficultyLevel?>(), It.IsAny<int>()))
        //        .Returns(questions);
            
        //    _engine.StartGame();
        //    _engine.SubmitAnswer(1, 0);

        //    Assert.That(_engine.AllPlayersAnswered(), Is.False);
        //}
    }
}
