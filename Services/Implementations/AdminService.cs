using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using PersonalProject.Data;
using PersonalProject.Models;
using PersonalProject.Models.Constants;
using PersonalProject.Models.DTOs;
using PersonalProject.Models.Entities;
using PersonalProject.Services.Interfaces;
using PersonalProject.Models.Constants;

namespace PersonalProject.Services.Implementations
{
    public class AdminService : IAdminService
    {
        private readonly PhilaLinkDbContext _context;

        public AdminService(PhilaLinkDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // REGISTER CLINIC ADMIN
        // =====================================================

        public async Task<NewStaffAccountDto> RegisterClinicAdminAsync(RegisterClinicAdminDto dto, Guid performedByUserId)
        {
            var actor = await GetAdminActorAsync(performedByUserId);

            if (actor.User.Role != RoleNames.SuperAdmin)
            {
                throw new UnauthorizedAccessException(
                    "Only a SuperAdmin can create ClinicAdmin accounts."
                );
            }

            var clinicExists = await _context.Clinics.AnyAsync(c => c.Id == dto.ClinicId);

            if (!clinicExists)
            {
                throw new KeyNotFoundException(
                    "Clinic not found."
                );
            }

            var (user, tempPassword) =
                await CreateUserAsync(
                    dto.FullName,
                    dto.IdNumber,
                    dto.PhoneNumber,
                    dto.Email,
                    RoleNames.ClinicAdmin
                );

            var admin = new Admin
            {
                UserId = user.Id,

                FullName = user.FullName,

                Email = dto.Email,

                ClinicId = dto.ClinicId,

                CreatedAt = DateTime.UtcNow
            };

            _context.Admins.Add(admin);

            await _context.SaveChangesAsync();

            return CreateNewAccountResponse(
                user,
                tempPassword
            );
        }

        // =====================================================
        // REGISTER NURSE
        // =====================================================

        public async Task<NewStaffAccountDto>
            RegisterNurseAsync(
                RegisterNurseDto dto,
                Guid performedByUserId
            )
        {
            var actor =
                await GetAdminActorAsync(
                    performedByUserId
                );

            await EnsureClinicAccessAsync(
                actor,
                dto.ClinicId
            );

            var clinicExists =
                await _context.Clinics.AnyAsync(
                    c => c.Id == dto.ClinicId
                );

            if (!clinicExists)
            {
                throw new KeyNotFoundException(
                    "Clinic not found."
                );
            }

            var (user, tempPassword) =
                await CreateUserAsync(
                    dto.FullName,
                    dto.IdNumber,
                    dto.PhoneNumber,
                    dto.Email,
                    RoleNames.Nurse
                );

            var nurse = new Nurse
            {
                Id = Guid.NewGuid(),

                UserId = user.Id,

                EmployeeNumber = dto.EmployeeNumber,

                RegistrationNumber =
                    dto.RegistrationNumber,

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

                EmergencyContactName =
                    dto.EmergencyContactName,

                EmergencyContactPhone =
                    dto.EmergencyContactPhone,

                EmergencyContactRelationship =
                    dto.EmergencyContactRelationship,

                CreatedAt = DateTime.UtcNow
            };

            _context.Nurses.Add(nurse);

            await _context.SaveChangesAsync();

            return CreateNewAccountResponse(
                user,
                tempPassword
            );
        }

        // =====================================================
        // REGISTER PROXY
        // =====================================================

        public async Task<NewStaffAccountDto>
            RegisterProxyAsync(
                RegisterProxyDto dto,
                Guid performedByUserId
            )
        {
            /*
             * At this stage Proxy does not directly belong to
             * one clinic. We still validate that the caller is
             * a valid SuperAdmin or ClinicAdmin.
             */
            await GetAdminActorAsync(performedByUserId);

            var (user, tempPassword) =
                await CreateUserAsync(
                    dto.FullName,
                    dto.IdNumber,
                    dto.PhoneNumber,
                    dto.Email,
                    RoleNames.Proxy
                );

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

                RelationshipToPatient =
                    dto.RelationshipToPatient,

                EmergencyContactName =
                    dto.EmergencyContactName,

                EmergencyContactPhone =
                    dto.EmergencyContactPhone,

                EmergencyContactRelationship =
                    dto.EmergencyContactRelationship,

                IsActive = true,

                CreatedAt = DateTime.UtcNow
            };

            _context.Proxies.Add(proxy);

            await _context.SaveChangesAsync();

            return CreateNewAccountResponse(
                user,
                tempPassword
            );
        }

