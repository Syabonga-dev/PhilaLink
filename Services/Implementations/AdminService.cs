using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PersonalProject.Data;
using PersonalProject.Models;
using PersonalProject.Models.Constants;
using PersonalProject.Models.DTOs;
using PersonalProject.Models.Entities;
using PersonalProject.Services.Interfaces;
using PersonalProject.Utilities;

namespace PersonalProject.Services.Implementations
{
    public class AdminService :
        IAdminService
    {
        private readonly PhilaLinkDbContext
            _context;

        private readonly IConfiguration
            _configuration;

        private readonly ILogger<AdminService>
            _logger;

        public AdminService(
            PhilaLinkDbContext context,
            IConfiguration configuration,
            ILogger<AdminService> logger
        )
        {
            _context =
                context;

            _configuration =
                configuration;

            _logger =
                logger;
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

            var clinic =
                await _context.Clinics
                    .FirstOrDefaultAsync(
                        item =>
                            item.Id ==
                                dto.ClinicId &&
                            item.IsActive
                    );

            if (
                clinic ==
                null
            )
            {
                throw new KeyNotFoundException(
                    "Active clinic not found."
                );
            }

            var (
                user,
                temporaryPassword
            ) =
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
                        clinic.Id,

                    CreatedAt =
                        DateTime.UtcNow
                };

            _context.Admins.Add(
                admin
            );

            /*
             * User + Admin are persisted by one SaveChanges.
             * EF Core automatically treats the operation
             * transactionally and it remains compatible with
             * Npgsql EnableRetryOnFailure.
             */
            await _context
                .SaveChangesAsync();

            var emailSent =
                await TrySendInvitationAsync(
                    user,
                    temporaryPassword,
                    clinic.Name
                );

            return CreateNewAccountResponse(
                user,
                clinic.Id,
                clinic.Name,
                emailSent
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

            EnsureClinicAdminAccess(
                actor,
                dto.ClinicId
            );

            var clinic =
                await _context.Clinics
                    .FirstOrDefaultAsync(
                        item =>
                            item.Id ==
                                dto.ClinicId &&
                            item.IsActive
                    );

            if (
                clinic ==
                null
            )
            {
                throw new KeyNotFoundException(
                    "Active clinic not found."
                );
            }

            var employeeNumber =
                dto.EmployeeNumber
                    .Trim();

            var registrationNumber =
                dto.RegistrationNumber
                    .Trim();

            var professionalIdentifierExists =
                await _context.Nurses
                    .AnyAsync(
                        nurse =>
                            nurse.EmployeeNumber ==
                                employeeNumber ||
                            nurse.RegistrationNumber ==
                                registrationNumber
                    );

            if (
                professionalIdentifierExists
            )
            {
                throw new InvalidOperationException(
                    "A nurse with that employee number or registration number already exists."
                );
            }

            var (
                user,
                temporaryPassword
            ) =
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
                        dto.Qualification
                            .Trim(),

                    ClinicId =
                        clinic.Id,

                    Email =
                        user.Email,

                    AddressLine1 =
                        dto.AddressLine1
                            .Trim(),

                    AddressLine2 =
                        string.IsNullOrWhiteSpace(
                            dto.AddressLine2
                        )
                            ? null
                            : dto.AddressLine2
                                .Trim(),

                    Suburb =
                        dto.Suburb
                            .Trim(),

                    City =
                        dto.City
                            .Trim(),

                    Province =
                        dto.Province
                            .Trim(),

                    PostalCode =
                        dto.PostalCode
                            .Trim(),

                    DateOfBirth =
                        dto.DateOfBirth,

                    Gender =
                        dto.Gender
                            .Trim(),

                    EmploymentDate =
                        dto.EmploymentDate,

                    EmergencyContactName =
                        dto.EmergencyContactName
                            .Trim(),

                    EmergencyContactPhone =
                        dto.EmergencyContactPhone
                            .Trim(),

                    EmergencyContactRelationship =
                        dto.EmergencyContactRelationship
                            .Trim(),

                    CreatedAt =
                        DateTime.UtcNow
                };

            _context.Nurses.Add(
                nurse
            );

            await _context
                .SaveChangesAsync();

            var emailSent =
                await TrySendInvitationAsync(
                    user,
                    temporaryPassword,
                    clinic.Name
                );

            return CreateNewAccountResponse(
                user,
                clinic.Id,
                clinic.Name,
                emailSent
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
            var actor =
                await GetAdminActorAsync(
                    performedByUserId
                );

            EnsureClinicAdminAccess(
                actor,
                dto.ClinicId
            );

            var clinic =
                await _context.Clinics
                    .FirstOrDefaultAsync(
                        item =>
                            item.Id ==
                                dto.ClinicId &&
                            item.IsActive
                    );

            if (
                clinic ==
                null
            )
            {
                throw new KeyNotFoundException(
                    "Active clinic not found."
                );
            }

            var (
                user,
                temporaryPassword
            ) =
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

                    ClinicId =
                        clinic.Id,

                    Email =
                        user.Email,

                    AddressLine1 =
                        dto.AddressLine1
                            .Trim(),

                    AddressLine2 =
                        string.IsNullOrWhiteSpace(
                            dto.AddressLine2
                        )
                            ? null
                            : dto.AddressLine2
                                .Trim(),

                    Suburb =
                        dto.Suburb
                            .Trim(),

                    City =
                        dto.City
                            .Trim(),

                    Province =
                        dto.Province
                            .Trim(),

                    PostalCode =
                        dto.PostalCode
                            .Trim(),

                    DateOfBirth =
                        SouthAfricanIdNumber
                            .GetDateOfBirth(
                                user.IdNumber
                            ),

                    Gender =
                        dto.Gender
                            .Trim(),

                    EmergencyContactName =
                        dto.EmergencyContactName
                            .Trim(),

                    EmergencyContactPhone =
                        dto.EmergencyContactPhone
                            .Trim(),

                    EmergencyContactRelationship =
                        dto.EmergencyContactRelationship
                            .Trim(),

                    CreatedAt =
                        DateTime.UtcNow
                };

            _context.Proxies.Add(
                proxy
            );

            await _context
                .SaveChangesAsync();

            var emailSent =
                await TrySendInvitationAsync(
                    user,
                    temporaryPassword,
                    clinic.Name
                );

            return CreateNewAccountResponse(
                user,
                clinic.Id,
                clinic.Name,
                emailSent
            );
        }

