using TriviaBackend.Models.Entities;
using TriviaBackend.Services.Implementations;

namespace TriviaBackendTests.Unit
{
    [TestFixture]
    public class StatisticsCalculatorTests
    {
        private StatisticsCalculator<GamePlayer, int> _intCalculator = null!;
        private StatisticsCalculator<GamePlayer, double> _doubleCalculator = null!;
        private StatisticsCalculator<GamePlayer, decimal> _decimalCalculator = null!;
        private List<GamePlayer> _players = null!;

        [SetUp]
        public void Setup()
        {
            _intCalculator = new StatisticsCalculator<GamePlayer, int>();
            _doubleCalculator = new StatisticsCalculator<GamePlayer, double>();
            _decimalCalculator = new StatisticsCalculator<GamePlayer, decimal>();

            _players = new List<GamePlayer>
            {
                new() { Id = 1, Name = "Alice", CurrentGameScore = 100, CorrectAnswersInGame = 8 },
                new() { Id = 2, Name = "Bob", CurrentGameScore = 150, CorrectAnswersInGame = 9 },
                new() { Id = 3, Name = "Charlie", CurrentGameScore = 200, CorrectAnswersInGame = 10 }
            };
        }

        [Test]
        public void CalculateAverage_IntType_ReturnsCorrectAverage()
        {
            var result = _intCalculator.CalculateAverage(_players, p => p.CurrentGameScore);
            Assert.That(result, Is.EqualTo(150)); // (100 + 150 + 200) / 3 = 150
        }

        [Test]
        public void CalculateAverage_DoubleType_ReturnsCorrectAverage()
        {
            var result = _doubleCalculator.CalculateAverage(_players, p => (double)p.CurrentGameScore);
            Assert.That(result, Is.EqualTo(150.0).Within(0.01));
        }

        [Test]
        public void CalculateAverage_DecimalType_ReturnsCorrectAverage()
        {
            var result = _decimalCalculator.CalculateAverage(_players, p => (decimal)p.CurrentGameScore);
            Assert.That(result, Is.EqualTo(150m));
        }

        [Test]
        public void CalculateAverage_EmptyList_ReturnsDefault()
        {
            var emptyList = new List<GamePlayer>();
            var result = _intCalculator.CalculateAverage(emptyList, p => p.CurrentGameScore);
            Assert.That(result, Is.EqualTo(0));
        }

        [Test]
        public void CalculateAverage_NullList_ReturnsDefault()
        {
            var result = _intCalculator.CalculateAverage(null!, p => p.CurrentGameScore);
            Assert.That(result, Is.EqualTo(0));
        }

        [Test]
        public void CalculateTotal_IntType_ReturnsCorrectSum()
        {
            var result = _intCalculator.CalculateTotal(_players, p => p.CurrentGameScore);
            Assert.That(result, Is.EqualTo(450)); // 100 + 150 + 200
        }

        [Test]
        public void CalculateTotal_DoubleType_ReturnsCorrectSum()
        {
            var result = _doubleCalculator.CalculateTotal(_players, p => (double)p.CurrentGameScore);
            Assert.That(result, Is.EqualTo(450.0).Within(0.01));
        }

        [Test]
        public void CalculateTotal_DecimalType_ReturnsCorrectSum()
        {
            var result = _decimalCalculator.CalculateTotal(_players, p => (decimal)p.CurrentGameScore);
            Assert.That(result, Is.EqualTo(450m));
        }

        [Test]
        public void CalculateTotal_EmptyList_ReturnsDefault()
        {
            var emptyList = new List<GamePlayer>();
            var result = _intCalculator.CalculateTotal(emptyList, p => p.CurrentGameScore);
            Assert.That(result, Is.EqualTo(0));
        }

        [Test]
        public void CalculateTotal_NullList_ReturnsDefault()
        {
            var result = _intCalculator.CalculateTotal(null!, p => p.CurrentGameScore);
            Assert.That(result, Is.EqualTo(0));
        }

        [Test]
        public void FindTopPerformer_ReturnsPlayerWithHighestScore()
        {
            var result = _intCalculator.FindTopPerformer(_players, p => p.CurrentGameScore);
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Name, Is.EqualTo("Charlie"));
            Assert.That(result.CurrentGameScore, Is.EqualTo(200));
        }