        // =====================================================
        // LIST ACCOUNTS
        // =====================================================

        public async Task<List<AdminAccountDto>>
            ListAccountsAsync(
                string? role,
                Guid performedByUserId
            )
        {
            var actor =
                await GetAdminActorAsync(
                    performedByUserId
                );

            var query = _context.Users
                .Include(u => u.Nurse)
                .Include(u => u.Proxy)
                .Include(u => u.Patient)
                .Include(u => u.Admin)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(role))
            {
                query = query.Where(
                    u => u.Role == role
                );
            }

            // =================================================
            // CLINIC ADMIN SCOPE
            // =================================================

            if (actor.User.Role == RoleNames.ClinicAdmin)
            {
                if (actor.Admin.ClinicId == null)
                {
                    throw new InvalidOperationException(
                        "ClinicAdmin account has no clinic assigned."
                    );
                }

                var clinicId = actor.Admin.ClinicId.Value;

                query = query.Where(u =>
                    (
                        u.Role == RoleNames.Nurse &&
                        u.Nurse != null &&
                        u.Nurse.ClinicId == clinicId
                    )
                    ||
                    (
                        u.Role == RoleNames.Patient &&
                        u.Patient != null &&
                        u.Patient.ClinicId == clinicId
                    )
                    ||
                    /*
                     * Proxies currently have no direct ClinicId.
                     * They will later be scoped through their
                     * patient assignments.
                     */
                    u.Id == performedByUserId
                );
            }

            var users = await query
                .OrderBy(u => u.FullName)
                .ToListAsync();

            return users.Select(
                u => new AdminAccountDto
                {
                    UserId = u.Id,

                    FullName = u.FullName,

                    IdNumber = u.IdNumber,

                    Role = u.Role,

                    IsActive = u.IsActive
                }
            ).ToList();
        }

        // =====================================================
        // DASHBOARD
        // =====================================================

        public async Task<AdminDashboardDto>
            GetDashboardAsync(
                Guid performedByUserId
            )
        {
            var actor =
                await GetAdminActorAsync(
                    performedByUserId
                );

            // =================================================
            // SUPER ADMIN
            // =================================================

            if (actor.User.Role == RoleNames.SuperAdmin)
            {
                return new AdminDashboardDto
                {
                    TotalNurses =
                        await _context.Nurses.CountAsync(),

                    ActiveNurses =
                        await _context.Users.CountAsync(
                            u =>
                                u.Role == RoleNames.Nurse &&
                                u.IsActive
                        ),

                    TotalProxies =
                        await _context.Proxies.CountAsync(),

                    ActiveProxies =
                        await _context.Users.CountAsync(
                            u =>
                                u.Role == RoleNames.Proxy &&
                                u.IsActive
                        ),

                    TotalPatients =
                        await _context.Patients.CountAsync(),

                    ActivePatients =
                        await _context.Users.CountAsync(
                            u =>
                                u.Role == RoleNames.Patient &&
                                u.IsActive
                        ),

                    TotalProxyLinks = await _context.ProxyLinks.CountAsync(pl => pl.IsActive)
                };
            }

            // =================================================
            // CLINIC ADMIN
            // =================================================

            if (actor.Admin.ClinicId == null)
            {
                throw new InvalidOperationException(
                    "ClinicAdmin account has no clinic assigned."
                );
            }

            var clinicId =
                actor.Admin.ClinicId.Value;

            var clinicPatientIds =
                _context.Patients
                    .Where(p => p.ClinicId == clinicId)
                    .Select(p => p.Id);

            return new AdminDashboardDto
            {
                TotalNurses =
                    await _context.Nurses.CountAsync(
                        n => n.ClinicId == clinicId
                    ),

                ActiveNurses =
                    await _context.Nurses
                        .Where(
                            n => n.ClinicId == clinicId
                        )
                        .CountAsync(
                            n => n.User.IsActive
                        ),

                TotalPatients =
                    await _context.Patients.CountAsync(
                        p => p.ClinicId == clinicId
                    ),

                ActivePatients =
                    await _context.Patients
                        .Where(
                            p => p.ClinicId == clinicId
                        )
                        .CountAsync(
                            p => p.User.IsActive
                        ),

                /*
                 * Proxy totals for ClinicAdmin are based on
                 * proxies assigned to patients in the clinic.
                 */
                TotalProxies =
                    await _context.ProxyLinks
                        .Where(
                            pl =>
                                clinicPatientIds.Contains(
                                    pl.PatientId
                                ) &&
                                pl.IsActive
                        )
                        .Select(pl => pl.ProxyId)
                        .Distinct()
                        .CountAsync(),

                ActiveProxies =
                    await _context.ProxyLinks
                        .Where(
                            pl =>
                                clinicPatientIds.Contains(
                                    pl.PatientId
                                ) &&
                                pl.IsActive &&
                                pl.Proxy.User.IsActive
                        )
                        .Select(pl => pl.ProxyId)
                        .Distinct()
                        .CountAsync(),

                TotalProxyLinks =
                    await _context.ProxyLinks.CountAsync(
                        pl =>
                            clinicPatientIds.Contains(
                                pl.PatientId
                            ) &&
                            pl.IsActive
                    )
            };
        }

