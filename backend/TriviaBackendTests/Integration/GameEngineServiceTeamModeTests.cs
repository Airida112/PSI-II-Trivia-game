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
    public class GameEngineServiceTeamModeTests
    {
        private Mock<IServiceProvider> _serviceProviderMock = null!;
        private Mock<ILogger<ExceptionHandler>> _loggerMock = null!;
        private Mock<IQuestionService> _questionServiceMock = null!;

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
        }

    //    [Test]
    //    public void Constructor_WithTeamMode_InitializesTeams()
    //    {
    //        var engine = new GameEngineService(_serviceProviderMock.Object, _loggerMock.Object, 
    //            new GameSettings 
    //            { 
    //                IsTeamMode = true,
    //                NumberOfTeams = 2,
    //                MaxPlayers = 4
    //            });

    //        var teams = engine.GetTeams();
    //        Assert.That(teams.Count, Is.EqualTo(2));
    //        Assert.That(teams[0].Name, Is.EqualTo("Red"));
    //        Assert.That(teams[1].Name, Is.EqualTo("Blue"));
    //    }

    //    [Test]
    //    public void Constructor_WithFourTeams_InitializesAllTeams()
    //    {
    //        var engine = new GameEngineService(_serviceProviderMock.Object, _loggerMock.Object, 
    //            new GameSettings 
    //            { 
    //                IsTeamMode = true,
    //                NumberOfTeams = 4,
    //                MaxPlayers = 8
    //            });

    //        var teams = engine.GetTeams();
    //        Assert.That(teams.Count, Is.EqualTo(4));
    //        Assert.That(teams[0].Name, Is.EqualTo("Red"));
    //        Assert.That(teams[1].Name, Is.EqualTo("Blue"));
    //        Assert.That(teams[2].Name, Is.EqualTo("Green"));
    //        Assert.That(teams[3].Name, Is.EqualTo("Yellow"));
    //    }

    //    [Test]
    //    public void AddPlayer_InTeamMode_AssignsToSmallestTeam()
    //    {
    //        var engine = new GameEngineService(_serviceProviderMock.Object, _loggerMock.Object, 
    //            new GameSettings 
    //            { 
    //                IsTeamMode = true,
    //                NumberOfTeams = 2,
    //                MaxPlayers = 4
    //            });

    //        engine.AddPlayer("Player1", playerId: 1);
    //        engine.AddPlayer("Player2", playerId: 2);
    //        engine.AddPlayer("Player3", playerId: 3);

    //        var teams = engine.GetTeams();
            
    //        // First two players should be distributed across teams
    //        Assert.That(teams[0].Members.Count + teams[1].Members.Count, Is.EqualTo(3));
    //    }

    //    [Test]
    //    public void AddPlayer_WithPreferredTeamId_AssignsToSpecificTeam()
    //    {
    //        var engine = new GameEngineService(_serviceProviderMock.Object, _loggerMock.Object, 
    //            new GameSettings 
    //            { 
    //                IsTeamMode = true,
    //                NumberOfTeams = 2,
    //                MaxPlayers = 4
    //            });

    //        engine.AddPlayer("Player1", playerId: 1, teamId: 2);

    //        var teams = engine.GetTeams();
    //        var team2 = teams.FirstOrDefault(t => t.Id == 2);
            
    //        Assert.That(team2, Is.Not.Null);
    //        Assert.That(team2!.Members.Count, Is.EqualTo(1));
    //        Assert.That(team2.Members[0].Name, Is.EqualTo("Player1"));
    //    }

    //    [Test]
    //    public void AssignPlayerToSpecificTeam_InWaitingStatus_ReturnsTrue()
    //    {
    //        var engine = new GameEngineService(_serviceProviderMock.Object, _loggerMock.Object, 
    //            new GameSettings 
    //            { 
    //                IsTeamMode = true,
    //                NumberOfTeams = 2,
    //                MaxPlayers = 4
    //            });

    //        engine.AddPlayer("Player1", playerId: 1, teamId: 1);

    //        var result = engine.AssignPlayerToSpecificTeam(1, 2);

    //        Assert.That(result, Is.True);
    //        var teams = engine.GetTeams();
    //        var team2 = teams.FirstOrDefault(t => t.Id == 2);
    //        Assert.That(team2!.Members.Any(m => m.Id == 1), Is.True);
    //    }

    //    [Test]
    //    public void AssignPlayerToSpecificTeam_NotInTeamMode_ReturnsFalse()
    //    {
    //        var engine = new GameEngineService(_serviceProviderMock.Object, _loggerMock.Object, 
    //            new GameSettings { MaxPlayers = 4 }); // Team mode disabled

    //        engine.AddPlayer("Player1", playerId: 1);

    //        var result = engine.AssignPlayerToSpecificTeam(1, 1);
    //        Assert.That(result, Is.False);
    //    }

    //    [Test]
    //    public void AssignPlayerToSpecificTeam_GameInProgress_ReturnsFalse()
    //    {
    //        var engine = new GameEngineService(_serviceProviderMock.Object, _loggerMock.Object, 
    //            new GameSettings 
    //            { 
    //                IsTeamMode = true,
    //                NumberOfTeams = 2,
    //                MaxPlayers = 4
    //            });

    //        engine.AddPlayer("Player1", playerId: 1);

    //        var questions = new List<TriviaQuestion>
    //        {
    //            new() { Id = 1, CorrectAnswerIndex = 0, TimeLimit = 30, Category = QuestionCategory.Science, Difficulty = DifficultyLevel.Easy }
    //        };
    //        _questionServiceMock.Setup(q => q.GetQuestions(It.IsAny<QuestionCategory[]>(), It.IsAny<DifficultyLevel?>(), It.IsAny<int>()))
    //            .Returns(questions);
            
    //        engine.StartGame();

    //        var result = engine.AssignPlayerToSpecificTeam(1, 2);
    //        Assert.That(result, Is.False);
    //    }

    //    [Test]
    //    public void AssignPlayerToSpecificTeam_NonExistentPlayer_ReturnsFalse()
    //    {
    //        var engine = new GameEngineService(_serviceProviderMock.Object, _loggerMock.Object, 
    //            new GameSettings 
    //            { 
    //                IsTeamMode = true,
    //                NumberOfTeams = 2,
    //                MaxPlayers = 4
    //            });

    //        var result = engine.AssignPlayerToSpecificTeam(999, 1);
    //        Assert.That(result, Is.False);
    //    }

    //    [Test]
    //    public void AssignPlayerToSpecificTeam_NonExistentTeam_ReturnsFalse()
    //    {
    //        var engine = new GameEngineService(_serviceProviderMock.Object, _loggerMock.Object, 
    //            new GameSettings 
    //            { 
    //                IsTeamMode = true,
    //                NumberOfTeams = 2,
    //                MaxPlayers = 4
    //            });

    //        engine.AddPlayer("Player1", playerId: 1);

    //        var result = engine.AssignPlayerToSpecificTeam(1, 999);
    //        Assert.That(result, Is.False);
    //    }

    //    [Test]
    //    public void SubmitAnswer_InTeamMode_UpdatesTeamScore()
    //    {
    //        var engine = new GameEngineService(_serviceProviderMock.Object, _loggerMock.Object, 
    //            new GameSettings 
    //            { 
    //                IsTeamMode = true,
    //                NumberOfTeams = 2,
    //                MaxPlayers = 4,
    //                QuestionsPerGame = 1
    //            });

    //        engine.AddPlayer("Player1", playerId: 1, teamId: 1);

    //        var questions = new List<TriviaQuestion>
    //        {
    //            new() { Id = 1, CorrectAnswerIndex = 2, TimeLimit = 30, Category = QuestionCategory.Science, Difficulty = DifficultyLevel.Easy }
    //        };
    //        _questionServiceMock.Setup(q => q.GetQuestions(It.IsAny<QuestionCategory[]>(), It.IsAny<DifficultyLevel?>(), It.IsAny<int>()))
    //            .Returns(questions);
            
    //        engine.StartGame();

    //        var result = engine.SubmitAnswer(1, 2); // Correct answer

    //        Assert.That(result, Is.EqualTo(AnswerResult.Correct));
            
    //        var teams = engine.GetTeams();
    //        var team1 = teams.FirstOrDefault(t => t.Id == 1);
    //        Assert.That(team1!.TotalScore, Is.GreaterThan(0));
    //        Assert.That(team1.CorrectAnswers, Is.EqualTo(1));
    //    }

    //    [Test]
    //    public void SubmitAnswer_InTeamMode_IncorrectAnswer_DoesNotUpdateTeamScore()
    //    {
    //        var engine = new GameEngineService(_serviceProviderMock.Object, _loggerMock.Object, 
    //            new GameSettings 
    //            { 
    //                IsTeamMode = true,
    //                NumberOfTeams = 2,
    //                MaxPlayers = 4,
    //                QuestionsPerGame = 1
    //            });

    //        engine.AddPlayer("Player1", playerId: 1, teamId: 1);

    //        var questions = new List<TriviaQuestion>
    //        {
    //            new() { Id = 1, CorrectAnswerIndex = 2, TimeLimit = 30, Category = QuestionCategory.Science, Difficulty = DifficultyLevel.Easy }
    //        };
    //        _questionServiceMock.Setup(q => q.GetQuestions(It.IsAny<QuestionCategory[]>(), It.IsAny<DifficultyLevel?>(), It.IsAny<int>()))
    //            .Returns(questions);
            
    //        engine.StartGame();

    //        var result = engine.SubmitAnswer(1, 0); // Wrong answer

    //        Assert.That(result, Is.EqualTo(AnswerResult.Incorrect));
            
    //        var teams = engine.GetTeams();
    //        var team1 = teams.FirstOrDefault(t => t.Id == 1);
    //        Assert.That(team1!.TotalScore, Is.EqualTo(0));
    //        Assert.That(team1.CorrectAnswers, Is.EqualTo(0));
    //    }

    //    [Test]
    //    public void GetTeamLeaderboard_ReturnsSortedByScore()
    //    {
    //        var engine = new GameEngineService(_serviceProviderMock.Object, _loggerMock.Object, 
    //            new GameSettings 
    //            { 
    //                IsTeamMode = true,
    //                NumberOfTeams = 3,
    //                MaxPlayers = 6,
    //                QuestionsPerGame = 1
    //            });

    //        engine.AddPlayer("Player1", playerId: 1, teamId: 1);
    //        engine.AddPlayer("Player2", playerId: 2, teamId: 2);
    //        engine.AddPlayer("Player3", playerId: 3, teamId: 3);

    //        var questions = new List<TriviaQuestion>
    //        {
    //            new() { Id = 1, CorrectAnswerIndex = 0, TimeLimit = 30, Category = QuestionCategory.Science, Difficulty = DifficultyLevel.Easy }
    //        };
    //        _questionServiceMock.Setup(q => q.GetQuestions(It.IsAny<QuestionCategory[]>(), It.IsAny<DifficultyLevel?>(), It.IsAny<int>()))
    //            .Returns(questions);
            
    //        engine.StartGame();

    //        // Team 1 and 3 answer correctly
    //        engine.SubmitAnswer(1, 0);
    //        engine.SubmitAnswer(3, 0);

    //        var leaderboard = engine.GetTeamLeaderboard();

    //        Assert.That(leaderboard.Count, Is.EqualTo(3));
    //        Assert.That(leaderboard[0].TotalScore, Is.GreaterThanOrEqualTo(leaderboard[1].TotalScore));
    //        Assert.That(leaderboard[1].TotalScore, Is.GreaterThanOrEqualTo(leaderboard[2].TotalScore));
    //    }

    //    [Test]
    //    public void GetTeamLeaderboard_TieBreaker_UseCorrectAnswers()
    //    {
    //        var engine = new GameEngineService(_serviceProviderMock.Object, _loggerMock.Object, 
    //            new GameSettings 
    //            { 
    //                IsTeamMode = true,
    //                NumberOfTeams = 2,
    //                MaxPlayers = 4,
    //                QuestionsPerGame = 2
    //            });

    //        engine.AddPlayer("Player1", playerId: 1, teamId: 1);
    //        engine.AddPlayer("Player2", playerId: 2, teamId: 2);

    //        var questions = new List<TriviaQuestion>
    //        {
    //            new() { Id = 1, CorrectAnswerIndex = 0, TimeLimit = 30, Category = QuestionCategory.Science, Difficulty = DifficultyLevel.Easy },
    //            new() { Id = 2, CorrectAnswerIndex = 1, TimeLimit = 30, Category = QuestionCategory.Science, Difficulty = DifficultyLevel.Easy }
    //        };
    //        _questionServiceMock.Setup(q => q.GetQuestions(It.IsAny<QuestionCategory[]>(), It.IsAny<DifficultyLevel?>(), It.IsAny<int>()))
    //            .Returns(questions);
            
    //        engine.StartGame();

    //        // Both teams get similar scores but different correct answer counts
    //        engine.SubmitAnswer(1, 0); // Correct
    //        engine.SubmitAnswer(2, 0); // Correct

    //        var leaderboard = engine.GetTeamLeaderboard();

    //        // Verify leaderboard is ordered properly
    //        Assert.That(leaderboard.Count, Is.EqualTo(2));
    //    }

    //    [Test]
    //    public void GetTeams_ReturnsListCopy()
    //    {
    //        var engine = new GameEngineService(_serviceProviderMock.Object, _loggerMock.Object, 
    //            new GameSettings 
    //            { 
    //                IsTeamMode = true,
    //                NumberOfTeams = 2,
    //                MaxPlayers = 4
    //            });

    //        var teams1 = engine.GetTeams();
    //        var teams2 = engine.GetTeams();

    //        Assert.That(teams1, Is.Not.SameAs(teams2));
    //        Assert.That(teams1.Count, Is.EqualTo(teams2.Count));
    //    }

    //    [Test]
    //    public void TeamMode_MultiplePlayersOneTeam_AggregatesScores()
    //    {
    //        var engine = new GameEngineService(_serviceProviderMock.Object, _loggerMock.Object, 
    //            new GameSettings 
    //            { 
    //                IsTeamMode = true,
    //                NumberOfTeams = 1,
    //                MaxPlayers = 3,
    //                QuestionsPerGame = 1
    //            });

    //        engine.AddPlayer("Player1", playerId: 1, teamId: 1);
    //        engine.AddPlayer("Player2", playerId: 2, teamId: 1);

    //        var questions = new List<TriviaQuestion>
    //        {
    //            new() { Id = 1, CorrectAnswerIndex = 0, TimeLimit = 30, Category = QuestionCategory.Science, Difficulty = DifficultyLevel.Easy }
    //        };
    //        _questionServiceMock.Setup(q => q.GetQuestions(It.IsAny<QuestionCategory[]>(), It.IsAny<DifficultyLevel?>(), It.IsAny<int>()))
    //            .Returns(questions);
            
    //        engine.StartGame();

    //        engine.SubmitAnswer(1, 0); // Correct
    //        engine.SubmitAnswer(2, 0); // Correct

    //        var teams = engine.GetTeams();
    //        var team1 = teams[0];
            
    //        Assert.That(team1.CorrectAnswers, Is.EqualTo(2));
    //        Assert.That(team1.TotalScore, Is.GreaterThan(100)); // Combined score from both players
    //    }
    }
}
