using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PersonalProject.Data;
using PersonalProject.Models.DTOs;
using PersonalProject.Models.Entities;
using PersonalProject.Services.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;

namespace PersonalProject.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly PhilaLinkDbContext _context;
        private readonly IConfiguration _config;

        public AuthService(PhilaLinkDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        public async Task<RegisterResponseDto> RegisterAsync(RegisterDto dto)
        {
            var exists = await _context.Users
                .AnyAsync(u =>
                    u.IdNumber == dto.IdNumber ||
                    u.PhoneNumber == dto.PhoneNumber);

            if (exists)
                throw new InvalidOperationException(
                    "An account with that ID number or phone number already exists.");

            var user = new User
            {
                Id = Guid.NewGuid(),
                FullName = dto.FullName,
                IdNumber = dto.IdNumber,
                PhoneNumber = dto.PhoneNumber,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = "Patient"
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return new RegisterResponseDto
            {
                UserId = user.Id,
                FullName = user.FullName,
                Role = user.Role
            };
        }

        public async Task<LoginResponseDto> LoginAsync(LoginDto dto)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.IdNumber == dto.IdNumber);

            // Deliberately the same error whether the ID number doesn't exist
            // or the password is wrong — don't let a client fish for which
            // ID numbers are registered.
            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                throw new UnauthorizedAccessException("Invalid ID number or password.");

            return new LoginResponseDto
            {
                Token = GenerateToken(user),
                User = new UserResponseDto
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    IdNumber = user.IdNumber,
                    PhoneNumber = user.PhoneNumber,
                    Email = user.Email,
                    Role = user.Role
                }
            };
        }

        private string GenerateToken(User user)
        {
            var jwtKey = _config["Jwt:Key"];

            if (string.IsNullOrWhiteSpace(jwtKey))
                throw new InvalidOperationException("JWT Key is missing in configuration (Jwt:Key).");

            var key = Encoding.UTF8.GetBytes(jwtKey);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("idNumber", user.IdNumber)
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256
                )
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}