        // =====================================================
        // DEACTIVATE ACCOUNT
        // =====================================================

        public async Task DeactivateAccountAsync(
            Guid userId,
            Guid performedByUserId
        )
        {
            await SetActiveAsync(
                userId,
                false,
                performedByUserId
            );
        }

        // =====================================================
        // ACTIVATE ACCOUNT
        // =====================================================

        public async Task ActivateAccountAsync(Guid userId, Guid performedByUserId)
        {
            await SetActiveAsync(
                userId,
                true,
                performedByUserId
            );
        }

        public async Task<ClinicAdminMeDto>
    GetClinicAdminMeAsync(
        Guid performedByUserId
    )
        {
            var actor =
                await GetAdminActorAsync(
                    performedByUserId
                );

            if (
                actor.User.Role !=
                    RoleNames.ClinicAdmin ||
                actor.Admin.ClinicId == null
            )
            {
                throw new UnauthorizedAccessException(
                    "ClinicAdmin account required."
                );
            }

            var clinic =
                await _context.Clinics
                    .FirstOrDefaultAsync(
                        c =>
                            c.Id ==
                            actor.Admin.ClinicId.Value
                    );

            if (clinic == null)
            {
                throw new KeyNotFoundException(
                    "Assigned clinic not found."
                );
            }

            return new ClinicAdminMeDto
            {
                UserId =
                    actor.User.Id,

                AdminId =
                    actor.Admin.Id,

                FullName =
                    actor.User.FullName,

                Email =
                    actor.User.Email,

                ClinicId =
                    clinic.Id,

                ClinicName =
                    clinic.Name,

                IsActive =
                    actor.User.IsActive
            };
        }

