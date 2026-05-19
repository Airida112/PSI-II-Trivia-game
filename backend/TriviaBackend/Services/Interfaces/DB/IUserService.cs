using TriviaBackend.Models.Entities;

namespace TriviaBackend.Services.Interfaces.DB
{
    public interface IUserService
    {
        Task<BaseUser?> GetUserByIdAsync(string id);

        Task<BaseUser?> GetUserByUsernameAsync(string usn);

        Task AddUserAsync(BaseUser user);

        Task RemoveUserAsync(BaseUser user);
    }
}
