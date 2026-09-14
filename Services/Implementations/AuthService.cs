using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PersonalProject.Data;
using PersonalProject.Models.Constants;
using PersonalProject.Models.DTOs;
using PersonalProject.Models.Entities;
using PersonalProject.Services.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace PersonalProject.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly PhilaLinkDbContext _context;
        private readonly IConfiguration _config;

        public AuthService(
            PhilaLinkDbContext context,
            IConfiguration config
        )
        {
            _context = context;
            _config = config;
        }

        // =====================================================
        // PATIENT SELF-REGISTRATION
        // =====================================================

        public async Task<RegisterResponseDto> RegisterAsync(
            RegisterDto dto
        )
        {
            var exists = await _context.Users.AnyAsync(u =>
                u.IdNumber == dto.IdNumber ||
                u.PhoneNumber == dto.PhoneNumber
            );

            if (exists)
            {
                throw new InvalidOperationException(
                    "An account with that ID number or phone number already exists."
                );
            }

            var user = new User
            {
                Id = Guid.NewGuid(),

                FullName = dto.FullName.Trim(),

                IdNumber = dto.IdNumber.Trim(),

                PhoneNumber = dto.PhoneNumber.Trim(),

                Email = dto.Email.Trim(),

                PasswordHash =
                    BCrypt.Net.BCrypt.HashPassword(dto.Password),

                Role = RoleNames.Patient,

                IsActive = true,

                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);

            await _context.SaveChangesAsync();

            /*
             * Patient profile creation will be completed during the
             * dedicated Patient onboarding batch because RegisterDto
             * currently does not contain DateOfBirth, address,
             * emergency contact, etc.
             */

            return new RegisterResponseDto
            {
                UserId = user.Id,
                FullName = user.FullName,
                Role = user.Role
            };
        }

        // =====================================================
        // LOGIN
        // =====================================================

        public async Task<LoginResponseDto> LoginAsync(LoginDto dto)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(
                    u => u.IdNumber == dto.IdNumber
                );

            /*
             * Keep one generic authentication failure message.
             * Do not reveal whether an ID number exists.
             */
            if (
                user == null ||
                !BCrypt.Net.BCrypt.Verify(
                    dto.Password,
                    user.PasswordHash
                )
            )
            {
                throw new UnauthorizedAccessException(
                    "Invalid ID number or password."
                );
            }

            // =====================================================
            // ACCOUNT STATUS
            // =====================================================

            if (!user.IsActive)
            {
                throw new UnauthorizedAccessException(
                    "This account is inactive. Contact an administrator."
                );
            }

            // =====================================================
            // LEGACY ROLE PROTECTION
            // =====================================================

            /*
             * The old architecture used Role = "Admin".
             *
             * Generic Admin is no longer a valid runtime role.
             * Existing database rows must eventually be migrated
             * to SuperAdmin or ClinicAdmin.
             */
            if (!RoleNames.IsValid(user.Role))
            {
                throw new UnauthorizedAccessException(
                    "This account has an unsupported role. Contact an administrator."
                );
            }

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

        // =====================================================
        // JWT
        // =====================================================

        private string GenerateToken(User user)
        {
            var jwtKey = _config["Jwt:Key"];

            if (string.IsNullOrWhiteSpace(jwtKey))
            {
                throw new InvalidOperationException(
                    "JWT Key is missing in configuration (Jwt:Key)."
                );
            }

            var key = Encoding.UTF8.GetBytes(jwtKey);

            var claims = new[]
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    user.Id.ToString()
                ),

                new Claim(
                    ClaimTypes.Name,
                    user.FullName
                ),

                new Claim(
                    ClaimTypes.Role,
                    user.Role
                ),

                new Claim(
                    "idNumber",
                    user.IdNumber
                )
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],

                audience: _config["Jwt:Audience"],

                claims: claims,

                expires: DateTime.UtcNow.AddHours(1),

                signingCredentials:
                    new SigningCredentials(
                        new SymmetricSecurityKey(key),
                        SecurityAlgorithms.HmacSha256
                    )
            );

            return new JwtSecurityTokenHandler()
                .WriteToken(token);
        }
    }
}