using Microsoft.EntityFrameworkCore;
using TriviaBackend.Data;
using TriviaBackend.Models.Entities;
using TriviaBackend.Services.Interfaces.DB;

namespace TriviaBackend.Services.Implementations.DB
{
    public class PlayerService(ITriviaDbContext context) : IPlayerService
    {
        private readonly ITriviaDbContext _context = context;

        public async Task<List<Player>> GetAllPlayersAsync() =>
            await _context.Users.OfType<Player>().ToListAsync();

        public async Task<Player?> GetPlayerByUsernameAsync(string username) =>
            await _context.Users.OfType<Player>().FirstOrDefaultAsync(p => p.Username == username);

        public async Task UpdatePlayerAsync(Player player)
        {
            _context.Users.Update(player);
            await _context.SaveChangesAsync();
        }
    }
}