        [Test]
        public void FindTopPerformer_ByCorrectAnswers_ReturnsCorrectPlayer()
        {
            var result = _intCalculator.FindTopPerformer(_players, p => p.CorrectAnswersInGame);
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Name, Is.EqualTo("Charlie"));
            Assert.That(result.CorrectAnswersInGame, Is.EqualTo(10));
        }

        [Test]
        public void FindTopPerformer_EmptyList_ReturnsNull()
        {
            var emptyList = new List<GamePlayer>();
            var result = _intCalculator.FindTopPerformer(emptyList, p => p.CurrentGameScore);
            Assert.That(result, Is.Null);
        }

        [Test]
        public void FindTopPerformer_NullList_ReturnsNull()
        {
            var result = _intCalculator.FindTopPerformer(null!, p => p.CurrentGameScore);
            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetTopN_ReturnsCorrectNumberOfPlayers()
        {
            var result = _intCalculator.GetTopN(_players, p => p.CurrentGameScore, 2);
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result[0].Name, Is.EqualTo("Charlie"));
            Assert.That(result[1].Name, Is.EqualTo("Bob"));
        }

        [Test]
        public void GetTopN_RequestMoreThanAvailable_ReturnsAllPlayers()
        {
            var result = _intCalculator.GetTopN(_players, p => p.CurrentGameScore, 10);
            Assert.That(result.Count, Is.EqualTo(3));
        }

        [Test]
        public void GetTopN_ZeroOrNegative_ReturnsEmptyList()
        {
            var result1 = _intCalculator.GetTopN(_players, p => p.CurrentGameScore, 0);
            var result2 = _intCalculator.GetTopN(_players, p => p.CurrentGameScore, -1);
            
            Assert.That(result1.Count, Is.EqualTo(0));
            Assert.That(result2.Count, Is.EqualTo(0));
        }

        [Test]
        public void GetTopN_EmptyList_ReturnsEmptyList()
        {
            var emptyList = new List<GamePlayer>();
            var result = _intCalculator.GetTopN(emptyList, p => p.CurrentGameScore, 5);
            Assert.That(result.Count, Is.EqualTo(0));
        }

        [Test]
        public void GetTopN_NullList_ReturnsEmptyList()
        {
            var result = _intCalculator.GetTopN(null!, p => p.CurrentGameScore, 5);
            Assert.That(result.Count, Is.EqualTo(0));
        }

        [Test]
        public void GetPlayerStatistics_IntType_ReturnsCorrectStats()
        {
            var player = _players[0];
            var stats = _intCalculator.GetPlayerStatistics(player);

            Assert.That(stats.ContainsKey("Score"), Is.True);
            Assert.That(stats.ContainsKey("CorrectAnswers"), Is.True);
            Assert.That(stats["Score"], Is.EqualTo(100));
            Assert.That(stats["CorrectAnswers"], Is.EqualTo(8));
        }

        [Test]
        public void GetPlayerStatistics_DoubleType_ReturnsStatsWithAccuracy()
        {
            var player = _players[1];
            var stats = _doubleCalculator.GetPlayerStatistics(player);

            Assert.That(stats.ContainsKey("Score"), Is.True);
            Assert.That(stats.ContainsKey("CorrectAnswers"), Is.True);
            Assert.That(stats.ContainsKey("AccuracyPercentage"), Is.True);
            
            Assert.That(stats["Score"], Is.EqualTo(150.0).Within(0.01));
            Assert.That(stats["CorrectAnswers"], Is.EqualTo(9.0).Within(0.01));
            Assert.That(stats["AccuracyPercentage"], Is.EqualTo(90.0).Within(0.01)); // 9/10 * 100
        }

        [Test]
        public void GetPlayerStatistics_DoubleType_PlayerWithZeroAnswers_NoAccuracy()
        {
            var player = new GamePlayer
            {
                Id = 4,
                Name = "David",
                CurrentGameScore = 0,
                CorrectAnswersInGame = 0
            };

            var stats = _doubleCalculator.GetPlayerStatistics(player);

            Assert.That(stats.ContainsKey("Score"), Is.True);
            Assert.That(stats.ContainsKey("CorrectAnswers"), Is.True);
            Assert.That(stats.ContainsKey("AccuracyPercentage"), Is.False);
        }

        [Test]
        public void CalculateAverage_CorrectAnswers_ReturnsCorrectValue()
        {
            var result = _intCalculator.CalculateAverage(_players, p => p.CorrectAnswersInGame);
            Assert.That(result, Is.EqualTo(9)); // (8 + 9 + 10) / 3 = 9
        }

        [Test]
        public void CalculateTotal_CorrectAnswers_ReturnsCorrectSum()
        {
            var result = _intCalculator.CalculateTotal(_players, p => p.CorrectAnswersInGame);
            Assert.That(result, Is.EqualTo(27)); // 8 + 9 + 10
        }
    }
}
