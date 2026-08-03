using PhilaLink_Backend.Models.DTOs;

namespace PhilaLink_Backend.Services.Interfaces
{
    public interface IAuthService
    {
        Task<string> RegisterAsync(RegisterDto dto);
        Task<string> LoginAsync(LoginDto dto);
    }
}