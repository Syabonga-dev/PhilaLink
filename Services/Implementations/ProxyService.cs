using Microsoft.EntityFrameworkCore;
using PersonalProject.Data;
using PersonalProject.Models.Constants;
using PersonalProject.Models.DTOs;
using PersonalProject.Models.Entities;
using PersonalProject.Services.Interfaces;

namespace PersonalProject.Services.Implementations
{
    public class ProxyService : IProxyService
    {
        private readonly PhilaLinkDbContext _context;

        private readonly IAuditLogService _audit;

        public ProxyService(
            PhilaLinkDbContext context,
            IAuditLogService audit
        )
        {
            _context =
                context;

            _audit =
                audit;
        }

        // =====================================================
        // PROXY PROFILE
        // =====================================================

        public async Task<ProxyMeDto> GetMeAsync(
            Guid proxyUserId
        )
        {
            var proxy =
                await GetActiveProxyProfileAsync(
                    proxyUserId,
                    asTracking: false
                );

            return ToMeDto(
                proxy
            );
        }

        public async Task<ProxyMeDto> UpdateMeAsync(
            Guid proxyUserId,
            UpdateProxyProfileDto dto
        )
        {
            var proxy =
                await GetActiveProxyProfileAsync(
                    proxyUserId,
                    asTracking: true
                );

            if (
                string.IsNullOrWhiteSpace(
                    dto.FullName
                )
            )
            {
                throw new InvalidOperationException(
                    "Full name is required."
                );
            }

            if (
                string.IsNullOrWhiteSpace(
                    dto.PhoneNumber
                )
            )
            {
                throw new InvalidOperationException(
                    "Phone number is required."
                );
            }

            if (
                string.IsNullOrWhiteSpace(
                    dto.Email
                )
            )
            {
                throw new InvalidOperationException(
                    "Email address is required."
                );
            }

            var normalizedPhoneNumber =
                dto.PhoneNumber
                    .Trim();

            var normalizedEmail =
                dto.Email
                    .Trim()
                    .ToLowerInvariant();

            var duplicatePhone =
                await _context.Users
                    .AsNoTracking()
                    .AnyAsync(
                        user =>
                            user.Id !=
                                proxyUserId &&
                            user.PhoneNumber ==
                                normalizedPhoneNumber
                    );

            if (
                duplicatePhone
            )
            {
                throw new InvalidOperationException(
                    "That phone number is already in use."
                );
            }

            /*
             * Email values are canonical lowercase values.
             * Direct equality keeps the Email index usable.
             */
            var duplicateEmail =
                await _context.Users
                    .AsNoTracking()
                    .AnyAsync(
                        user =>
                            user.Id !=
                                proxyUserId &&
                            user.Email ==
                                normalizedEmail
                    );

            if (
                duplicateEmail
            )
            {
                throw new InvalidOperationException(
                    "That email address is already in use."
                );
            }

            var now =
                DateTime.UtcNow;

            proxy.User.FullName =
                dto.FullName
                    .Trim();

            proxy.User.PhoneNumber =
                normalizedPhoneNumber;

            proxy.User.Email =
                normalizedEmail;

            proxy.User.UpdatedAt =
                now;

            proxy.Email =
                normalizedEmail;

            proxy.Gender =
                dto.Gender
                    .Trim();

            proxy.AddressLine1 =
                dto.AddressLine1
                    .Trim();

            proxy.AddressLine2 =
                string.IsNullOrWhiteSpace(
                    dto.AddressLine2
                )
                    ? null
                    : dto.AddressLine2
                        .Trim();

            proxy.Suburb =
                dto.Suburb
                    .Trim();

            proxy.City =
                dto.City
                    .Trim();

            proxy.Province =
                dto.Province
                    .Trim();

            proxy.PostalCode =
                dto.PostalCode
                    .Trim();

            proxy.EmergencyContactName =
                dto.EmergencyContactName
                    .Trim();

            proxy.EmergencyContactPhone =
                dto.EmergencyContactPhone
                    .Trim();

            proxy.EmergencyContactRelationship =
                dto.EmergencyContactRelationship
                    .Trim();

            proxy.UpdatedAt =
                now;

            /*
             * ClinicId intentionally remains immutable here.
             */
            await _context
                .SaveChangesAsync();

            await _audit.LogAsync(
                "ProxyProfileUpdated",
                proxyUserId,
                $"Proxy {proxy.Id} updated their profile.",
                proxy.ClinicId
            );

            return ToMeDto(
                proxy
            );
        }