        // =====================================================
        // RESEND ACCOUNT INVITATION
        // =====================================================

        public async Task<NewStaffAccountDto>
            ResendAccountInvitationAsync(
                Guid targetUserId,
                Guid performedByUserId
            )
        {
            var actor =
                await GetAdminActorAsync(
                    performedByUserId
                );

            var target =
                await _context.Users
                    .Include(
                        user =>
                            user.Admin
                    )
                    .ThenInclude(
                        admin =>
                            admin!.Clinic
                    )
                    .Include(
                        user =>
                            user.Nurse
                    )
                    .ThenInclude(
                        nurse =>
                            nurse!.Clinic
                    )
                    .Include(
                        user =>
                            user.Proxy
                    )
                    .ThenInclude(
                        proxy =>
                            proxy!.Clinic
                    )
                    .FirstOrDefaultAsync(
                        user =>
                            user.Id ==
                            targetUserId
                    );

            if (
                target ==
                null
            )
            {
                throw new KeyNotFoundException(
                    "Account not found."
                );
            }

            if (
                !target.IsActive
            )
            {
                throw new InvalidOperationException(
                    "The account is inactive. Activate it before resending an invitation."
                );
            }

            if (
                !target.MustChangePassword
            )
            {
                throw new InvalidOperationException(
                    "This account has already completed first-login password setup. Use the normal password-reset flow instead."
                );
            }

            Guid? clinicId;
            string? clinicName;

            switch (
                target.Role
            )
            {
                case RoleNames.ClinicAdmin:
                {
                    if (
                        actor.User.Role !=
                        RoleNames.SuperAdmin
                    )
                    {
                        throw new UnauthorizedAccessException(
                            "Only a SuperAdmin can resend a Clinic Administrator invitation."
                        );
                    }

                    clinicId =
                        target.Admin
                            ?.ClinicId;

                    clinicName =
                        target.Admin
                            ?.Clinic
                            ?.Name;

                    break;
                }

                case RoleNames.Nurse:
                {
                    if (
                        actor.User.Role !=
                        RoleNames.ClinicAdmin
                    )
                    {
                        throw new UnauthorizedAccessException(
                            "Only a Clinic Administrator can resend a Nurse invitation."
                        );
                    }

                    clinicId =
                        target.Nurse
                            ?.ClinicId;

                    clinicName =
                        target.Nurse
                            ?.Clinic
                            ?.Name;

                    EnsureClinicAdminAccess(
                        actor,
                        clinicId ??
                            Guid.Empty
                    );

                    break;
                }

                case RoleNames.Proxy:
                {
                    if (
                        actor.User.Role !=
                        RoleNames.ClinicAdmin
                    )
                    {
                        throw new UnauthorizedAccessException(
                            "Only a Clinic Administrator can resend a Proxy invitation."
                        );
                    }

                    clinicId =
                        target.Proxy
                            ?.ClinicId;

                    clinicName =
                        target.Proxy
                            ?.Clinic
                            ?.Name;

                    EnsureClinicAdminAccess(
                        actor,
                        clinicId ??
                            Guid.Empty
                    );

                    break;
                }

                default:
                    throw new InvalidOperationException(
                        "This account type does not use administrator-issued invitations."
                    );
            }

            if (
                string.IsNullOrWhiteSpace(
                    target.Email
                )
            )
            {
                throw new InvalidOperationException(
                    "The account does not have an email address."
                );
            }

            var temporaryPassword =
                GenerateTempPassword();

            /*
             * Resending an invitation invalidates the previous
             * temporary credential.
             */
            target.PasswordHash =
                BCrypt.Net.BCrypt
                    .HashPassword(
                        temporaryPassword
                    );

            target.ClearLoginAbuseState();

            target.MustChangePassword =
                true;

            target.UpdatedAt =
                DateTime.UtcNow;

            await _context
                .SaveChangesAsync();

            var emailSent =
                await TrySendInvitationAsync(
                    target,
                    temporaryPassword,
                    clinicName
                );

            return CreateNewAccountResponse(
                target,
                clinicId,
                clinicName,
                emailSent
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
                    .Include(
                        user =>
                            user.Nurse
                    )
                    .Include(
                        user =>
                            user.Proxy
                    )
                    .Include(
                        user =>
                            user.Patient
                    )
                    .Include(
                        user =>
                            user.Admin
                    )
                    .AsQueryable();

            if (
                !string.IsNullOrWhiteSpace(
                    role
                )
            )
            {
                query =
                    query.Where(
                        user =>
                            user.Role ==
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
                    actor.Admin
                        .ClinicId
                        .Value;

                query =
                    query.Where(
                        user =>
                            (
                                user.Role ==
                                    RoleNames.Nurse &&
                                user.Nurse !=
                                    null &&
                                user.Nurse.ClinicId ==
                                    clinicId
                            )
                            ||
                            (
                                user.Role ==
                                    RoleNames.Patient &&
                                user.Patient !=
                                    null &&
                                user.Patient.ClinicId ==
                                    clinicId
                            )
                            ||
                            user.Id ==
                                performedByUserId
                    );
            }

            var users =
                await query
                    .OrderBy(
                        user =>
                            user.FullName
                    )
                    .ToListAsync();

            return users
                .Select(
                    user =>
                        new AdminAccountDto
                        {
                            UserId =
                                user.Id,

                            FullName =
                                user.FullName,

                            IdNumber =
                                user.IdNumber,

                            Role =
                                user.Role,

                            IsActive =
                                user.IsActive
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
                                user =>
                                    user.Role ==
                                        RoleNames.Nurse &&
                                    user.IsActive
                            ),

                    TotalProxies =
                        await _context.Proxies
                            .CountAsync(),

                    ActiveProxies =
                        await _context.Users
                            .CountAsync(
                                user =>
                                    user.Role ==
                                        RoleNames.Proxy &&
                                    user.IsActive
                            ),

                    TotalPatients =
                        await _context.Patients
                            .CountAsync(),

                    ActivePatients =
                        await _context.Users
                            .CountAsync(
                                user =>
                                    user.Role ==
                                        RoleNames.Patient &&
                                    user.IsActive
                            ),

                    TotalProxyLinks =
                        await _context.ProxyLinks
                            .CountAsync(
                                link =>
                                    link.IsActive
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
                actor.Admin
                    .ClinicId
                    .Value;

            var clinicPatientIds =
                _context.Patients
                    .Where(
                        patient =>
                            patient.ClinicId ==
                            clinicId
                    )
                    .Select(
                        patient =>
                            patient.Id
                    );

            return new AdminDashboardDto
            {
                TotalNurses =
                    await _context.Nurses
                        .CountAsync(
                            nurse =>
                                nurse.ClinicId ==
                                clinicId
                        ),

                ActiveNurses =
                    await _context.Nurses
                        .Where(
                            nurse =>
                                nurse.ClinicId ==
                                clinicId
                        )
                        .CountAsync(
                            nurse =>
                                nurse.User
                                    .IsActive
                        ),

                TotalPatients =
                    await _context.Patients
                        .CountAsync(
                            patient =>
                                patient.ClinicId ==
                                clinicId
                        ),

                ActivePatients =
                    await _context.Patients
                        .Where(
                            patient =>
                                patient.ClinicId ==
                                clinicId
                        )
                        .CountAsync(
                            patient =>
                                patient.User
                                    .IsActive
                        ),

                TotalProxies =
                    await _context.ProxyLinks
                        .Where(
                            link =>
                                clinicPatientIds
                                    .Contains(
                                        link.PatientId
                                    ) &&
                                link.IsActive
                        )
                        .Select(
                            link =>
                                link.ProxyId
                        )
                        .Distinct()
                        .CountAsync(),

                ActiveProxies =
                    await _context.ProxyLinks
                        .Where(
                            link =>
                                clinicPatientIds
                                    .Contains(
                                        link.PatientId
                                    ) &&
                                link.IsActive &&
                                link.Proxy
                                    .User
                                    .IsActive
                        )
                        .Select(
                            link =>
                                link.ProxyId
                        )
                        .Distinct()
                        .CountAsync(),

                TotalProxyLinks =
                    await _context.ProxyLinks
                        .CountAsync(
                            link =>
                                clinicPatientIds
                                    .Contains(
                                        link.PatientId
                                    ) &&
                                link.IsActive
                        )
            };
        }

        // =====================================================
        // DEACTIVATE / ACTIVATE
        // =====================================================

        public async Task
            DeactivateAccountAsync(
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

        public async Task
            ActivateAccountAsync(
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
                        item =>
                            item.Id ==
                            actor.Admin
                                .ClinicId
                                .Value
                    );

            if (
                clinic ==
                null
            )
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
                actor.Admin
                    .ClinicId
                    .Value;

            var clinic =
                await _context.Clinics
                    .FirstOrDefaultAsync(
                        item =>
                            item.Id ==
                            clinicId
                    );

            if (
                clinic ==
                null
            )
            {
                throw new KeyNotFoundException(
                    "Assigned clinic not found."
                );
            }

            var today =
                DateTime.UtcNow
                    .Date;

            var tomorrow =
                today.AddDays(
                    1
                );

            var activePatients =
                await _context.Patients
                    .CountAsync(
                        patient =>
                            patient.ClinicId ==
                                clinicId &&
                            patient.User
                                .IsActive
                    );

            var activeNurses =
                await _context.Nurses
                    .CountAsync(
                        nurse =>
                            nurse.ClinicId ==
                                clinicId &&
                            nurse.User
                                .IsActive
                    );

            var appointmentsToday =
                await _context.Appointments
                    .CountAsync(
                        appointment =>
                            appointment.ClinicId ==
                                clinicId &&
                            appointment.ScheduledAt >=
                                today &&
                            appointment.ScheduledAt <
                                tomorrow &&
                            appointment.Status !=
                                AppointmentStatuses.Cancelled
                    );

            var collectionsDueToday =
                await _context
                    .MedicationCollections
                    .CountAsync(
                        collection =>
                            collection.ClinicId ==
                                clinicId &&
                            collection.ScheduledCollectionDate >=
                                today &&
                            collection.ScheduledCollectionDate <
                                tomorrow &&
                            collection.Status !=
                                MedicationCollectionStatuses.Collected &&
                            collection.Status !=
                                MedicationCollectionStatuses.Cancelled
                    );

            var overdueCollections =
                await _context
                    .MedicationCollections
                    .CountAsync(
                        collection =>
                            collection.ClinicId ==
                                clinicId &&
                            collection.ScheduledCollectionDate <
                                today &&
                            collection.Status !=
                                MedicationCollectionStatuses.Collected &&
                            collection.Status !=
                                MedicationCollectionStatuses.Cancelled
                    );

            var lowStockItems =
                await _context.ClinicStocks
                    .CountAsync(
                        stock =>
                            stock.ClinicId ==
                                clinicId &&
                            stock.IsActive &&
                            stock.QuantityOnHand <=
                                stock.ReorderLevel
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
        // ACCOUNT STATUS
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
                    .Include(
                        user =>
                            user.Nurse
                    )
                    .Include(
                        user =>
                            user.Patient
                    )
                    .FirstOrDefaultAsync(
                        user =>
                            user.Id ==
                            targetUserId
                    );

            if (
                target ==
                null
            )
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
                    actor.Admin
                        .ClinicId
                        .Value;

                var allowed =
                    (
                        target.Role ==
                            RoleNames.Nurse &&
                        target.Nurse
                            ?.ClinicId ==
                            clinicId
                    )
                    ||
                    (
                        target.Role ==
                            RoleNames.Patient &&
                        target.Patient
                            ?.ClinicId ==
                            clinicId
                    );

                if (
                    !allowed
                )
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

            target.IsActive =
                isActive;

            target.UpdatedAt =
                DateTime.UtcNow;

            await _context
                .SaveChangesAsync();
        }

        // =====================================================
        // CREATE USER
        // =====================================================

        private async Task<(
            User User,
            string TemporaryPassword
        )>
            CreateUserAsync(
                string fullName,
                string idNumber,
                string phoneNumber,
                string email,
                string role
            )
        {
            if (
                !RoleNames.IsValid(
                    role
                )
            )
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
                email
                    .Trim()
                    .ToLowerInvariant();

            var exists =
                await _context.Users
                    .AnyAsync(
                        user =>
                            user.IdNumber ==
                                normalizedIdNumber ||
                            user.PhoneNumber ==
                                normalizedPhoneNumber ||
                            user.Email
                                .ToLower() ==
                                normalizedEmail
                    );

            if (
                exists
            )
            {
                throw new InvalidOperationException(
                    "An account with that ID number, phone number, or email address already exists."
                );
            }

            var temporaryPassword =
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
                                temporaryPassword
                            ),

                    Role =
                        role,

                    IsActive =
                        true,

                    IsVerified =
                        true,

                    VerifiedAt =
                        now,

                    MustChangePassword =
                        true,

                    CreatedAt =
                        now
                };

            /*
             * Do not save here.
             *
             * The caller adds the corresponding Admin, Nurse or
             * Proxy profile before the single SaveChanges call.
             */
            _context.Users.Add(
                user
            );

            return (
                user,
                temporaryPassword
            );
        }

        // =====================================================
        // ADMIN ACTOR
        // =====================================================

        private async Task<(
            User User,
            Admin Admin
        )>
            GetAdminActorAsync(
                Guid userId
            )
        {
            var user =
                await _context.Users
                    .Include(
                        item =>
                            item.Admin
                    )
                    .FirstOrDefaultAsync(
                        item =>
                            item.Id ==
                            userId
                    );

            if (
                user ==
                    null ||
                user.Admin ==
                    null
            )
            {
                throw new UnauthorizedAccessException(
                    "Administrator account not found."
                );
            }

            if (
                !user.IsActive
            )
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
        // CLINIC ADMIN ACCESS
        // =====================================================

        private static void
            EnsureClinicAdminAccess(
                (
                    User User,
                    Admin Admin
                )
                actor,
                Guid clinicId
            )
        {
            if (
                actor.User.Role !=
                RoleNames.ClinicAdmin
            )
            {
                throw new UnauthorizedAccessException(
                    "Only a Clinic Administrator can create or manage clinic workforce accounts."
                );
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
        }

        // =====================================================
        // SEND INVITATION
        // =====================================================

        private async Task<bool>
            TrySendInvitationAsync(
                User user,
                string temporaryPassword,
                string? clinicName
            )
        {
            try
            {
                await AccountEmailSender
                    .SendAccountInvitationAsync(
                        _configuration,
                        _logger,
                        user.Email,
                        user.FullName,
                        user.Role,
                        clinicName,
                        temporaryPassword
                    );

                return true;
            }
            catch (
                Exception ex
            )
            {
                /*
                 * The account remains valid but the administrator
                 * never receives the password.
                 *
                 * A resend regenerates a completely new temporary
                 * password and invalidates the undelivered one.
                 */
                _logger.LogWarning(
                    ex,
                    "Account {UserId} was created/updated but its invitation email could not be delivered.",
                    user.Id
                );

                return false;
            }
        }

        // =====================================================
        // TEMPORARY PASSWORD
        // =====================================================

        private static string
            GenerateTempPassword()
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
                var index = 4;
                index <
                password.Length;
                index++
            )
            {
                password[index] =
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
                Guid? clinicId,
                string? clinicName,
                bool emailSent
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

                Email =
                    user.Email,

                Role =
                    user.Role,

                ClinicId =
                    clinicId,

                ClinicName =
                    clinicName,

                EmailSent =
                    emailSent,

                Message =
                    emailSent
                        ? $"The account was created and login credentials were sent to {user.Email}."
                        : $"The account was created, but the credential email could not be delivered to {user.Email}. Use Resend invitation to generate and send a new temporary password."
            };
        }
    }
}
