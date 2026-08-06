using PhilaLink_Backend.Data;
using PhilaLink_Backend.Models.Entities;
using PhilaLink_Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace PhilaLink_Backend.Services.Implementations
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
