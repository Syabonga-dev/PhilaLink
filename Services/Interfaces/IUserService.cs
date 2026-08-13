using PersonalProject.Models.Entities;

namespace PersonalProject.Services.Interfaces
{
    public interface IUserService
    {
        Task<List<User>> GetAllUsersAsync();
        Task<User?> GetUserByIdAsync(Guid id);
    }
}


