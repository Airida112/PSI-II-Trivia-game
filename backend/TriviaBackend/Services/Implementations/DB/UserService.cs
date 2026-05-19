using Microsoft.EntityFrameworkCore;
using TriviaBackend.Data;
using TriviaBackend.Models.Entities;
using TriviaBackend.Services.Interfaces.DB;

namespace TriviaBackend.Services.Implementations.DB
{
    public class UserService(ITriviaDbContext context) : IUserService
    {
        private readonly ITriviaDbContext _context = context;

        public async Task<BaseUser?> GetUserByIdAsync(string id) =>
            await _context.Users.FindAsync(id);
        public async Task<BaseUser?> GetUserByUsernameAsync(string usn) =>
            await _context.Users.FirstOrDefaultAsync(u => u.Username == usn);

        public async Task AddUserAsync(BaseUser user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveUserAsync(BaseUser user)
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
        }
    }
}
