using Microsoft.EntityFrameworkCore;
using PersonalProject.Data;
using PersonalProject.Models.DTOs;
using PersonalProject.Models.Entities;
using PersonalProject.Services.Interfaces;
using BCrypt.Net;

namespace PersonalProject.Services.Implementations
{
    public class AdminService : IAdminService
    {
        private readonly PhilaLinkDbContext _context;

        public AdminService(PhilaLinkDbContext context)
        {
            _context = context;
        }

        public async Task<NewStaffAccountDto> RegisterNurseAsync(RegisterNurseDto dto)
        {
            var (user, tempPassword) = await CreateUserAsync(
                dto.FullName, dto.IdNumber, dto.PhoneNumber, dto.Email, "Nurse");

            var nurse = new Nurse
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                EmployeeNumber = dto.EmployeeNumber,
                RegistrationNumber = dto.RegistrationNumber,
                Qualification = dto.Qualification,
                ClinicId = dto.ClinicId,
                Email = dto.Email,
                AddressLine1 = dto.AddressLine1,
                AddressLine2 = dto.AddressLine2,
                Suburb = dto.Suburb,
                City = dto.City,
                Province = dto.Province,
                PostalCode = dto.PostalCode,
                DateOfBirth = dto.DateOfBirth,
                Gender = dto.Gender,
                EmploymentDate = dto.EmploymentDate,
                IsActive = true,
                EmergencyContactName = dto.EmergencyContactName,
                EmergencyContactPhone = dto.EmergencyContactPhone,
                EmergencyContactRelationship = dto.EmergencyContactRelationship
            };

            _context.Nurses.Add(nurse);
            await _context.SaveChangesAsync();

            return new NewStaffAccountDto
            {
                UserId = user.Id,
                FullName = user.FullName,
                IdNumber = user.IdNumber,
                Role = user.Role,
                TemporaryPassword = tempPassword
            };
        }

        public async Task<NewStaffAccountDto> RegisterProxyAsync(RegisterProxyDto dto)
        {
            var (user, tempPassword) = await CreateUserAsync(
                dto.FullName, dto.IdNumber, dto.PhoneNumber, dto.Email, "Proxy");

            var proxy = new Proxy
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Email = dto.Email,
                AddressLine1 = dto.AddressLine1,
                AddressLine2 = dto.AddressLine2,
                Suburb = dto.Suburb,
                City = dto.City,
                Province = dto.Province,
                PostalCode = dto.PostalCode,
                DateOfBirth = dto.DateOfBirth,
                Gender = dto.Gender,
                RelationshipToPatient = dto.RelationshipToPatient,
                EmergencyContactName = dto.EmergencyContactName,
                EmergencyContactPhone = dto.EmergencyContactPhone,
                EmergencyContactRelationship = dto.EmergencyContactRelationship,
                IsActive = true
            };

            _context.Proxies.Add(proxy);
            await _context.SaveChangesAsync();

