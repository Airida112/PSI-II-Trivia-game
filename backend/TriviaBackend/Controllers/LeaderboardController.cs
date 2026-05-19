using Microsoft.AspNetCore.Mvc;
using TriviaBackend.Exceptions;
using TriviaBackend.Models.Entities;
using TriviaBackend.Services.Interfaces.DB;
using TriviaBackend.Services.Implementations;
using TriviaBackend.Models.Records.Leaderboard;

namespace TriviaBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LeaderboardController(IPlayerService _PlayerService, ILogger<ExceptionHandler> _logger) : ControllerBase
    {
        /// <summary>
        /// Get global ranking of players by elo
        /// </summary>
        /// <param name="top"></param>
        /// <returns></returns>
        [HttpGet("global")]
        public async Task<ActionResult<IEnumerable<GlobalLeaderboardEntry>>> GetGlobalLeaderboard([FromQuery] int top = 100)
        {
            var players = await _PlayerService.GetAllPlayersAsync();

            players.Sort();

            var leaderboard = players
                .Take(top)
                .Select((player, index) => new GlobalLeaderboardEntry(
                    Rank: index + 1,
                    Username: player.Username,
                    Elo: player.Elo,
                    TotalPoints: player.TotalPoints,
                    GamesPlayed: player.GamesPlayed,
                    JoinDate: player.Created
                ));

            return Ok(leaderboard);
        }

        /// <summary>
        /// Get the position of a player in the global leaderboard
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        [HttpGet("rank/{username}")]
        public async Task<ActionResult<PlayerRankInfo>> GetPlayerRank(string username)
        {
            var targetPlayer = await _PlayerService.GetPlayerByUsernameAsync(username);

            if (targetPlayer == null)
            {
                _logger.LogError("ERROR: Player not found");
                return NotFound("Player not found");
            }

            var allPlayers = await _PlayerService.GetAllPlayersAsync();

            allPlayers.Sort();

            var rank = allPlayers.FindIndex(p => p.Username == username) + 1;

            return Ok(new PlayerRankInfo
            (
                Username: targetPlayer.Username,
                Rank: rank,
                Elo: targetPlayer.Elo,
                GamesPlayed: targetPlayer.GamesPlayed,
                TotalPlayers: allPlayers.Count
            ));
        }

        /// <summary>
        /// Update the elo of a player and increase GamesPlayed
        /// </summary>
        /// <param name="statsUpdate"></param>
        /// <returns></returns>
        [HttpPost("update-stats")]
        public async Task<ActionResult> UpdatePlayerStats([FromBody] PlayerStatsUpdate statsUpdate)
        {
            var player = await _PlayerService.GetPlayerByUsernameAsync(statsUpdate.Username);

            if (player == null)
            {
                _logger.LogError("ERROR: Player not found");
                return NotFound("Player not found");
            }

            player.Elo += statsUpdate.EloChange;
            player.GamesPlayed++;

            try
            {
                await _PlayerService.UpdatePlayerAsync(player);
            }
            catch (Exception ex)
            {
                _logger.LogError($"ERROR updating player statistics: {ex.Message}");
                throw new PlayerStatsUpdateException("Error while updating player statistics");
            }

            return Ok(new PlayerStatsUpdateResult
            (
                Username: player.Username,
                NewElo: player.Elo,
                TotalGames: player.GamesPlayed
            ));
        }

        [HttpGet("statistics")]
        public async Task<ActionResult<LeaderboardStatistics>> GetLeaderboardStatistics()
        {
            var players = await _PlayerService.GetAllPlayersAsync();

            if (players == null || players.Count == 0)
            {
                return Ok(new LeaderboardStatistics
                (
                    AveragePoints: 0,
                    TotalPoints: 0,
                    TopPlayer: null,
                    PlayerCount: 0
                ));
            }

            var gamePlayers = players.Select(p => new GamePlayer
            {
                Id = 0, // Temporary ID for statistics
                Name = p.Username,
                CurrentGameScore = p.TotalPoints,
                CorrectAnswersInGame = p.GamesPlayed // Using GamesPlayed as approximation
            }).ToList();

            var statsCalculator = new StatisticsCalculator<GamePlayer, int>();

            var avgPoints = statsCalculator.CalculateAverage(gamePlayers, gp => gp.CurrentGameScore);
            var totalPoints = statsCalculator.CalculateTotal(gamePlayers, gp => gp.CurrentGameScore);
            var topPlayer = statsCalculator.FindTopPerformer(gamePlayers, gp => gp.CurrentGameScore);

            return Ok(new LeaderboardStatistics
            (
                AveragePoints: avgPoints,
                TotalPoints: totalPoints,
                TopPlayer: topPlayer?.Name,
                PlayerCount: players.Count
            ));
        }
    }
}