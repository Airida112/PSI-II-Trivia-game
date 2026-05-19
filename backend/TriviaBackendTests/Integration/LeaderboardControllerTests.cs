using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using TriviaBackend.Controllers;
using TriviaBackend.Exceptions;
using TriviaBackend.Models.Entities;
using TriviaBackend.Models.Records.Leaderboard;
using TriviaBackend.Services.Interfaces.DB;

namespace TriviaBackendTests.Integration
{
    [TestFixture]
    public class LeaderboardControllerTests
    {
        private Mock<IPlayerService> _playerServiceMock = null!;
        private Mock<ILogger<ExceptionHandler>> _loggerMock = null!;
        private LeaderboardController _controller = null!;

        [SetUp]
        public void Setup()
        {
            _playerServiceMock = new Mock<IPlayerService>();
            _loggerMock = new Mock<ILogger<ExceptionHandler>>();
            _controller = new LeaderboardController(_playerServiceMock.Object, _loggerMock.Object);
        }

        [Test]
        public async Task GetGlobalLeaderboard_ReturnsTopPlayers()
        {
            var players = new List<Player>
            {
                new() { Username = "A", Elo = 1500, TotalPoints = 500, GamesPlayed = 10 },
                new() { Username = "B", Elo = 1200, TotalPoints = 300, GamesPlayed = 5 }
            };

            _playerServiceMock.Setup(s => s.GetAllPlayersAsync()).ReturnsAsync(players);

            var result = await _controller.GetGlobalLeaderboard(2);

            var okResult = result.Result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);

            var leaderboard = okResult!.Value as IEnumerable<GlobalLeaderboardEntry>;
            Assert.That(leaderboard!.Count(), Is.EqualTo(2));

            var first = leaderboard!.First();
            Assert.That(first.Username, Is.EqualTo("A"));
            Assert.That(first.Rank, Is.EqualTo(1));
            Assert.That(first.Elo, Is.EqualTo(1500));
        }

        [Test]
        public async Task GetPlayerRank_ReturnsCorrectRank()
        {
            var players = new List<Player>
            {
                new() { Username = "A", Elo = 1500 },
                new() { Username = "B", Elo = 1200 },
                new() { Username = "C", Elo = 1300 }
            };

            _playerServiceMock.Setup(s => s.GetAllPlayersAsync()).ReturnsAsync(players);
            _playerServiceMock.Setup(s => s.GetPlayerByUsernameAsync("C")).ReturnsAsync(players[2]);

            var result = await _controller.GetPlayerRank("C");

            var okResult = result.Result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);

            var rankInfo = okResult!.Value as PlayerRankInfo;
            Assert.That(rankInfo!.Username, Is.EqualTo("C"));
            Assert.That(rankInfo.Rank, Is.EqualTo(2)); // Order by elo desc
            Assert.That(rankInfo.Elo, Is.EqualTo(1300));
            Assert.That(rankInfo.TotalPlayers, Is.EqualTo(3));
        }

        [Test]
        public async Task GetPlayerRank_ReturnsNotFound_WhenPlayerDoesNotExist()
        {
            _playerServiceMock.Setup(s => s.GetPlayerByUsernameAsync("NonExistent")).ReturnsAsync((Player?)null);

            var result = await _controller.GetPlayerRank("NonExistent");

            Assert.That(result.Result, Is.TypeOf<NotFoundObjectResult>());
        }

        [Test]
        public async Task UpdatePlayerStats_ReturnsOk_WhenPlayerExists()
        {
            var player = new Player() { Username = "A", Elo = 1000, GamesPlayed = 5, TotalPoints = 200 };
            _playerServiceMock.Setup(s => s.GetPlayerByUsernameAsync("A")).ReturnsAsync(player);
            _playerServiceMock.Setup(s => s.UpdatePlayerAsync(It.IsAny<Player>())).Returns(Task.CompletedTask);

            var statsUpdate = new PlayerStatsUpdate("A", 50, 10);

            var result = await _controller.UpdatePlayerStats(statsUpdate);

            Assert.That(result, Is.TypeOf<OkObjectResult>());

            var okResult = result as OkObjectResult;
            var updatedStats = okResult!.Value as PlayerStatsUpdateResult;
            Assert.That(updatedStats!.Username, Is.EqualTo("A"));
            Assert.That(updatedStats.NewElo, Is.EqualTo(1050));
            Assert.That(updatedStats.TotalGames, Is.EqualTo(6));
        }

        [Test]
        public async Task UpdatePlayerStats_ReturnsNotFound_WhenPlayerDoesNotExist()
        {
            _playerServiceMock.Setup(s => s.GetPlayerByUsernameAsync("Unknown")).ReturnsAsync((Player?)null);

            var statsUpdate = new PlayerStatsUpdate { Username = "Unknown", EloChange = 50 };

            var result = await _controller.UpdatePlayerStats(statsUpdate);

            Assert.That(result, Is.TypeOf<NotFoundObjectResult>());
        }

        [Test]
        public async Task GetLeaderboardStatistics_ReturnsCorrectStats()
        {
            var players = new List<Player>
            {
                new() { Username = "A", TotalPoints = 200, GamesPlayed = 5 },
                new() { Username = "B", TotalPoints = 300, GamesPlayed = 10 }
            };
            _playerServiceMock.Setup(s => s.GetAllPlayersAsync()).ReturnsAsync(players);

            var actionResult = await _controller.GetLeaderboardStatistics();
            var okResult = actionResult.Result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);

            var stats = okResult!.Value as LeaderboardStatistics;
            Assert.That(stats!.AveragePoints, Is.EqualTo(250));
            Assert.That(stats.TotalPoints, Is.EqualTo(500));
            Assert.That(stats.TopPlayer, Is.EqualTo("B"));
            Assert.That(stats.PlayerCount, Is.EqualTo(2));
        }

        [Test]
        public async Task GetLeaderboardStatistics_NoPlayers_ReturnsEmptyValues()
        {
            var players = new List<Player>();
            _playerServiceMock.Setup(s => s.GetAllPlayersAsync()).ReturnsAsync(players);

            var actionResult = await _controller.GetLeaderboardStatistics();
            var okResult = actionResult.Result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);

            var stats = okResult!.Value as LeaderboardStatistics;
            Assert.That(stats!.AveragePoints, Is.EqualTo(0));
            Assert.That(stats.TotalPoints, Is.EqualTo(0));
            Assert.That(stats.TopPlayer, Is.Null);
            Assert.That(stats.PlayerCount, Is.EqualTo(0));
        }
    }
}