        // =====================================================
        // ASSIGN PROXY
        // =====================================================

        public async Task AssignProxyAsync(
            Guid patientId,
            Guid proxyId,
            Guid performedByUserId
        )
        {
            var actor =
                await GetStaffActorAsync(
                    performedByUserId
                );

            /*
             * Only Patient.Id and ClinicId are required for
             * assignment authorization.
             */
            var patient =
                await _context.Patients
                    .AsNoTracking()
                    .Where(
                        item =>
                            item.Id ==
                            patientId
                    )
                    .Select(
                        item =>
                            new
                            {
                                item.Id,
                                item.ClinicId
                            }
                    )
                    .FirstOrDefaultAsync();

            if (
                patient ==
                null
            )
            {
                throw new KeyNotFoundException(
                    "Patient not found."
                );
            }

            if (
                actor.ClinicId !=
                    null &&
                patient.ClinicId !=
                    actor.ClinicId
            )
            {
                throw new UnauthorizedAccessException(
                    "Patient does not belong to your clinic."
                );
            }

            if (
                patient.ClinicId ==
                null
            )
            {
                throw new InvalidOperationException(
                    "The patient must be assigned to a clinic before a proxy can be assigned."
                );
            }

            /*
             * Only the fields required for authorization are
             * projected rather than loading Proxy, User and Clinic.
             */
            var proxy =
                await _context.Proxies
                    .AsNoTracking()
                    .Where(
                        item =>
                            item.Id ==
                            proxyId
                    )
                    .Select(
                        item =>
                            new
                            {
                                item.Id,
                                item.ClinicId,

                                UserIsActive =
                                    item.User.IsActive,

                                UserRole =
                                    item.User.Role
                            }
                    )
                    .FirstOrDefaultAsync();

            if (
                proxy ==
                    null ||
                !proxy.UserIsActive ||
                proxy.UserRole !=
                    RoleNames.Proxy
            )
            {
                throw new KeyNotFoundException(
                    "Active proxy account not found."
                );
            }

            if (
                proxy.ClinicId !=
                patient.ClinicId.Value
            )
            {
                throw new InvalidOperationException(
                    "The proxy and patient must belong to the same clinic."
                );
            }

            if (
                actor.ClinicId !=
                    null &&
                proxy.ClinicId !=
                    actor.ClinicId.Value
            )
            {
                throw new UnauthorizedAccessException(
                    "Proxy does not belong to your clinic."
                );
            }

            var exists =
                await _context.ProxyLinks
                    .AsNoTracking()
                    .AnyAsync(
                        link =>
                            link.PatientId ==
                                patientId &&
                            link.ProxyId ==
                                proxyId &&
                            link.IsActive
                    );

            if (
                exists
            )
            {
                throw new InvalidOperationException(
                    "Proxy is already assigned to this patient."
                );
            }

            var link =
                new ProxyLink
                {
                    Id =
                        Guid.NewGuid(),

                    PatientId =
                        patientId,

                    ProxyId =
                        proxyId,

                    AssignedAt =
                        DateTime.UtcNow,

                    IsActive =
                        true,

                    EndedAt =
                        null,

                    EndedByUserId =
                        null
                };

            if (
                actor.NurseId !=
                null
            )
            {
                link.AssignedByNurseId =
                    actor.NurseId;
            }
            else if (
                actor.AdminId !=
                null
            )
            {
                link.AssignedByAdminId =
                    actor.AdminId;
            }

            _context.ProxyLinks.Add(
                link
            );

            await _context
                .SaveChangesAsync();

            await _audit.LogAsync(
                "ProxyAssigned",
                performedByUserId,
                $"Proxy {proxyId} assigned to patient {patientId}."
            );
        }

        // =====================================================
        // REMOVE PROXY
        // =====================================================

