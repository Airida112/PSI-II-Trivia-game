using TriviaBackend.Exceptions;
using TriviaBackend.Models.Entities;
using TriviaBackend.Models.Enums;
using TriviaBackend.Models.Records;
using TriviaBackend.Services.Interfaces;

namespace TriviaBackend.Services.Implementations
{
    /// <summary>
    /// Service for processing actions and events in a match. Works together with GameHub
    /// </summary>
    public class GameEngineService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ExceptionHandler> _logger;
        private List<GamePlayer> _players = new List<GamePlayer>();
        private List<Team> _teams = new List<Team>();
        private Queue<TriviaQuestion> _gameQuestions = new Queue<TriviaQuestion>();
        private Dictionary<int, List<GameAnswer>> _gameAnswers = new Dictionary<int, List<GameAnswer>>();
        private TriviaQuestion? _currentQuestion;
        private DateTime _questionStartTime;
        private GameSettings _settings;

        public GameStatus Status { get; private set; } = GameStatus.Waiting;
        public int CurrentQuestionNumber { get; private set; } = 0;
        public string GameId { get; private set; }
        public TriviaQuestion? CurrentQuestion => _currentQuestion;

        public TimeSpan TimeRemaining => _currentQuestion != null ?
            TimeSpan.FromSeconds(_currentQuestion.TimeLimit) - (DateTime.Now - _questionStartTime) : TimeSpan.Zero;

        // Traditional constructor
        public GameEngineService(
            IServiceProvider serviceProvider,
            ILogger<ExceptionHandler> logger,
            GameSettings settings = default,
            string? gameId = null)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _settings = settings.MaxPlayers == 0 ? new GameSettings() : settings;
            GameId = gameId ?? Guid.NewGuid().ToString();

