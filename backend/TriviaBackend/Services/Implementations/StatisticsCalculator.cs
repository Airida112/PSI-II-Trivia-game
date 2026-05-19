using TriviaBackend.Models.Entities;
using TriviaBackend.Services.Interfaces;

namespace TriviaBackend.Services.Implementations
{
    ///<summary>
    /// Generic statistics calculator implementation
    ///</summary>
    ///<typeparam name="T">Entity type that must inherit from Gameplayer</typeparam>
    ///<typeparam name="TResult">Result type that must be a value type (struct)</typeparam>
    public class StatisticsCalculator<T, TResult> : IStatisticsCalculator<T, TResult>
    where T : GamePlayer
    where TResult : struct
    {
        /// <summary>
        /// Calculate specific average value based on selector function
        /// </summary>
        public TResult CalculateAverage(List<T> players, Func<T, TResult> selector)
        {
            if(players == null || players.Count == 0)
                return default;

            var values = players.Select(selector).ToList();

            if(typeof(TResult) == typeof(int))
            {
                var sum = values.Cast<int>().Sum();
                var avg = sum / players.Count;
                return (TResult)(object)avg;
            }
            else if(typeof(TResult) == typeof(double))
            {
                var sum = values.Cast<double>().Sum();
                var avg = sum / players.Count;
                return (TResult)(object)avg;
            }
            else if(typeof(TResult) == typeof(decimal))
            {
                var sum = values.Cast<decimal>().Sum();
                var avg = sum / players.Count;
                return (TResult)(object)avg;
            }

            return default;
        }

        /// <summary>
        /// Calculate total sum of a specific statistic 
        /// </summary>
        public TResult CalculateTotal(List<T> players, Func<T, TResult> selector)
        {
            if(players == null || players.Count == 0)
                return default;

            var values = players.Select(selector).ToList();

            if(typeof(TResult) == typeof(int))
            {
                var sum = values.Cast<int>().Sum();
                return (TResult)(object)sum;
            }
            else if(typeof(TResult) == typeof(double))
            {
                var sum = values.Cast<double>().Sum();
                return (TResult)(object)sum;
            }
            else if(typeof(TResult) == typeof(decimal))
            {
                var sum = values.Cast<decimal>().Sum();
                return (TResult)(object)sum;
            }
            return default;
        }
        /// <summary>
        /// Find a player with the highest value based on a specific statistic
        /// </summary>
        public T? FindTopPerformer(List<T> players, Func<T, TResult> selector)
        {
            if(players == null || players.Count == 0)
                return default;
            return players.OrderByDescending(selector).FirstOrDefault();
        }
        /// <summary>
        /// Get top N players based on a specific statistic
        /// </summary>
        public List<T> GetTopN(List<T> players, Func<T, TResult> selector, int n)
        {
            if(players == null || players.Count == 0 || n <= 0)
                return new List<T>();
            return players.OrderByDescending(selector).Take(n).ToList();
        }
        /// <summary>
        /// Get comprehensive statistics for a single player
        /// </summary>
        public Dictionary<string, TResult> GetPlayerStatistics(T player)
        {
            var stats = new Dictionary<string, TResult>();
            if(typeof(TResult) == typeof(int))
            {
                stats["Score"] = (TResult)(object)player.CurrentGameScore;
                stats["CorrectAnswers"] = (TResult)(object)player.CorrectAnswersInGame;
            }
            else if(typeof(TResult) == typeof(double))
            {
                stats["Score"] = (TResult)(object)(double)player.CurrentGameScore;
                stats["CorrectAnswers"] = (TResult)(object)(double)player.CorrectAnswersInGame;

                if(player.CorrectAnswersInGame > 0)
                {
                    var accuracy = (double)player.CorrectAnswersInGame / 10.0 * 100.0; // assuming 10 questions
                    stats["AccuracyPercentage"] = (TResult)(object)accuracy;
                }
            }
            return stats;
        }
    }
}