        public async Task RemoveProxyAsync(
            Guid proxyLinkId,
            Guid performedByUserId
        )
        {
            var actor =
                await GetStaffActorAsync(
                    performedByUserId
                );

            /*
             * Tracking is required because this link is mutated.
             * Patient and Proxy are reference navigations needed
             * for clinic-boundary authorization.
             */
            var link =
                await _context.ProxyLinks
                    .Include(
                        item =>
                            item.Patient
                    )
                    .Include(
                        item =>
                            item.Proxy
                    )
                    .FirstOrDefaultAsync(
                        item =>
                            item.Id ==
                            proxyLinkId
                    );

            if (
                link ==
                null
            )
            {
                throw new KeyNotFoundException(
                    "Proxy link not found."
                );
            }

            if (
                actor.ClinicId !=
                    null &&
                link.Patient.ClinicId !=
                    actor.ClinicId
            )
            {
                throw new UnauthorizedAccessException(
                    "You cannot modify proxy links outside your clinic."
                );
            }

            if (
                actor.ClinicId !=
                    null &&
                link.Proxy.ClinicId !=
                    actor.ClinicId.Value
            )
            {
                throw new UnauthorizedAccessException(
                    "You cannot modify proxy links for a proxy outside your clinic."
                );
            }

            if (
                !link.IsActive
            )
            {
                throw new InvalidOperationException(
                    "Proxy assignment is already inactive."
                );
            }

            link.IsActive =
                false;

            link.EndedAt =
                DateTime.UtcNow;

            link.EndedByUserId =
                performedByUserId;

            await _context
                .SaveChangesAsync();

            await _audit.LogAsync(
                "ProxyRemoved",
                performedByUserId,
                $"Proxy link {proxyLinkId} removed."
            );
        }

        // =====================================================
        // GET PATIENT PROXIES
        // =====================================================

        public async Task<List<PatientProxyResponseDto>>
            GetPatientProxiesAsync(
                Guid patientId,
                Guid performedByUserId
            )
        {
            var actor =
                await GetStaffActorAsync(
                    performedByUserId
                );

            var patient =
                await _context.Patients
                    .AsNoTracking()
                    .Where(
                        item =>
                            item.Id ==
                            patientId
                    )
                    .Select(
                        item =>
                            new
                            {
                                item.Id,
                                item.ClinicId
                            }
                    )
                    .FirstOrDefaultAsync();

            if (
                patient ==
                null
            )
            {
                throw new KeyNotFoundException(
                    "Patient not found."
                );
            }

            if (
                actor.ClinicId !=
                    null &&
                patient.ClinicId !=
                    actor.ClinicId
            )
            {
                throw new UnauthorizedAccessException(
                    "Patient does not belong to your clinic."
                );
            }

            /*
             * Includes are unnecessary when navigation values are
             * projected directly into the result DTO.
             */
            return await _context.ProxyLinks
                .AsNoTracking()
                .Where(
                    link =>
                        link.PatientId ==
                            patientId &&
                        link.IsActive &&
                        patient.ClinicId !=
                            null &&
                        link.Proxy.ClinicId ==
                            patient.ClinicId.Value
                )
                .OrderByDescending(
                    link =>
                        link.AssignedAt
                )
                .Select(
                    link =>
                        new PatientProxyResponseDto
                        {
                            ProxyLinkId =
                                link.Id,

                            ProxyId =
                                link.ProxyId,

                            ProxyName =
                                link.Proxy
                                    .User
                                    .FullName,

                            PhoneNumber =
                                link.Proxy
                                    .User
                                    .PhoneNumber,

                            AssignedAt =
                                link.AssignedAt
                        }
                )
                .ToListAsync();
        }

        // =====================================================
        // GET CURRENT PROXY PATIENTS
        // =====================================================

