using TriviaBackend.Models.Entities;

namespace TriviaBackend.Services.Interfaces
{
    public interface IStatisticsCalculator<T, TResult>
    where T : GamePlayer
    where TResult : struct
    {
        TResult CalculateAverage(List<T> players, Func<T, TResult> selector);
        TResult CalculateTotal(List<T> players, Func<T, TResult> selector);
        T? FindTopPerformer(List<T> players, Func<T, TResult> selector);
        List<T> GetTopN(List<T> players, Func<T, TResult> selector, int n);
        Dictionary<string, TResult> GetPlayerStatistics(T player);
    }
}