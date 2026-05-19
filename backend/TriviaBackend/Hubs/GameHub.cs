using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TriviaBackend.Data;
using TriviaBackend.Exceptions;
using TriviaBackend.Models.Entities;
using TriviaBackend.Models.Enums;
using TriviaBackend.Models.Records;
using TriviaBackend.Services.Implementations;
using TriviaBackend.Services.Interfaces;
using TriviaBackend.Services.Interfaces.DB;
using System.Collections.Concurrent;

namespace TriviaBackend.Hubs
{
    /// <summary>
    /// Manages player activity and game progression in a match.
    /// </summary>
    public class GameHub : Hub
    {
        private static readonly ConcurrentDictionary<string, GameEngineService> _activeGames = new();
        private static readonly ConcurrentDictionary<string, string> _playerGameMap = new();
        private static readonly ConcurrentDictionary<string, CancellationTokenSource> _gameTimers = new();
        private static readonly ConcurrentDictionary<string, bool> _questionRevealed = new();
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<int, string>> _gamePlayerUsernames = new();

        private static IHubContext<GameHub>? _staticHubContext;
        private static IServiceProvider? _staticServiceProvider;

        private readonly ILogger<ExceptionHandler> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IPresenceService _presenceService;
        private readonly IFriendshipService _friendshipService;

        public GameHub(
            IServiceProvider serviceProvider,
            ILogger<ExceptionHandler> logger,
            IPresenceService presenceService,
            IFriendshipService friendshipService)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _presenceService = presenceService;
            _friendshipService = friendshipService;
        }