        public async Task<List<ProxyPatientResponseDto>>
            GetMyPatientsAsync(
                Guid proxyUserId
            )
        {
            /*
             * Only Proxy.Id and ClinicId are needed to run the
             * patient query.
             */
            var proxy =
                await _context.Proxies
                    .AsNoTracking()
                    .Where(
                        item =>
                            item.UserId ==
                                proxyUserId &&
                            item.User.Role ==
                                RoleNames.Proxy &&
                            item.User.IsActive
                    )
                    .Select(
                        item =>
                            new
                            {
                                item.Id,
                                item.ClinicId
                            }
                    )
                    .FirstOrDefaultAsync();

            if (
                proxy ==
                null
            )
            {
                throw new UnauthorizedAccessException(
                    "Active proxy profile not found."
                );
            }

            return await _context.ProxyLinks
                .AsNoTracking()
                .Where(
                    link =>
                        link.ProxyId ==
                            proxy.Id &&
                        link.IsActive &&
                        link.Patient.ClinicId ==
                            proxy.ClinicId
                )
                .OrderBy(
                    link =>
                        link.Patient
                            .User
                            .FullName
                )
                .Select(
                    link =>
                        new ProxyPatientResponseDto
                        {
                            ProxyLinkId =
                                link.Id,

                            PatientId =
                                link.PatientId,

                            PatientName =
                                link.Patient
                                    .User
                                    .FullName,

                            PatientNumber =
                                link.Patient
                                    .PatientNumber,

                            ClinicId =
                                link.Patient
                                    .ClinicId,

                            ClinicName =
                                link.Patient
                                    .Clinic ==
                                    null
                                    ? null
                                    : link.Patient
                                        .Clinic
                                        .Name,

                            AssignedAt =
                                link.AssignedAt
                        }
                )
                .ToListAsync();
        }

        // =====================================================
        // GET PATIENT'S ASSIGNED PROXY
        // =====================================================

        public async Task<PatientAssignedWorkerDto?>
            GetMyAssignedWorkerAsync(
                Guid patientUserId
            )
        {
            var patient =
                await _context.Patients
                    .AsNoTracking()
                    .Where(
                        item =>
                            item.UserId ==
                                patientUserId &&
                            item.User.Role ==
                                RoleNames.Patient &&
                            item.User.IsActive
                    )
                    .Select(
                        item =>
                            new
                            {
                                item.Id,
                                item.ClinicId
                            }
                    )
                    .FirstOrDefaultAsync();

            if (
                patient ==
                null
            )
            {
                throw new UnauthorizedAccessException(
                    "Active patient profile not found."
                );
            }

            if (
                patient.ClinicId ==
                null
            )
            {
                return null;
            }

            return await _context.ProxyLinks
                .AsNoTracking()
                .Where(
                    link =>
                        link.PatientId ==
                            patient.Id &&
                        link.IsActive &&
                        link.Proxy
                            .User
                            .IsActive &&
                        link.Proxy
                            .User
                            .Role ==
                            RoleNames.Proxy &&
                        link.Proxy.ClinicId ==
                            patient.ClinicId.Value
                )
                .OrderByDescending(
                    link =>
                        link.AssignedAt
                )
                .Select(
                    link =>
                        new PatientAssignedWorkerDto
                        {
                            ProxyLinkId =
                                link.Id,

                            ProxyId =
                                link.ProxyId,

                            FullName =
                                link.Proxy
                                    .User
                                    .FullName,

                            PhoneNumber =
                                link.Proxy
                                    .User
                                    .PhoneNumber,

                            Email =
                                link.Proxy
                                    .Email,

                            AssignedAt =
                                link.AssignedAt,

                            IsActive =
                                link.IsActive
                        }
                )
                .FirstOrDefaultAsync();
        }

        // =====================================================
        // ACTIVE PROXY PROFILE
        // =====================================================

        private async Task<Proxy>
            GetActiveProxyProfileAsync(
                Guid proxyUserId,
                bool asTracking
            )
        {
            IQueryable<Proxy> query =
                _context.Proxies
                    .Include(
                        proxy =>
                            proxy.User
                    )
                    .Include(
                        proxy =>
                            proxy.Clinic
                    );

            if (
                !asTracking
            )
            {
                query =
                    query.AsNoTracking();
            }

            var proxy =
                await query
                    .FirstOrDefaultAsync(
                        item =>
                            item.UserId ==
                                proxyUserId &&
                            item.User.Role ==
                                RoleNames.Proxy &&
                            item.User.IsActive
                    );

            if (
                proxy ==
                null
            )
            {
                throw new UnauthorizedAccessException(
                    "Active proxy profile not found."
                );
            }

            return proxy;
        }

