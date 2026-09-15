using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using PersonalProject.Data;
using PersonalProject.Models;
using PersonalProject.Models.Constants;
using PersonalProject.Models.DTOs;
using PersonalProject.Models.Entities;
using PersonalProject.Services.Interfaces;

namespace PersonalProject.Services.Implementations
{
    public class AdminService : IAdminService
    {
        private readonly PhilaLinkDbContext _context;

        public AdminService(
            PhilaLinkDbContext context
        )
        {
            _context = context;
        }

        // =====================================================
        // REGISTER CLINIC ADMIN
        // =====================================================

        public async Task<NewStaffAccountDto>
            RegisterClinicAdminAsync(
                RegisterClinicAdminDto dto,
                Guid performedByUserId
            )
        {
            var actor =
                await GetAdminActorAsync(
                    performedByUserId
                );

            if (
                actor.User.Role !=
                RoleNames.SuperAdmin
            )
            {
                throw new UnauthorizedAccessException(
                    "Only a SuperAdmin can create ClinicAdmin accounts."
                );
            }

            var clinicExists =
                await _context.Clinics
                    .AnyAsync(
                        c =>
                            c.Id ==
                            dto.ClinicId
                    );

            if (!clinicExists)
            {
                throw new KeyNotFoundException(
                    "Clinic not found."
                );
            }

            await using var transaction =
                await _context.Database
                    .BeginTransactionAsync();

            var (user, tempPassword) =
                await CreateUserAsync(
                    dto.FullName,
                    dto.IdNumber,
                    dto.PhoneNumber,
                    dto.Email,
                    RoleNames.ClinicAdmin
                );

            var admin =
                new Admin
                {
                    UserId =
                        user.Id,

                    FullName =
                        user.FullName,

                    Email =
                        user.Email,

                    ClinicId =
                        dto.ClinicId,

                    CreatedAt =
                        DateTime.UtcNow
                };

            _context.Admins.Add(
                admin
            );

            /*
             * User + Admin profile are persisted together.
             *
             * If either insert fails, the transaction is never
             * committed and no orphan User account remains.
             */
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

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
                await _context.Clinics
                    .AnyAsync(
                        c =>
                            c.Id ==
                            dto.ClinicId
                    );

            if (!clinicExists)
            {
                throw new KeyNotFoundException(
                    "Clinic not found."
                );
            }

            var employeeNumber =
                dto.EmployeeNumber.Trim();

            var registrationNumber =
                dto.RegistrationNumber.Trim();

            var professionalIdentifierExists =
                await _context.Nurses
                    .AnyAsync(
                        n =>
                            n.EmployeeNumber ==
                                employeeNumber ||
                            n.RegistrationNumber ==
                                registrationNumber
                    );

            if (professionalIdentifierExists)
            {
                throw new InvalidOperationException(
                    "A nurse with that employee number or registration number already exists."
                );
            }

            await using var transaction =
                await _context.Database
                    .BeginTransactionAsync();

            var (user, tempPassword) =
                await CreateUserAsync(
                    dto.FullName,
                    dto.IdNumber,
                    dto.PhoneNumber,
                    dto.Email,
                    RoleNames.Nurse
                );

            var nurse =
                new Nurse
                {
                    Id =
                        Guid.NewGuid(),

                    UserId =
                        user.Id,

                    EmployeeNumber =
                        employeeNumber,

                    RegistrationNumber =
                        registrationNumber,

                    Qualification =
                        dto.Qualification.Trim(),

                    ClinicId =
                        dto.ClinicId,

                    Email =
                        user.Email,

                    AddressLine1 =
                        dto.AddressLine1.Trim(),

                    AddressLine2 =
                        string.IsNullOrWhiteSpace(
                            dto.AddressLine2
                        )
                            ? null
                            : dto.AddressLine2.Trim(),

                    Suburb =
                        dto.Suburb.Trim(),

                    City =
                        dto.City.Trim(),

                    Province =
                        dto.Province.Trim(),

                    PostalCode =
                        dto.PostalCode.Trim(),

                    DateOfBirth =
                        dto.DateOfBirth,

                    Gender =
                        dto.Gender.Trim(),

                    EmploymentDate =
                        dto.EmploymentDate,

                    EmergencyContactName =
                        dto.EmergencyContactName.Trim(),

                    EmergencyContactPhone =
                        dto.EmergencyContactPhone.Trim(),

                    EmergencyContactRelationship =
                        dto.EmergencyContactRelationship.Trim(),

                    CreatedAt =
                        DateTime.UtcNow
                };

            _context.Nurses.Add(
                nurse
            );

            /*
             * User + Nurse profile are one logical operation.
             */
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

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
            await GetAdminActorAsync(
                performedByUserId
            );

            await using var transaction =
                await _context.Database
                    .BeginTransactionAsync();

            var (user, tempPassword) =
                await CreateUserAsync(
                    dto.FullName,
                    dto.IdNumber,
                    dto.PhoneNumber,
                    dto.Email,
                    RoleNames.Proxy
                );

            var proxy =
                new Proxy
                {
                    Id =
                        Guid.NewGuid(),

                    UserId =
                        user.Id,

                    Email =
                        user.Email,

                    AddressLine1 =
                        dto.AddressLine1.Trim(),

                    AddressLine2 =
                        string.IsNullOrWhiteSpace(
                            dto.AddressLine2
                        )
                            ? null
                            : dto.AddressLine2.Trim(),

                    Suburb =
                        dto.Suburb.Trim(),

                    City =
                        dto.City.Trim(),

                    Province =
                        dto.Province.Trim(),

                    PostalCode =
                        dto.PostalCode.Trim(),

                    DateOfBirth =
                        dto.DateOfBirth,

                    Gender =
                        dto.Gender.Trim(),

                    RelationshipToPatient =
                        dto.RelationshipToPatient.Trim(),

                    EmergencyContactName =
                        dto.EmergencyContactName.Trim(),

                    EmergencyContactPhone =
                        dto.EmergencyContactPhone.Trim(),

                    EmergencyContactRelationship =
                        dto.EmergencyContactRelationship.Trim(),

                    CreatedAt =
                        DateTime.UtcNow
                };

            _context.Proxies.Add(
                proxy
            );

            /*
             * User + Proxy profile are one logical operation.
             */
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

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

            var query =
                _context.Users
                    .Include(u => u.Nurse)
                    .Include(u => u.Proxy)
                    .Include(u => u.Patient)
                    .Include(u => u.Admin)
                    .AsQueryable();

            if (
                !string.IsNullOrWhiteSpace(
                    role
                )
            )
            {
                query =
                    query.Where(
                        u =>
                            u.Role ==
                            role
                    );
            }

            if (
                actor.User.Role ==
                RoleNames.ClinicAdmin
            )
            {
                if (
                    actor.Admin.ClinicId ==
                    null
                )
                {
                    throw new InvalidOperationException(
                        "ClinicAdmin account has no clinic assigned."
                    );
                }

                var clinicId =
                    actor.Admin.ClinicId.Value;

                query =
                    query.Where(
                        u =>
                            (
                                u.Role ==
                                    RoleNames.Nurse &&
                                u.Nurse != null &&
                                u.Nurse.ClinicId ==
                                    clinicId
                            )
                            ||
                            (
                                u.Role ==
                                    RoleNames.Patient &&
                                u.Patient != null &&
                                u.Patient.ClinicId ==
                                    clinicId
                            )
                            ||
                            u.Id ==
                                performedByUserId
                    );
            }

            var users =
                await query
                    .OrderBy(
                        u =>
                            u.FullName
                    )
                    .ToListAsync();

            return users
                .Select(
                    u =>
                        new AdminAccountDto
                        {
                            UserId =
                                u.Id,

                            FullName =
                                u.FullName,

                            IdNumber =
                                u.IdNumber,

                            Role =
                                u.Role,

                            IsActive =
                                u.IsActive
                        }
                )
                .ToList();
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

            if (
                actor.User.Role ==
                RoleNames.SuperAdmin
            )
            {
                return new AdminDashboardDto
                {
                    TotalNurses =
                        await _context.Nurses
                            .CountAsync(),

                    ActiveNurses =
                        await _context.Users
                            .CountAsync(
                                u =>
                                    u.Role ==
                                        RoleNames.Nurse &&
                                    u.IsActive
                            ),

                    TotalProxies =
                        await _context.Proxies
                            .CountAsync(),

                    ActiveProxies =
                        await _context.Users
                            .CountAsync(
                                u =>
                                    u.Role ==
                                        RoleNames.Proxy &&
                                    u.IsActive
                            ),

                    TotalPatients =
                        await _context.Patients
                            .CountAsync(),

                    ActivePatients =
                        await _context.Users
                            .CountAsync(
                                u =>
                                    u.Role ==
                                        RoleNames.Patient &&
                                    u.IsActive
                            ),

                    TotalProxyLinks =
                        await _context.ProxyLinks
                            .CountAsync(
                                pl =>
                                    pl.IsActive
                            )
                };
            }

            if (
                actor.Admin.ClinicId ==
                null
            )
            {
                throw new InvalidOperationException(
                    "ClinicAdmin account has no clinic assigned."
                );
            }

            var clinicId =
                actor.Admin.ClinicId.Value;

            var clinicPatientIds =
                _context.Patients
                    .Where(
                        p =>
                            p.ClinicId ==
                            clinicId
                    )
                    .Select(
                        p =>
                            p.Id
                    );

            return new AdminDashboardDto
            {
                TotalNurses =
                    await _context.Nurses
                        .CountAsync(
                            n =>
                                n.ClinicId ==
                                clinicId
                        ),

                ActiveNurses =
                    await _context.Nurses
                        .Where(
                            n =>
                                n.ClinicId ==
                                clinicId
                        )
                        .CountAsync(
                            n =>
                                n.User.IsActive
                        ),

                TotalPatients =
                    await _context.Patients
                        .CountAsync(
                            p =>
                                p.ClinicId ==
                                clinicId
                        ),

                ActivePatients =
                    await _context.Patients
                        .Where(
                            p =>
                                p.ClinicId ==
                                clinicId
                        )
                        .CountAsync(
                            p =>
                                p.User.IsActive
                        ),

                TotalProxies =
                    await _context.ProxyLinks
                        .Where(
                            pl =>
                                clinicPatientIds
                                    .Contains(
                                        pl.PatientId
                                    ) &&
                                pl.IsActive
                        )
                        .Select(
                            pl =>
                                pl.ProxyId
                        )
                        .Distinct()
                        .CountAsync(),

                ActiveProxies =
                    await _context.ProxyLinks
                        .Where(
                            pl =>
                                clinicPatientIds
                                    .Contains(
                                        pl.PatientId
                                    ) &&
                                pl.IsActive &&
                                pl.Proxy.User.IsActive
                        )
                        .Select(
                            pl =>
                                pl.ProxyId
                        )
                        .Distinct()
                        .CountAsync(),

                TotalProxyLinks =
                    await _context.ProxyLinks
                        .CountAsync(
                            pl =>
                                clinicPatientIds
                                    .Contains(
                                        pl.PatientId
                                    ) &&
                                pl.IsActive
                        )
            };
        }