        public static void SetHubContext(IHubContext<GameHub> hubContext) => _staticHubContext = hubContext;
        public static void SetServiceProvider(IServiceProvider serviceProvider) => _staticServiceProvider = serviceProvider;

        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            if (!string.IsNullOrEmpty(userId))
            {
                _presenceService.SetOnline(userId, Context.ConnectionId);
                await NotifyFriendsOfStatusChange(userId);
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation($"=== OnDisconnectedAsync called for connection {Context.ConnectionId} ===");

            var disconnectingUserId = _presenceService.GetUserIdByConnection(Context.ConnectionId);
            _presenceService.SetOffline(Context.ConnectionId);
            if (!string.IsNullOrEmpty(disconnectingUserId))
                await NotifyFriendsOfStatusChange(disconnectingUserId);

            if (_playerGameMap.TryGetValue(Context.ConnectionId, out var gameId))
            {
                _logger.LogInformation($"Connection {Context.ConnectionId} was in game {gameId}");

                if (_activeGames.TryGetValue(gameId, out var gameEngine))
                {
                    var players = gameEngine.GetPlayers();
                    _logger.LogInformation($"Game {gameId} still active with {players.Count} players");

                    await Clients.Group(gameId).SendAsync("PlayerDisconnected", Context.ConnectionId);

                    if (gameEngine.Status == GameStatus.Waiting && players.Count == 1)
                    {
                        _logger.LogInformation($"Last player disconnected from waiting game {gameId}, cleaning up");

                        if (_gameTimers.TryRemove(gameId, out var cts))
                        {
                            cts.Cancel();
                            cts.Dispose();
                        }

                        _activeGames.TryRemove(gameId, out _);
                        _gamePlayerUsernames.TryRemove(gameId, out _);
                    }
                }

                _playerGameMap.TryRemove(Context.ConnectionId, out _);
            }
            else
            {
                _logger.LogInformation($"Connection {Context.ConnectionId} was not in any game");
            }

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Returns the caller's friend list with live status.
        /// </summary>
        public async Task GetFriendList(string userId)
        {
            var friends = await _friendshipService.GetFriendsAsync(userId);
            await Clients.Caller.SendAsync("FriendListUpdated", friends);
        }

        /// <summary>
        /// Sends a game lobby invite to an online friend.
        /// </summary>
        public async Task SendGameInvite(string gameId, string inviterUserId, string friendUserId)
        {
            if (!_activeGames.ContainsKey(gameId))
            {
                await Clients.Caller.SendAsync("Error", "Game not found");
                return;
            }

            var friends = await _friendshipService.GetFriendsAsync(inviterUserId);
            var target = friends.FirstOrDefault(f => f.UserId == friendUserId);

            if (target == null)
            {
                await Clients.Caller.SendAsync("Error", "Not friends with this user");
                return;
            }

            if (target.Status != PlayerStatus.Online)
            {
                await Clients.Caller.SendAsync("Error", "Friend is not online or is already in a game");
                return;
            }

            await Clients.User(friendUserId)
                .SendAsync("GameInviteReceived", new GameInvitePayload(inviterUserId, gameId));
        }

        /// <summary>
        /// Create a new game with a random Id and initialize it in lobby state.
        /// </summary>
        public async Task CreateGame(string playerName)
        {
            _logger.LogInformation("=== CreateGame called ===");
            var gameId = Guid.NewGuid().ToString()[..6].ToUpper();
            _logger.LogInformation($"Generated gameId: {gameId}");

            var setting = new GameSettings
            {
                MaxPlayers = 10,
                QuestionsPerGame = 10,
                DefaultTimeLimit = 30,
                QuestionCategories = Enum.GetValues<QuestionCategory>(),
                MaxDifficulty = DifficultyLevel.Hard,
                IsTeamMode = false,
                NumberOfTeams = 2
            };

            var gameEngine = new GameEngineService(_staticServiceProvider!, _logger, setting, gameId);

            if (!_activeGames.TryAdd(gameId, gameEngine))
            {
                await Clients.Caller.SendAsync("Error", "Failed to create game with this ID");
                return;
            }

            _gamePlayerUsernames.TryAdd(gameId, new ConcurrentDictionary<int, string>());

            var creatorUserId = Context.UserIdentifier;
            if (!string.IsNullOrEmpty(creatorUserId))
            {
                _presenceService.SetInGame(creatorUserId, gameId);
                await NotifyFriendsOfStatusChange(creatorUserId);
            }

            var playerId = await JoinGameInternalForCreator(gameId, playerName, gameEngine);

            if (_gamePlayerUsernames.TryGetValue(gameId, out var playerDict))
                playerDict.TryAdd(playerId, playerName);

            var allCategories = Enum.GetValues<QuestionCategory>().Select(c => c.ToString()).ToArray();
            var allDifficulties = Enum.GetValues<DifficultyLevel>().Select(d => d.ToString()).ToArray();

            await Clients.Caller.SendAsync("GameCreated", new
            {
                gameId,
                playerId,
                playerName,
                settings = new
                {
                    maxPlayers = setting.MaxPlayers,
                    questionsPerGame = setting.QuestionsPerGame,
                    defaultTimeLimit = setting.DefaultTimeLimit,
                    questionCategories = setting.QuestionCategories.Select(c => c.ToString()).ToArray(),
                    maxDifficulty = setting.MaxDifficulty.ToString(),
                    isTeamMode = setting.IsTeamMode,
                    numberOfTeams = setting.NumberOfTeams
                },
                availableCategories = allCategories,
                availableDifficulties = allDifficulties,
                teams = (object?)null
            });
        }

        /// <summary>
        /// Assigns a player to a specific team before the game starts.
        /// </summary>
        public async Task AssignPlayerToTeam(string gameId, int playerId, int teamId)
        {
            if (!_activeGames.TryGetValue(gameId, out var gameEngine))
            {
                await Clients.Caller.SendAsync("Error", "Game not found");
                return;
            }

            if (!gameEngine.GetSettings().IsTeamMode)
            {
                await Clients.Caller.SendAsync("Error", "Game is not in team mode");
                return;
            }

            if (gameEngine.Status != GameStatus.Waiting)
            {
                await Clients.Caller.SendAsync("Error", "Cannot change teams after game has started");
                return;
            }

            if (!gameEngine.AssignPlayerToSpecificTeam(playerId, teamId))
            {
                await Clients.Caller.SendAsync("Error", "Failed to assign player to team");
                return;
            }

            var teams = gameEngine.GetTeams().Select(t => new
            {
                t.Id,
                t.Name,
                memberCount = t.Members.Count,
                members = t.Members.Select(m => new { m.Id, m.Name })
            });

            await Clients.Group(gameId).SendAsync("TeamsUpdated", new { teams });
        }

        /// <summary>
        /// Updates game settings while the lobby is still in the waiting state.
        /// </summary>
        public async Task UpdateGameSettings(string gameId, int? maxPlayers = null, int? questionsPerGame = null,
            string[]? categories = null, string? maxDifficulty = null, bool? isTeamMode = null, int? numberOfTeams = null)
        {
            try
            {
                if (!_activeGames.TryGetValue(gameId, out var gameEngine))
                {
                    await Clients.Caller.SendAsync("Error", "Game not found");
                    return;
                }

                if (gameEngine.Status != GameStatus.Waiting)
                {
                    await Clients.Caller.SendAsync("Error", "Cannot update settings after game has started");
                    return;
                }

                var existingSettings = gameEngine.GetSettings();
                var currentPlayers = gameEngine.GetPlayers();

                var currentSettings = new GameSettings
                {
                    MaxPlayers = maxPlayers ?? existingSettings.MaxPlayers,
                    QuestionsPerGame = questionsPerGame ?? existingSettings.QuestionsPerGame,
                    DefaultTimeLimit = existingSettings.DefaultTimeLimit,
                    IsTeamMode = isTeamMode ?? existingSettings.IsTeamMode,
                    NumberOfTeams = numberOfTeams ?? existingSettings.NumberOfTeams
                };

                if (currentSettings.IsTeamMode && currentSettings.NumberOfTeams < 2)
                    currentSettings.NumberOfTeams = 2;
                if (currentSettings.NumberOfTeams > 6)
                    currentSettings.NumberOfTeams = 6;

                if (categories != null && categories.Length > 0)
                {
                    currentSettings.QuestionCategories = categories
                        .Select(c => Enum.TryParse<QuestionCategory>(c, true, out var cat) ? cat : (QuestionCategory?)null)
                        .Where(c => c.HasValue)
                        .Select(c => c!.Value)
                        .ToArray();
                }
                else
                {
                    currentSettings.QuestionCategories = existingSettings.QuestionCategories;
                }

                if (!string.IsNullOrEmpty(maxDifficulty) && Enum.TryParse<DifficultyLevel>(maxDifficulty, true, out var difficulty))
                    currentSettings.MaxDifficulty = difficulty;
                else
                    currentSettings.MaxDifficulty = existingSettings.MaxDifficulty;

                var newGameEngine = new GameEngineService(_staticServiceProvider!, _logger, currentSettings, gameId);

                foreach (var player in currentPlayers)
                    newGameEngine.AddPlayer(player.Name, player.Id, player.JoinedGameAt);

                _activeGames.TryUpdate(gameId, newGameEngine, gameEngine);

                var teams = currentSettings.IsTeamMode
                    ? newGameEngine.GetTeams().Select(t => new
                    {
                        t.Id,
                        t.Name,
                        memberCount = t.Members.Count,
                        members = t.Members.Select(m => new { m.Id, m.Name })
                    })
                    : null;

                await Clients.Group(gameId).SendAsync("SettingsUpdated", new
                {
                    settings = new
                    {
                        maxPlayers = currentSettings.MaxPlayers,
                        questionsPerGame = currentSettings.QuestionsPerGame,
                        defaultTimeLimit = currentSettings.DefaultTimeLimit,
                        questionCategories = currentSettings.QuestionCategories.Select(c => c.ToString()).ToArray(),
                        maxDifficulty = currentSettings.MaxDifficulty.ToString(),
                        isTeamMode = currentSettings.IsTeamMode,
                        numberOfTeams = currentSettings.NumberOfTeams
                    },
                    teams
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"ERROR updating game settings: {ex.Message}");
                await Clients.Caller.SendAsync("Error", "Failed to update game settings");
            }
        }

        private async Task<int> JoinGameInternalForCreator(string gameId, string playerName, GameEngineService gameEngine)
        {
            var playerId = GeneratePlayerId(gameEngine);

            if (!gameEngine.AddPlayer(playerName, playerId))
            {
                await Clients.Caller.SendAsync("Error", "Could not join game");
                return -1;
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, gameId);
            _playerGameMap.TryAdd(Context.ConnectionId, gameId);

            var players = gameEngine.GetPlayers();
            await Clients.Group(gameId).SendAsync("PlayerJoined", new
            {
                playerId,
                playerName,
                players = players.Select(p => new { p.Id, p.Name, p.IsActive })
            });

            return playerId;
        }

        /// <summary>
        /// Joins an existing game.
        /// </summary>
        public async Task JoinGame(string gameId, string playerName)
        {
            if (!_activeGames.TryGetValue(gameId, out var gameEngine))
            {
                await Clients.Caller.SendAsync("Error", "Game not found");
                return;
            }

            var playerId = GeneratePlayerId(gameEngine);

            if (!gameEngine.AddPlayer(playerName, playerId))
            {
                await Clients.Caller.SendAsync("Error", "Game is full or already started");
                return;
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, gameId);
            _playerGameMap.TryAdd(Context.ConnectionId, gameId);

            if (_gamePlayerUsernames.TryGetValue(gameId, out var playerDict))
                playerDict.TryAdd(playerId, playerName);

            var joinerUserId = Context.UserIdentifier;
            if (!string.IsNullOrEmpty(joinerUserId))
            {
                _presenceService.SetInGame(joinerUserId, gameId);
                await NotifyFriendsOfStatusChange(joinerUserId);
            }

            var players = gameEngine.GetPlayers().Select(p => new { p.Id, p.Name }).ToList();

            await Clients.Caller.SendAsync("JoinedGame", new { gameId, playerId, players });
            await Clients.OthersInGroup(gameId).SendAsync("PlayerJoined", new { playerId, playerName, players });
        }

        /// <summary>
        /// Leaves the current game.
        /// </summary>
        public async Task LeaveGame()
        {
            if (_playerGameMap.TryGetValue(Context.ConnectionId, out var gameId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, gameId);
                _playerGameMap.TryRemove(Context.ConnectionId, out _);

                var leavingUserId = Context.UserIdentifier;
                if (!string.IsNullOrEmpty(leavingUserId))
                {
                    _presenceService.ClearGame(leavingUserId);
                    await NotifyFriendsOfStatusChange(leavingUserId);
                }

                if (_activeGames.TryGetValue(gameId, out var gameEngine))
                {
                    var players = gameEngine.GetPlayers().Select(p => new { p.Id, p.Name }).ToList();
                    await Clients.Group(gameId).SendAsync("PlayerLeft", new { connectionId = Context.ConnectionId, players });

                    if (gameEngine.Status == GameStatus.Waiting && players.Count == 0)
                    {
                        _activeGames.TryRemove(gameId, out _);
                        _gamePlayerUsernames.TryRemove(gameId, out _);
                    }
                }
            }
        }

        /// <summary>
        /// Starts the game.
        /// </summary>
        public async Task StartGame(string gameId, string[]? categories, string? difficulty)
        {
            try
            {
                _logger.LogInformation($"=== StartGame called with gameId: {gameId} ===");

                if (!_activeGames.TryGetValue(gameId, out var gameEngine))
                {
                    _logger.LogError($"ERROR: Game {gameId} not found in _activeGames");
                    await Clients.Caller.SendAsync("Error", "Game not found");
                    return;
                }

                _logger.LogInformation($"Game found. Status: {gameEngine.Status}, Players: {gameEngine.GetPlayers().Count}");

                using var scope = _serviceProvider.CreateScope();
                var questionService = scope.ServiceProvider.GetRequiredService<IQuestionService>();
                var allCategories = questionService.GetQuestionCountByCategory();
                _logger.LogInformation($"Total questions available: {allCategories.Values.Sum()}");

                QuestionCategory[]? selectedCategories = null;
                if (categories != null && categories.Length > 0)
                {
                    selectedCategories = categories
                        .Select(c => Enum.TryParse<QuestionCategory>(c, true, out var cat) ? cat : (QuestionCategory?)null)
                        .Where(c => c.HasValue)
                        .Select(c => c!.Value)
                        .ToArray();
                }

                DifficultyLevel? maxDifficulty = null;
                if (!string.IsNullOrEmpty(difficulty) && Enum.TryParse<DifficultyLevel>(difficulty, true, out var diff))
                    maxDifficulty = diff;

                if (gameEngine.StartGame(selectedCategories, maxDifficulty))
                {
                    _logger.LogInformation("Game started successfully!");
                    await Clients.Group(gameId).SendAsync("GameStarted");
                    await SendQuestion(gameId);
                }
                else
                {
                    _logger.LogError("ERROR: gameEngine.StartGame returned false");
                    await Clients.Caller.SendAsync("Error", "Failed to start game");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"ERROR starting game: {ex.Message}");
                throw new StartGameException("Game could not be started");
            }
        }

        /// <summary>
        /// Submits a player's answer to the current question.
        /// </summary>
        public async Task SubmitAnswer(string gameId, int playerId, int answer)
        {
            if (!_activeGames.TryGetValue(gameId, out var gameEngine))
            {
                await Clients.Caller.SendAsync("Error", "Game not found");
                return;
            }

            var question = gameEngine.CurrentQuestion;
            if (question == null)
            {
                await Clients.Caller.SendAsync("Error", "No current question");
                return;
            }

            var questionKey = $"{gameId}_{question.Id}";

            if (_questionRevealed.TryGetValue(questionKey, out var revealed) && revealed)
            {
                _logger.LogInformation($"Question already revealed for {questionKey}");
                await Clients.Caller.SendAsync("Error", "Question already completed");
                return;
            }

            var result = gameEngine.SubmitAnswer(playerId, answer);

            await Clients.Caller.SendAsync("AnswerResult", new
            {
                result = result.ToString(),
                earnedPoints = gameEngine.GetPlayers().FirstOrDefault(p => p.Id == playerId)?.CurrentGameScore ?? 0,
                correctAnswer = gameEngine.CurrentQuestion?.CorrectAnswerIndex ?? 0
            });

            if (gameEngine.AllPlayersAnswered())
            {
                if (_gameTimers.TryRemove(gameId, out var cts))
                {
                    cts.Cancel();
                    cts.Dispose();
                }

                await RevealAnswerAndProgress(gameId, 5000, gameEngine);
            }
        }

        private async Task SendQuestion(string gameId)
        {
            if (_staticHubContext == null)
            {
                _logger.LogError("ERROR: Hub context is null in SendQuestion!");
                return;
            }

            if (!_activeGames.TryGetValue(gameId, out var gameEngine))
                return;

            var question = gameEngine.CurrentQuestion;

            if (question == null)
            {
                _logger.LogError($"ERROR: No current question for game {gameId}");
                await EndGame(gameId);
                return;
            }

            var questionKey = $"{gameId}_{question.Id}";
            _questionRevealed.AddOrUpdate(questionKey, false, (key, oldValue) => false);

            if (_gameTimers.TryRemove(gameId, out var oldCts))
            {
                oldCts.Cancel();
                oldCts.Dispose();
            }

            _logger.LogInformation($"Sending question {gameEngine.CurrentQuestionNumber} (ID: {question.Id}) to game {gameId}");

            await _staticHubContext.Clients.Group(gameId).SendAsync("QuestionSent", new
            {
                questionNumber = gameEngine.CurrentQuestionNumber,
                questionText = question.QuestionText,
                answerOptions = question.AnswerOptions,
                category = question.Category.ToString(),
                difficulty = question.Difficulty.ToString(),
                timeLimit = question.TimeLimit,
                points = question.Points
            });

            var cancelTokenSource = new CancellationTokenSource();
            _gameTimers.TryAdd(gameId, cancelTokenSource);

            _ = Task.Run(async () =>
            {
                try
                {
                    _logger.LogInformation($"[TIMER] Started for {question.TimeLimit} seconds for game {gameId}");
                    await Task.Delay(TimeSpan.FromSeconds(question.TimeLimit), cancelTokenSource.Token);

                    if (!cancelTokenSource.Token.IsCancellationRequested && _staticHubContext != null)
                    {
                        _logger.LogInformation($"[TIMER] Time's up for game {gameId}!");
                        await _staticHubContext.Clients.Group(gameId).SendAsync("TimeUp");
                        await RevealAnswerAndProgress(gameId, 5000, gameEngine);
                    }
                }
                catch (TaskCanceledException)
                {
                    _logger.LogInformation($"[TIMER] Cancelled for game {gameId} - all players answered early");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"[TIMER] ERROR in timer for game {gameId}: {ex.Message}");
                    _logger.LogError($"[TIMER] Stack trace: {ex.StackTrace}");
                }
                finally
                {
                    if (_gameTimers.TryGetValue(gameId, out var storedCts) && storedCts == cancelTokenSource)
                        _gameTimers.TryRemove(gameId, out _);
                    cancelTokenSource.Dispose();
                }
            });
        }

        private async Task RevealAnswerAndProgress(string gameId, int autoProgressMilliseconds, GameEngineService gameEngine)
        {
            _logger.LogInformation($"=== RevealAnswerAndProgress called for game {gameId} ===");

            if (_staticHubContext == null)
            {
                _logger.LogError("ERROR: Hub context is null!");
                return;
            }

            var question = gameEngine.CurrentQuestion;
            if (question == null)
            {
                _logger.LogError($"ERROR: No current question in RevealAnswerAndProgress for game {gameId}");
                return;
            }

            var questionKey = $"{gameId}_{question.Id}";

            if (_questionRevealed.TryGetValue(questionKey, out var wasRevealed) && wasRevealed)
            {
                _logger.LogInformation($"Question {question.Id} already revealed for game {gameId}, skipping");
                return;
            }

            _questionRevealed[questionKey] = true;

            var leaderboard = gameEngine.GetCurrentGameLeaderboard();

            await _staticHubContext.Clients.Group(gameId).SendAsync("QuestionRevealed", new
            {
                correctAnswer = question.CorrectAnswerIndex,
                correctAnswerText = question.AnswerOptions[question.CorrectAnswerIndex],
                leaderboard = leaderboard.Select(p => new
                {
                    p.Id,
                    p.Name,
                    score = p.CurrentGameScore,
                    correctAnswers = p.CorrectAnswersInGame
                })
            });

            await Task.Delay(autoProgressMilliseconds);

            if (!gameEngine.NextQuestion())
                await EndGame(gameId);
            else
                await SendQuestion(gameId);
        }

        private async Task EndGame(string gameId)
        {
            _logger.LogInformation($"=== Ending game {gameId} ===");

            if (_staticHubContext == null)
            {
                _logger.LogError("ERROR: Hub context is null in EndGame!");
                return;
            }

            if (!_activeGames.TryGetValue(gameId, out var gameEngine))
                return;

            if (_gameTimers.TryRemove(gameId, out var cts))
            {
                cts.Cancel();
                cts.Dispose();
            }

            var keysToRemove = _questionRevealed.Keys.Where(k => k.StartsWith($"{gameId}_")).ToList();
            foreach (var key in keysToRemove)
                _questionRevealed.TryRemove(key, out _);

            gameEngine.EndGame();

            var finalLeaderboard = gameEngine.GetCurrentGameLeaderboard();

            try
            {
                await UpdatePlayerStats(gameId, finalLeaderboard);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to update player stats for game {gameId}: {ex.Message}");
            }

            await _staticHubContext.Clients.Group(gameId).SendAsync("GameEnded", new
            {
                leaderboard = finalLeaderboard.Select(p => new
                {
                    p.Id,
                    p.Name,
                    score = p.CurrentGameScore,
                    correctAnswers = p.CorrectAnswersInGame
                })
            });

            _activeGames.TryRemove(gameId, out _);
            _gamePlayerUsernames.TryRemove(gameId, out _);
            _logger.LogInformation($"Game {gameId} ended and removed from active games");
        }

        private async Task UpdatePlayerStats(string gameId, List<GamePlayer> finalLeaderboard)
        {
            if (_staticServiceProvider == null)
            {
                _logger.LogError("ERROR: Static service provider is null in UpdatePlayerStats!");
                return;
            }

            using var scope = _staticServiceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ITriviaDbContext>();

            try
            {
                _logger.LogInformation($"=== UpdatePlayerStats called for game {gameId} ===");

                if (!_gamePlayerUsernames.TryGetValue(gameId, out var playerUsernames))
                {
                    _logger.LogError($"ERROR: No player usernames found for game {gameId}");
                    return;
                }

                foreach (var gamePlayer in finalLeaderboard)
                {
                    if (!playerUsernames.TryGetValue(gamePlayer.Id, out var username))
                    {
                        _logger.LogError($"ERROR: No username found for player ID {gamePlayer.Id}");
                        continue;
                    }

                    var player = await dbContext.Users
                        .OfType<Player>()
                        .FirstOrDefaultAsync(p => p.Username == username);

                    if (player == null)
                    {
                        _logger.LogError($"Player {username} not found");
                        throw new GameUpdateException($"Player {username} not found");
                    }

                    var eloChange = CalculateEloChange(gamePlayer, finalLeaderboard);
                    player.Elo += eloChange;
                    player.GamesPlayed++;
                    player.TotalPoints += gamePlayer.CurrentGameScore;
                }

                var changes = await dbContext.SaveChangesAsync();
                _logger.LogInformation($"Saved {changes} changes to database for game {gameId}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"ERROR updating player statistics: {ex.Message}");
                throw new PlayerStatsUpdateException("Error while updating player statistics");
            }
        }

        /// <summary>
        /// Calculates Elo change based on final position.
        /// </summary>
        /// <seealso href="https://en.wikipedia.org/wiki/Elo_rating_system"/>
        private static int CalculateEloChange(GamePlayer player, List<GamePlayer> leaderboard)
        {
            var position = leaderboard.FindIndex(p => p.Id == player.Id);
            var totalPlayers = leaderboard.Count;

            if (position == 0) return 25;
            if (position == 1) return 15;
            if (position == 2) return 10;
            if (position < totalPlayers / 2) return 5;
            return -5;
        }

        private static int GeneratePlayerId(GameEngineService gameEngine)
        {
            var existingIds = gameEngine.GetPlayers().Select(p => p.Id).ToHashSet();
            int playerId = 1;
            while (existingIds.Contains(playerId)) playerId++;
            return playerId;
        }

        /// <summary>
        /// Returns the count of available questions per category.
        /// </summary>
        public async Task GetAvailableCategories()
        {
            using var scope = _serviceProvider.CreateScope();
            var questionService = scope.ServiceProvider.GetRequiredService<IQuestionService>();
            var categories = questionService.GetQuestionCountByCategory();
            await Clients.Caller.SendAsync("AvailableCategories",
                categories.Select(c => new { category = c.Key.ToString(), count = c.Value }));
        }

        private async Task NotifyFriendsOfStatusChange(string userId)
        {
            if (_staticHubContext == null) return;
            try
            {
                var friends = await _friendshipService.GetFriendsAsync(userId);
                var status = _presenceService.GetStatus(userId);

                foreach (var friend in friends)
                {
                    await _staticHubContext.Clients.User(friend.UserId)
                        .SendAsync("FriendStatusChanged", new
                        {
                            userId,
                            status = status.ToString(),
                            gameId = _presenceService.GetActiveGameId(userId)
                        });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error notifying friends of status change: {ex.Message}");
            }
        }
    }
}