        public async Task<ClinicAdminOverviewDto>
            GetClinicOverviewAsync(
                Guid performedByUserId
            )
        {
            var actor =
                await GetAdminActorAsync(
                    performedByUserId
                );

            if (
                actor.User.Role !=
                    RoleNames.ClinicAdmin ||
                actor.Admin.ClinicId == null
            )
            {
                throw new UnauthorizedAccessException(
                    "ClinicAdmin account required."
                );
            }

            var clinicId =
                actor.Admin.ClinicId.Value;

            var clinic =
                await _context.Clinics
                    .FirstOrDefaultAsync(
                        c => c.Id == clinicId
                    );

            if (clinic == null)
            {
                throw new KeyNotFoundException(
                    "Assigned clinic not found."
                );
            }

            var today =
                DateTime.UtcNow.Date;

            var tomorrow =
                today.AddDays(1);

            var activePatients =
                await _context.Patients
                    .CountAsync(
                        p =>
                            p.ClinicId ==
                                clinicId &&
                            p.User.IsActive
                    );

            var activeNurses =
                await _context.Nurses
                    .CountAsync(
                        n =>
                            n.ClinicId ==
                                clinicId &&
                            n.User.IsActive
                    );

            var appointmentsToday =
                await _context.Appointments
                    .CountAsync(
                        a =>
                            a.ClinicId ==
                                clinicId &&
                            a.ScheduledAt >=
                                today &&
                            a.ScheduledAt <
                                tomorrow &&
                            a.Status !=
                                "Cancelled"
                    );

            var collectionsDueToday =
                await _context
                    .MedicationCollections
                    .CountAsync(
                        c =>
                            c.ClinicId ==
                                clinicId &&
                            c.ScheduledCollectionDate >=
                                today &&
                            c.ScheduledCollectionDate <
                                tomorrow &&
                            c.Status !=
                                "Collected" &&
                            c.Status !=
                                "Cancelled"
                    );

            var overdueCollections =
                await _context
                    .MedicationCollections
                    .CountAsync(
                        c =>
                            c.ClinicId ==
                                clinicId &&
                            c.ScheduledCollectionDate <
                                today &&
                            c.Status !=
                                "Collected" &&
                            c.Status !=
                                "Cancelled"
                    );

            var lowStockItems =
                await _context.ClinicStocks
                    .CountAsync(
                        s =>
                            s.ClinicId ==
                                clinicId &&
                            s.IsActive &&
                            s.QuantityOnHand <=
                                s.ReorderLevel
                    );

            return new ClinicAdminOverviewDto
            {
                ClinicId =
                    clinic.Id,

                ClinicName =
                    clinic.Name,

                ActivePatients =
                    activePatients,

                ActiveNurses =
                    activeNurses,

                AppointmentsToday =
                    appointmentsToday,

                CollectionsDueToday =
                    collectionsDueToday,

                OverdueCollections =
                    overdueCollections,

                LowStockItems =
                    lowStockItems
            };
        }


        // =====================================================
        // CENTRAL ACCOUNT STATUS
        // =====================================================

