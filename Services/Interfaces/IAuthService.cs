using PersonalProject.Models.DTOs;

namespace PersonalProject.Services.Interfaces
{
    public interface IAuthService
    {
        Task<RegisterResponseDto> RegisterAsync(
            RegisterDto dto
        );

        Task<LoginResponseDto> LoginAsync(
            LoginDto dto
        );

        Task<UserResponseDto> GetMeAsync(
            Guid userId
        );

        Task<LoginResponseDto> ChangePasswordAsync(
            Guid userId,
            ChangePasswordDto dto
        );
    }
}