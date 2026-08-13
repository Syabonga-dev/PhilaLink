using PersonalProject.Data;
using PersonalProject.Models.Entities;
using PersonalProject.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace PersonalProject.Services.Implementations
{
    public class UserService : IUserService
    {
        private readonly PhilaLinkDbContext _context;

        public UserService(PhilaLinkDbContext context)
        {
            _context = context;
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _context.Users.ToListAsync();
        }

        public async Task<User?> GetUserByIdAsync(Guid id)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
        }
    }
}