        // =====================================================
        // PROFILE DTO
        // =====================================================

        private static ProxyMeDto ToMeDto(
            Proxy proxy
        )
        {
            return new ProxyMeDto
            {
                ProxyId =
                    proxy.Id,

                UserId =
                    proxy.UserId,

                FullName =
                    proxy.User
                        .FullName,

                IdNumber =
                    proxy.User
                        .IdNumber,

                PhoneNumber =
                    proxy.User
                        .PhoneNumber,

                Email =
                    proxy.User
                        .Email,

                DateOfBirth =
                    proxy.DateOfBirth,

                Gender =
                    proxy.Gender,

                AddressLine1 =
                    proxy.AddressLine1,

                AddressLine2 =
                    proxy.AddressLine2,

                Suburb =
                    proxy.Suburb,

                City =
                    proxy.City,

                Province =
                    proxy.Province,

                PostalCode =
                    proxy.PostalCode,

                EmergencyContactName =
                    proxy
                        .EmergencyContactName,

                EmergencyContactPhone =
                    proxy
                        .EmergencyContactPhone,

                EmergencyContactRelationship =
                    proxy
                        .EmergencyContactRelationship,

                ClinicId =
                    proxy.ClinicId,

                ClinicName =
                    proxy.Clinic
                        .Name,

                IsActive =
                    proxy.User
                        .IsActive,

                IsVerified =
                    proxy.User
                        .IsVerified,

                MustChangePassword =
                    proxy.User
                        .MustChangePassword,

                CreatedAt =
                    proxy.CreatedAt,

                UpdatedAt =
                    proxy.UpdatedAt
            };
        }

        // =====================================================
        // STAFF ACTOR
        // =====================================================

        private async Task<StaffActor>
            GetStaffActorAsync(
                Guid userId
            )
        {
            /*
             * Actor resolution is read-only and very frequent.
             *
             * Project only the security fields needed instead of
             * loading complete User/Nurse/Admin entities.
             */
            var actor =
                await _context.Users
                    .AsNoTracking()
                    .Where(
                        user =>
                            user.Id ==
                            userId
                    )
                    .Select(
                        user =>
                            new
                            {
                                user.IsActive,
                                user.Role,

                                NurseId =
                                    user.Nurse ==
                                    null
                                        ? (Guid?)null
                                        : user.Nurse.Id,

                                NurseClinicId =
                                    user.Nurse ==
                                    null
                                        ? (Guid?)null
                                        : user.Nurse
                                            .ClinicId,

                                AdminId =
                                    user.Admin ==
                                    null
                                        ? (int?)null
                                        : user.Admin.Id,

                                AdminClinicId =
                                    user.Admin ==
                                    null
                                        ? (Guid?)null
                                        : user.Admin
                                            .ClinicId
                            }
                    )
                    .FirstOrDefaultAsync();

            if (
                actor ==
                    null ||
                !actor.IsActive
            )
            {
                throw new UnauthorizedAccessException();
            }

            if (
                actor.Role ==
                    RoleNames.Nurse &&
                actor.NurseId !=
                    null
            )
            {
                return new StaffActor
                {
                    ClinicId =
                        actor.NurseClinicId,

                    NurseId =
                        actor.NurseId
                };
            }

            if (
                actor.Role ==
                    RoleNames.ClinicAdmin &&
                actor.AdminId !=
                    null &&
                actor.AdminClinicId !=
                    null
            )
            {
                return new StaffActor
                {
                    ClinicId =
                        actor.AdminClinicId,

                    AdminId =
                        actor.AdminId
                };
            }

            if (
                actor.Role ==
                    RoleNames.SuperAdmin &&
                actor.AdminId !=
                    null
            )
            {
                return new StaffActor
                {
                    ClinicId =
                        null,

                    AdminId =
                        actor.AdminId
                };
            }

            throw new UnauthorizedAccessException();
        }

        // =====================================================
        // INTERNAL MODEL
        // =====================================================

        private class StaffActor
        {
            public Guid? ClinicId
            {
                get;
                set;
            }

            public Guid? NurseId
            {
                get;
                set;
            }

            public int? AdminId
            {
                get;
                set;
            }
        }
    }
}