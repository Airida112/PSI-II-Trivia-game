using TriviaBackend.Models.Entities;

namespace TriviaBackend.Services.Interfaces.DB
{
    public interface IClanService
    {
        Task<Clan?> GetClanByIdAsync(int clanId);
        Task<Clan?> GetClanByNameAsync(string name);
        Task<List<BaseUser>?> GetAllClanMembersAsnyc(Clan clan);
        Task AddMemberToClanAsync(Clan clan, BaseUser user);
        Task RemoveMemberFromClanAsync(Clan clan, BaseUser user);
        Task CreateClanAsync (Clan clan);
        Task RenameClanAsync(Clan clan, string newName);
        Task DeleteClanAsync(Clan clan);
    }
}