        // =====================================================
        // DEACTIVATE / ACTIVATE
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

        public async Task ActivateAccountAsync(
            Guid userId,
            Guid performedByUserId
        )
        {
            await SetActiveAsync(
                userId,
                true,
                performedByUserId
            );
        }

        // =====================================================
        // CLINIC ADMIN SELF
        // =====================================================

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
                actor.Admin.ClinicId ==
                    null
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

        // =====================================================
        // CLINIC OVERVIEW
        // =====================================================

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
                actor.Admin.ClinicId ==
                    null
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
                        c =>
                            c.Id ==
                            clinicId
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
                                AppointmentStatuses.Cancelled
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
                                MedicationCollectionStatuses.Collected &&
                            c.Status !=
                                MedicationCollectionStatuses.Cancelled
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
                                MedicationCollectionStatuses.Collected &&
                            c.Status !=
                                MedicationCollectionStatuses.Cancelled
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

        private async Task SetActiveAsync(
            Guid targetUserId,
            bool isActive,
            Guid performedByUserId
        )
        {
            var actor =
                await GetAdminActorAsync(
                    performedByUserId
                );

            if (
                targetUserId ==
                performedByUserId
            )
            {
                throw new InvalidOperationException(
                    "You cannot change the active state of your own account."
                );
            }

            var target =
                await _context.Users
                    .Include(u => u.Nurse)
                    .Include(u => u.Patient)
                    .FirstOrDefaultAsync(
                        u =>
                            u.Id ==
                            targetUserId
                    );

            if (target == null)
            {
                throw new KeyNotFoundException(
                    "Account not found."
                );
            }

            if (
                actor.User.Role ==
                RoleNames.ClinicAdmin
            )
            {
                if (
                    actor.Admin.ClinicId ==
                    null
                )
                {
                    throw new InvalidOperationException(
                        "ClinicAdmin account has no clinic assigned."
                    );
                }

                var clinicId =
                    actor.Admin.ClinicId.Value;

                var allowed =
                    (
                        target.Role ==
                            RoleNames.Nurse &&
                        target.Nurse?.ClinicId ==
                            clinicId
                    )
                    ||
                    (
                        target.Role ==
                            RoleNames.Patient &&
                        target.Patient?.ClinicId ==
                            clinicId
                    );

                if (!allowed)
                {
                    throw new UnauthorizedAccessException(
                        "ClinicAdmin can only manage accounts belonging to their clinic."
                    );
                }
            }

            if (
                target.Role ==
                    RoleNames.SuperAdmin &&
                actor.User.Role !=
                    RoleNames.SuperAdmin
            )
            {
                throw new UnauthorizedAccessException(
                    "Only a SuperAdmin can manage another SuperAdmin account."
                );
            }

            /*
             * User.IsActive is the single authoritative
             * account-state flag.
             */
            target.IsActive =
                isActive;

            target.UpdatedAt =
                DateTime.UtcNow;

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

            var normalizedFullName =
                fullName.Trim();

            var normalizedIdNumber =
                idNumber.Trim();

            var normalizedPhoneNumber =
                phoneNumber.Trim();

            var normalizedEmail =
                email.Trim();

            var exists =
                await _context.Users
                    .AnyAsync(
                        u =>
                            u.IdNumber ==
                                normalizedIdNumber ||
                            u.PhoneNumber ==
                                normalizedPhoneNumber
                    );

            if (exists)
            {
                throw new InvalidOperationException(
                    "An account with that ID number or phone number already exists."
                );
            }

            var tempPassword =
                GenerateTempPassword();

            var now =
                DateTime.UtcNow;

            var user =
                new User
                {
                    Id =
                        Guid.NewGuid(),

                    FullName =
                        normalizedFullName,

                    IdNumber =
                        normalizedIdNumber,

                    PhoneNumber =
                        normalizedPhoneNumber,

                    Email =
                        normalizedEmail,

                    PasswordHash =
                        BCrypt.Net.BCrypt
                            .HashPassword(
                                tempPassword
                            ),

                    Role =
                        role,

                    IsActive =
                        true,

                    /*
                     * Staff accounts are created through an
                     * authenticated administrator workflow.
                     */
                    IsVerified =
                        true,

                    VerifiedAt =
                        now,

                    CreatedAt =
                        now
                };

            /*
             * Deliberately do NOT call SaveChanges here.
             *
             * The caller creates the corresponding Admin,
             * Nurse, or Proxy profile first and then saves the
             * complete account inside one transaction.
             */
            _context.Users.Add(
                user
            );

            return (
                user,
                tempPassword
            );
        }

