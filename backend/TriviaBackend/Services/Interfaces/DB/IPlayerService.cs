using TriviaBackend.Models.Entities;

namespace TriviaBackend.Services.Interfaces.DB
{
    public interface IPlayerService
    {
        Task<List<Player>> GetAllPlayersAsync();

        Task<Player?> GetPlayerByUsernameAsync(string username);

        Task UpdatePlayerAsync(Player player);
    }
}