        private async Task SetActiveAsync(Guid targetUserId, bool isActive, Guid performedByUserId)
        {
            var actor = await GetAdminActorAsync(performedByUserId);

            if (targetUserId == performedByUserId)
            {
                throw new InvalidOperationException("You cannot change the active state of your own account.");
            }

            var target = await _context.Users
                .Include(u => u.Admin)
                .Include(u => u.Nurse)
                .Include(u => u.Proxy)
                .Include(u => u.Patient)
                .FirstOrDefaultAsync(
                    u => u.Id == targetUserId
                );

            if (target == null)
            {
                throw new KeyNotFoundException(
                    "Account not found."
                );
            }

            // =================================================
            // CLINIC ADMIN RESTRICTIONS
            // =================================================

            if (actor.User.Role == RoleNames.ClinicAdmin)
            {
                if (actor.Admin.ClinicId == null)
                {
                    throw new InvalidOperationException(
                        "ClinicAdmin account has no clinic assigned."
                    );
                }

                var clinicId =
                    actor.Admin.ClinicId.Value;

                var allowed =
                    (
                        target.Role == RoleNames.Nurse &&
                        target.Nurse?.ClinicId == clinicId
                    )
                    ||
                    (
                        target.Role == RoleNames.Patient &&
                        target.Patient?.ClinicId == clinicId
                    );

                if (!allowed)
                {
                    throw new UnauthorizedAccessException(
                        "ClinicAdmin can only manage accounts belonging to their clinic."
                    );
                }
            }

            // =================================================
            // SUPERADMIN PROTECTION
            // =================================================

            if (
                target.Role == RoleNames.SuperAdmin &&
                actor.User.Role != RoleNames.SuperAdmin
            )
            {
                throw new UnauthorizedAccessException(
                    "Only a SuperAdmin can manage another SuperAdmin account."
                );
            }

            target.IsActive = isActive;
            target.UpdatedAt = DateTime.UtcNow;

            /*
             * Temporarily keep legacy profile flags synchronized
             * while those properties still exist.
             */
            if (target.Nurse != null)
            {
                target.Nurse.IsActive = isActive;
                target.Nurse.UpdatedAt = DateTime.UtcNow;
            }

            if (target.Proxy != null)
            {
                target.Proxy.IsActive = isActive;
                target.Proxy.UpdatedAt = DateTime.UtcNow;
            }

            if (target.Patient != null)
            {
                target.Patient.IsActive = isActive;
                target.Patient.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        // =====================================================
        // CREATE USER
        // =====================================================

        private async Task<(User user, string tempPassword)>
            CreateUserAsync(
                string fullName,
                string idNumber,
                string phoneNumber,
                string email,
                string role
            )
        {
            if (!RoleNames.IsValid(role))
            {
                throw new InvalidOperationException(
                    $"Unsupported role '{role}'."
                );
            }

            var exists = await _context.Users.AnyAsync(u =>
                u.IdNumber == idNumber ||
                u.PhoneNumber == phoneNumber
            );

            if (exists)
            {
                throw new InvalidOperationException(
                    "An account with that ID number or phone number already exists."
                );
            }

            var tempPassword =
                GenerateTempPassword(
                    fullName,
                    idNumber
                );

            var user = new User
            {
                Id = Guid.NewGuid(),

                FullName = fullName.Trim(),

                IdNumber = idNumber.Trim(),

                PhoneNumber = phoneNumber.Trim(),

                Email = email.Trim(),

                PasswordHash =
                    BCrypt.Net.BCrypt.HashPassword(
                        tempPassword
                    ),

                Role = role,

                IsActive = true,

                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);

            await _context.SaveChangesAsync();

            return (user, tempPassword);
        }

        // =====================================================
        // ADMIN ACTOR
        // =====================================================

        private async Task<(User User, Admin Admin)>
            GetAdminActorAsync(Guid userId)
        {
            var user = await _context.Users
                .Include(u => u.Admin)
                .FirstOrDefaultAsync(
                    u => u.Id == userId
                );

            if (
                user == null ||
                user.Admin == null
            )
            {
                throw new UnauthorizedAccessException(
                    "Administrator account not found."
                );
            }

            if (!user.IsActive)
            {
                throw new UnauthorizedAccessException(
                    "Administrator account is inactive."
                );
            }

            if (
                user.Role != RoleNames.SuperAdmin &&
                user.Role != RoleNames.ClinicAdmin
            )
            {
                throw new UnauthorizedAccessException(
                    "Administrator privileges are required."
                );
            }

            return (user, user.Admin);
        }

        // =====================================================
        // CLINIC ACCESS
        // =====================================================

        private static Task EnsureClinicAccessAsync(
            (User User, Admin Admin) actor,
            Guid clinicId
        )
        {
            if (actor.User.Role == RoleNames.SuperAdmin)
            {
                return Task.CompletedTask;
            }

            if (
                actor.Admin.ClinicId == null ||
                actor.Admin.ClinicId != clinicId
            )
            {
                throw new UnauthorizedAccessException(
                    "ClinicAdmin can only manage their assigned clinic."
                );
            }

            return Task.CompletedTask;
        }

        // =====================================================
        // TEMP PASSWORD
        // =====================================================

        private static string GenerateTempPassword(
            string fullName,
            string idNumber
        )
        {
            var namePart =
                fullName.Replace(" ", "");

            var idPart =
                idNumber.Length >= 6
                    ? idNumber[..6]
                    : idNumber;

            return $"{namePart}{idPart}";
        }

        // =====================================================
        // RESPONSE
        // =====================================================

        private static NewStaffAccountDto
            CreateNewAccountResponse(
                User user,
                string temporaryPassword
            )
        {
            return new NewStaffAccountDto
            {
                UserId = user.Id,

                FullName = user.FullName,

                IdNumber = user.IdNumber,

                Role = user.Role,

                TemporaryPassword =
                    temporaryPassword
            };
        }
    }
}