            return new NewStaffAccountDto
            {
                UserId = user.Id,
                FullName = user.FullName,
                IdNumber = user.IdNumber,
                Role = user.Role,
                TemporaryPassword = tempPassword
            };
        }

        public async Task<List<AdminAccountDto>> ListAccountsAsync(string? role)
        {
            var query = _context.Users
                .Include(u => u.Nurse)
                .Include(u => u.Proxy)
                .Include(u => u.Patient)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(role))
                query = query.Where(u => u.Role == role);

            var users = await query.ToListAsync();

            return users.Select(u => new AdminAccountDto
            {
                UserId = u.Id,
                FullName = u.FullName,
                IdNumber = u.IdNumber,
                Role = u.Role,
                IsActive = u.Role switch
                {
                    "Nurse" => u.Nurse?.IsActive ?? true,
                    "Proxy" => u.Proxy?.IsActive ?? true,
                    "Patient" => u.Patient?.IsActive ?? true,
                    _ => true // Admin accounts have no IsActive flag today
                }
            }).ToList();
        }

        public async Task<AdminDashboardDto> GetDashboardAsync()
        {
            return new AdminDashboardDto
            {
                TotalNurses = await _context.Nurses.CountAsync(),
                ActiveNurses = await _context.Nurses.CountAsync(n => n.IsActive),

                TotalProxies = await _context.Proxies.CountAsync(),
                ActiveProxies = await _context.Proxies.CountAsync(p => p.IsActive),

                TotalPatients = await _context.Patients.CountAsync(),
                ActivePatients = await _context.Patients.CountAsync(p => p.IsActive),

                TotalProxyLinks = await _context.ProxyLinks.CountAsync()
            };
        }

        public async Task DeactivateAccountAsync(Guid userId) => await SetActiveAsync(userId, false);

        public async Task ActivateAccountAsync(Guid userId) => await SetActiveAsync(userId, true);

        public async Task DeleteAccountAsync(Guid userId)
        {
            var user = await _context.Users
                .Include(u => u.Nurse)
                .Include(u => u.Proxy)
                .Include(u => u.Patient)
                .Include(u => u.Admin)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                throw new KeyNotFoundException("Account not found.");

            // Clear any proxy-patient links this account is part of first,
            // so the ProxyLink foreign keys don't block deletion.
            if (user.Proxy != null)
            {
                var links = await _context.ProxyLinks
                    .Where(l => l.ProxyId == user.Proxy.Id)
                    .ToListAsync();
                _context.ProxyLinks.RemoveRange(links);
            }
            if (user.Patient != null)
            {
                var links = await _context.ProxyLinks
                    .Where(l => l.PatientId == user.Patient.Id)
                    .ToListAsync();
                _context.ProxyLinks.RemoveRange(links);
            }

            if (user.Nurse != null) _context.Nurses.Remove(user.Nurse);
            if (user.Proxy != null) _context.Proxies.Remove(user.Proxy);
            if (user.Patient != null) _context.Patients.Remove(user.Patient);
            if (user.Admin != null) _context.Admins.Remove(user.Admin);

            _context.Users.Remove(user);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                // Most likely cause: this account is referenced by records
                // this method doesn't know about yet (medication logs, audit
                // history, symptom assessments, etc.) — anything with a
                // required FK back to this user. Deactivating instead avoids
                // this entirely, since it doesn't touch referential integrity.
                throw new InvalidOperationException(
                    "Couldn't delete this account — it's likely still referenced by " +
                    "other records (medication history, audit logs, past assignments). " +
                    "Deactivating instead preserves that history without deleting it.",
                    ex);
            }
        }

        private async Task<(User user, string tempPassword)> CreateUserAsync(
            string fullName, string idNumber, string phoneNumber, string email, string role)
        {
            var exists = await _context.Users
                .AnyAsync(u => u.IdNumber == idNumber || u.PhoneNumber == phoneNumber);

            if (exists)
                throw new InvalidOperationException(
                    "An account with that ID number or phone number already exists.");

            var tempPassword = GenerateTempPassword(fullName, idNumber);

            var user = new User
            {
                Id = Guid.NewGuid(),
                FullName = fullName,
                IdNumber = idNumber,
                PhoneNumber = phoneNumber,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword),
                Role = role
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return (user, tempPassword);
        }

        private static string GenerateTempPassword(string fullName, string idNumber)
        {
            var namePart = fullName.Replace(" ", "");
            var idPart = idNumber.Length >= 6 ? idNumber[..6] : idNumber;
            return $"{namePart}{idPart}";
        }

        private async Task SetActiveAsync(Guid userId, bool isActive)
        {
            var user = await _context.Users
                .Include(u => u.Nurse)
                .Include(u => u.Proxy)
                .Include(u => u.Patient)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                throw new KeyNotFoundException("Account not found.");

            switch (user.Role)
            {
                case "Nurse" when user.Nurse != null:
                    user.Nurse.IsActive = isActive;
                    user.Nurse.UpdatedAt = DateTime.UtcNow;
                    break;
                case "Proxy" when user.Proxy != null:
                    user.Proxy.IsActive = isActive;
                    user.Proxy.UpdatedAt = DateTime.UtcNow;
                    break;
                case "Patient" when user.Patient != null:
                    user.Patient.IsActive = isActive;
                    user.Patient.UpdatedAt = DateTime.UtcNow;
                    break;
                default:
                    throw new InvalidOperationException(
                        $"Accounts with role '{user.Role}' don't support activation toggling.");
            }

            await _context.SaveChangesAsync();
        }
    }
}