            if(_settings.IsTeamMode)
            {
                InitializeTeams();
            }
        }

        public GameSettings GetSettings() => _settings;

        private void InitializeTeams()
        {
            _teams.Clear();
            var teamNames = new[] { "Red", "Blue", "Green", "Yellow" };

            for (int i = 0; i < _settings.NumberOfTeams; i++)
            {
                _teams.Add(new Team
                {
                    Id = i + 1,
                    Name = teamNames[i]
                });
            }
        }

        public bool AddPlayer(string playerName, int? playerId = null, DateTime? joinTime = null, int? teamId = null)
        {
            if (_players.Count >= _settings.MaxPlayers)
                return false;

            if (Status == GameStatus.InProgress && !_settings.AllowLateJoining)
                return false;

            var player = new GamePlayer
            {
                Id = playerId ?? GeneratePlayerId(),
                Name = playerName,
                JoinedGameAt = joinTime ?? DateTime.Now,
                CurrentGameScore = 0,
                CorrectAnswersInGame = 0,
                IsActive = true
            };

            _players.Add(player);
            _gameAnswers[player.Id] = new List<GameAnswer>();

            if(_settings.IsTeamMode)
            {
                AssignPlayerToTeam(player, teamId);
            }

            return true;
        }

        private void AssignPlayerToTeam(GamePlayer player, int? preferredTeamId = null)
        {
            Team? targetTeam = null;

            if (preferredTeamId.HasValue)
            {
                targetTeam = _teams.FirstOrDefault(t => t.Id == preferredTeamId.Value);
            }

            // Auto-assign to smallest team if no preference or team not found
            if (targetTeam == null)
            {
                targetTeam = _teams.OrderBy(t => t.Members.Count).First();
            }

            targetTeam.AddMember(player);
        }

        public bool AssignPlayerToSpecificTeam(int playerId, int teamId)
        {
            if (!_settings.IsTeamMode || Status != GameStatus.Waiting)
                return false;

            var player = _players.FirstOrDefault(p => p.Id == playerId);
            if (player == null)
                return false;

            var newTeam = _teams.FirstOrDefault(t => t.Id == teamId);
            if (newTeam == null)
                return false;

            // Remove from current team
            foreach (var team in _teams)
            {
                team.RemoveMember(player);
            }

            // Add to new team
            newTeam.AddMember(player);
            return true;
        }

        public List<Team> GetTeams() => _teams.ToList();

        public bool StartGame(QuestionCategory[]? categories = null, DifficultyLevel? maxDifficulty = null)
        {
            if (_players.Count == 0 || Status != GameStatus.Waiting)
            {
                _logger.LogError($"Cannot start game - Players: {_players.Count}, Status: {Status}");
                return false;
            }

            try
            {
                _logger.LogInformation("=== GameEngineService.StartGame called ===");

                var categoriesToUse = categories ?? _settings.QuestionCategories;
                var difficultyToUse = maxDifficulty ?? _settings.MaxDifficulty;

                _logger.LogInformation("Creating scope for IQuestionService...");

                // Create a scope and resolve IQuestionService
                using var scope = _serviceProvider.CreateScope();
                _logger.LogInformation("Scope created successfully");

                var questionService = scope.ServiceProvider.GetRequiredService<IQuestionService>();
                _logger.LogInformation("IQuestionService resolved successfully");

                _logger.LogInformation($"Calling GetQuestions with {categoriesToUse.Length} categories, difficulty {difficultyToUse}, count {_settings.QuestionsPerGame}");

                var questions = questionService.GetQuestions(categoriesToUse, difficultyToUse, _settings.QuestionsPerGame);

                _logger.LogInformation($"Requested {_settings.QuestionsPerGame} questions, got {questions?.Count ?? 0} questions");
                _logger.LogInformation($"Categories: {string.Join(", ", categoriesToUse.Select(c => c.ToString()))}");
                _logger.LogInformation($"Max Difficulty: {difficultyToUse}");

                if (questions == null || questions.Count == 0)
                {
                    _logger.LogError("No questions returned from QuestionService");
                    return false;
                }

                _gameQuestions.Clear();
                foreach (var question in questions)
                {
                    _gameQuestions.Enqueue(question);
                    _logger.LogInformation($"Enqueued question {question.Id}: {question.QuestionText} ({question.Category}, {question.Difficulty})");
                }

                Status = GameStatus.InProgress;
                CurrentQuestionNumber = 0;

                _logger.LogInformation("Calling NextQuestion...");
                NextQuestion();

                _logger.LogInformation("Game started successfully!");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"ERROR starting game: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");
                _logger.LogError($"Inner exception: {ex.InnerException?.Message}");
                throw new StartGameException("Game could not be started");
            }
        }

        public bool NextQuestion()
        {
            if (CurrentQuestionNumber > 0 && DateTime.Now - _questionStartTime < TimeSpan.FromSeconds(5))
                return true;

            if (_gameQuestions.Count == 0)
            {
                EndGame();
                return false;
            }

            _currentQuestion = _gameQuestions.Dequeue();
            _questionStartTime = DateTime.Now;
            CurrentQuestionNumber++;
            return true;
        }

        public AnswerResult SubmitAnswer(int playerId, int selectedAnswer)
        {
            if (_currentQuestion == null || Status != GameStatus.InProgress)
                return AnswerResult.TimeUp;

            var player = _players.FirstOrDefault(p => p.Id == playerId);
            if (player == null) return AnswerResult.TimeUp;

            var submissionTime = DateTime.Now;

            if (submissionTime - _questionStartTime > TimeSpan.FromSeconds(_currentQuestion.TimeLimit))
            {
                return AnswerResult.TimeUp;
            }

            bool isCorrect = selectedAnswer == _currentQuestion.CorrectAnswerIndex;

            var timeBonus = isCorrect ? Math.Max(0, _currentQuestion.TimeLimit - (int)(submissionTime - _questionStartTime).TotalSeconds) : 0;
            var pointsEarned = isCorrect ? _currentQuestion.Points + timeBonus : 0;

            player.CurrentGameScore += pointsEarned;
            if (isCorrect) player.CorrectAnswersInGame++;

            // Update team score if in team mode
            if (_settings.IsTeamMode)
            {
                var team = _teams.FirstOrDefault(t => t.Members.Any(m => m.Id == playerId));
                if (team != null)
                {
                    team.UpdateScore(pointsEarned);
                    if (isCorrect) team.CorrectAnswers++;
                }
            }

            var answer = new GameAnswer(playerId, _currentQuestion.Id, selectedAnswer, submissionTime);
            _gameAnswers[playerId].Add(answer);

            return isCorrect ? AnswerResult.Correct : AnswerResult.Incorrect;
        }

        public List<GamePlayer> GetCurrentGameLeaderboard()
        {
            var activePlayers = _players.Where(p => p.IsActive).ToList();
            activePlayers.Sort();
            return activePlayers;
        }

        public List<Team> GetTeamLeaderboard()
        {
            return _teams.OrderByDescending(t => t.TotalScore)
                        .ThenByDescending(t => t.CorrectAnswers)
                        .ToList();
        }

        public void EndGame()
        {
            Status = GameStatus.Finished;
            _currentQuestion = null;
        }

        public List<GamePlayer> GetPlayers()
        {
            return _players.ToList();
        }

        public Dictionary<int, List<GameAnswer>> GetGameAnswers()
        {
            return _gameAnswers.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToList());
        }

        private int GeneratePlayerId()
        {
            return _players.Count > 0 ? _players.Max(p => p.Id) + 1 : 1;
        }

        public bool AllPlayersAnswered()
        {
            if (_currentQuestion == null) return false;

            return _players.Where(p => p.IsActive)
                          .All(p => _gameAnswers[p.Id].Any(a => a.QuestionId == _currentQuestion.Id));
        }
    }
}