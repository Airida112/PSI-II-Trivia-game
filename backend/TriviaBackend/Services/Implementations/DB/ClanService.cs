using Microsoft.EntityFrameworkCore;
using TriviaBackend.Data;
using TriviaBackend.Models.Entities;
using TriviaBackend.Services.Interfaces.DB;

namespace TriviaBackend.Services.Implementations.DB
{
    public class ClanService(ITriviaDbContext context) : IClanService
    {
        private readonly ITriviaDbContext _context = context;

        public async Task<Clan?> GetClanByIdAsync(int clanId) =>
            await _context.Clans.FindAsync(clanId);
        public async Task<Clan?> GetClanByNameAsync(string name) =>
            await _context.Clans.FirstOrDefaultAsync(c => c.Name == name);
        public async Task<List<BaseUser>?> GetAllClanMembersAsnyc(Clan clan) =>
            await _context.Users.Where(u => u.ClanId == clan.Id).ToListAsync();

        public async Task AddMemberToClanAsync(Clan clan, BaseUser user)
        {
            user.ClanId = clan.Id;
            clan.MemberCount++;
            await _context.SaveChangesAsync();
        }

        public async Task RemoveMemberFromClanAsync(Clan clan, BaseUser user)
        {
            user.ClanId = null;
            clan.MemberCount--;
            await _context.SaveChangesAsync();
        }

        public async Task CreateClanAsync(Clan clan)
        {
            _context.Clans.Add(clan);
            await _context.SaveChangesAsync();
        }

        public async Task RenameClanAsync(Clan clan, string newName)
        {
            clan.Name = newName;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteClanAsync(Clan clan)
        {
            _context.Clans.Remove(clan);
            await _context.SaveChangesAsync();
        }
    }
}