        // =====================================================
        // ADMIN ACTOR
        // =====================================================

        private async Task<(User User, Admin Admin)>
            GetAdminActorAsync(
                Guid userId
            )
        {
            var user =
                await _context.Users
                    .Include(
                        u =>
                            u.Admin
                    )
                    .FirstOrDefaultAsync(
                        u =>
                            u.Id ==
                            userId
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
                user.Role !=
                    RoleNames.SuperAdmin &&
                user.Role !=
                    RoleNames.ClinicAdmin
            )
            {
                throw new UnauthorizedAccessException(
                    "Administrator privileges are required."
                );
            }

            return (
                user,
                user.Admin
            );
        }

        // =====================================================
        // CLINIC ACCESS
        // =====================================================

        private static Task EnsureClinicAccessAsync(
            (User User, Admin Admin) actor,
            Guid clinicId
        )
        {
            if (
                actor.User.Role ==
                RoleNames.SuperAdmin
            )
            {
                return Task.CompletedTask;
            }

            if (
                actor.Admin.ClinicId ==
                    null ||
                actor.Admin.ClinicId !=
                    clinicId
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

        private static string GenerateTempPassword()
        {
            const string uppercase =
                "ABCDEFGHJKLMNPQRSTUVWXYZ";

            const string lowercase =
                "abcdefghijkmnopqrstuvwxyz";

            const string digits =
                "23456789";

            const string symbols =
                "!@#$%&*";

            const string all =
                uppercase +
                lowercase +
                digits +
                symbols;

            var password =
                new char[14];

            password[0] =
                uppercase[
                    System.Security.Cryptography
                        .RandomNumberGenerator
                        .GetInt32(
                            uppercase.Length
                        )
                ];

            password[1] =
                lowercase[
                    System.Security.Cryptography
                        .RandomNumberGenerator
                        .GetInt32(
                            lowercase.Length
                        )
                ];

            password[2] =
                digits[
                    System.Security.Cryptography
                        .RandomNumberGenerator
                        .GetInt32(
                            digits.Length
                        )
                ];

            password[3] =
                symbols[
                    System.Security.Cryptography
                        .RandomNumberGenerator
                        .GetInt32(
                            symbols.Length
                        )
                ];

            for (
                var i = 4;
                i < password.Length;
                i++
            )
            {
                password[i] =
                    all[
                        System.Security.Cryptography
                            .RandomNumberGenerator
                            .GetInt32(
                                all.Length
                            )
                    ];
            }

            Random.Shared.Shuffle(
                password
            );

            return new string(
                password
            );
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
                UserId =
                    user.Id,

                FullName =
                    user.FullName,

                IdNumber =
                    user.IdNumber,

                Role =
                    user.Role,

                TemporaryPassword =
                    temporaryPassword
            };
        }
    }
}