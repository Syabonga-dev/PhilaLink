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

            // -------------------------------------------------
            // REQUIRED ACCOUNT FIELDS
            // -------------------------------------------------

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

            // -------------------------------------------------
            // NORMALIZE UNIQUE ACCOUNT DATA
            // -------------------------------------------------

            var normalizedPhoneNumber =
                dto.PhoneNumber
                    .Trim();

            var normalizedEmail =
                dto.Email
                    .Trim()
                    .ToLowerInvariant();

            // -------------------------------------------------
            // DUPLICATE PHONE
            // -------------------------------------------------

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

            if (duplicatePhone)
            {
                throw new InvalidOperationException(
                    "That phone number is already in use."
                );
            }

            // -------------------------------------------------
            // DUPLICATE EMAIL
            // -------------------------------------------------

            var duplicateEmail =
                await _context.Users
                    .AsNoTracking()
                    .AnyAsync(
                        user =>
                            user.Id !=
                                proxyUserId &&
                            user.Email
                                .ToLower() ==
                                normalizedEmail
                    );

            if (duplicateEmail)
            {
                throw new InvalidOperationException(
                    "That email address is already in use."
                );
            }

            var now =
                DateTime.UtcNow;

            // -------------------------------------------------
            // USER ACCOUNT
            // -------------------------------------------------

            proxy.User.FullName =
                dto.FullName
                    .Trim();

            proxy.User.PhoneNumber =
                normalizedPhoneNumber;

            proxy.User.Email =
                normalizedEmail;

            proxy.User.UpdatedAt =
                now;

            // -------------------------------------------------
            // PROXY PROFILE
            // -------------------------------------------------

            /*
             * Keep the profile email synchronized with the
             * canonical Users record, matching the existing
             * Patient profile behaviour.
             */
            proxy.Email =
                normalizedEmail;

            proxy.DateOfBirth =
                dto.DateOfBirth;

            proxy.Gender =
                dto.Gender
                    .Trim();

            proxy.RelationshipToPatient =
                dto.RelationshipToPatient
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

            // -------------------------------------------------
            // IMPORTANT SECURITY BOUNDARY
            // -------------------------------------------------
            //
            // The update DTO deliberately does not contain
            // ClinicId.
            //
            // A Proxy must not be able to move themselves to
            // another clinic because Proxy.ClinicId is used by
            // the patient and collection authorization logic.
            // -------------------------------------------------

            await _context.SaveChangesAsync();

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

            var patient =
                await _context.Patients
                    .Include(
                        p =>
                            p.User
                    )
                    .FirstOrDefaultAsync(
                        p =>
                            p.Id ==
                            patientId
                    );

            if (
                patient ==
                null
            )
            {
                throw new KeyNotFoundException(
                    "Patient not found."
                );
            }

            /*
             * Clinic staff may only manage patients belonging
             * to their own clinic.
             *
             * SuperAdmin has actor.ClinicId == null and may
             * therefore work across clinics.
             */
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

            var proxy =
                await _context.Proxies
                    .Include(
                        p =>
                            p.User
                    )
                    .Include(
                        p =>
                            p.Clinic
                    )
                    .FirstOrDefaultAsync(
                        p =>
                            p.Id ==
                            proxyId
                    );

            if (
                proxy ==
                    null ||
                !proxy.User.IsActive ||
                proxy.User.Role !=
                    RoleNames.Proxy
            )
            {
                throw new KeyNotFoundException(
                    "Active proxy account not found."
                );
            }

            /*
             * CRITICAL CLINIC BOUNDARY
             *
             * A Proxy may only be linked to patients belonging
             * to the clinic where the Proxy is registered.
             */
            if (
                proxy.ClinicId !=
                patient.ClinicId.Value
            )
            {
                throw new InvalidOperationException(
                    "The proxy and patient must belong to the same clinic."
                );
            }

            /*
             * A ClinicAdmin/Nurse must also be operating inside
             * the same clinic as the Proxy.
             */
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
                    .AnyAsync(
                        p =>
                            p.PatientId ==
                                patientId &&
                            p.ProxyId ==
                                proxyId &&
                            p.IsActive
                    );

            if (exists)
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

            await _context.SaveChangesAsync();

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

            var link =
                await _context.ProxyLinks
                    .Include(
                        p =>
                            p.Patient
                    )
                    .Include(
                        p =>
                            p.Proxy
                    )
                    .FirstOrDefaultAsync(
                        p =>
                            p.Id ==
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

            await _context.SaveChangesAsync();

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
                    .FirstOrDefaultAsync(
                        p =>
                            p.Id ==
                            patientId
                    );

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
             * Only return Proxy assignments that are valid
             * for the patient's current clinic.
             *
             * This also prevents legacy cross-clinic links
             * from being exposed.
             */
            return await _context.ProxyLinks
                .Include(
                    pl =>
                        pl.Proxy
                )
                    .ThenInclude(
                        p =>
                            p.User
                    )
                .Where(
                    pl =>
                        pl.PatientId ==
                            patientId &&
                        pl.IsActive &&
                        patient.ClinicId !=
                            null &&
                        pl.Proxy.ClinicId ==
                            patient.ClinicId.Value
                )
                .OrderByDescending(
                    pl =>
                        pl.AssignedAt
                )
                .Select(
                    pl =>
                        new PatientProxyResponseDto
                        {
                            ProxyLinkId =
                                pl.Id,

                            ProxyId =
                                pl.ProxyId,

                            ProxyName =
                                pl.Proxy
                                    .User
                                    .FullName,

                            PhoneNumber =
                                pl.Proxy
                                    .User
                                    .PhoneNumber,

                            AssignedAt =
                                pl.AssignedAt
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
            var proxy =
                await _context.Proxies
                    .Include(
                        p =>
                            p.User
                    )
                    .Include(
                        p =>
                            p.Clinic
                    )
                    .FirstOrDefaultAsync(
                        p =>
                            p.UserId ==
                                proxyUserId &&
                            p.User.Role ==
                                RoleNames.Proxy &&
                            p.User.IsActive
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

            /*
             * CRITICAL CLINIC FILTER
             *
             * A Proxy can only retrieve linked patients whose
             * ClinicId matches the Proxy's own ClinicId.
             *
             * This protects the API even if an old or manually
             * inserted ProxyLink exists across clinics.
             */
            return await _context.ProxyLinks
                .AsNoTracking()
                .Include(
                    pl =>
                        pl.Patient
                )
                    .ThenInclude(
                        p =>
                            p.User
                    )
                .Include(
                    pl =>
                        pl.Patient
                )
                    .ThenInclude(
                        p =>
                            p.Clinic
                    )
                .Where(
                    pl =>
                        pl.ProxyId ==
                            proxy.Id &&
                        pl.IsActive &&
                        pl.Patient.ClinicId ==
                            proxy.ClinicId
                )
                .OrderBy(
                    pl =>
                        pl.Patient
                            .User
                            .FullName
                )
                .Select(
                    pl =>
                        new ProxyPatientResponseDto
                        {
                            ProxyLinkId =
                                pl.Id,

                            PatientId =
                                pl.PatientId,

                            PatientName =
                                pl.Patient
                                    .User
                                    .FullName,

                            PatientNumber =
                                pl.Patient
                                    .PatientNumber,

                            ClinicId =
                                pl.Patient
                                    .ClinicId,

                            ClinicName =
                                pl.Patient
                                    .Clinic ==
                                    null
                                    ? null
                                    : pl.Patient
                                        .Clinic
                                        .Name,

                            AssignedAt =
                                pl.AssignedAt
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
                    .Include(
                        p =>
                            p.User
                    )
                    .FirstOrDefaultAsync(
                        p =>
                            p.UserId ==
                                patientUserId &&
                            p.User.Role ==
                                RoleNames.Patient &&
                            p.User.IsActive
                    );

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

            /*
             * Do not expose a legacy Proxy assignment where
             * the Proxy belongs to another clinic.
             */
            return await _context.ProxyLinks
                .Include(
                    pl =>
                        pl.Proxy
                )
                    .ThenInclude(
                        p =>
                            p.User
                    )
                .Where(
                    pl =>
                        pl.PatientId ==
                            patient.Id &&
                        pl.IsActive &&
                        pl.Proxy
                            .User
                            .IsActive &&
                        pl.Proxy
                            .User
                            .Role ==
                            RoleNames.Proxy &&
                        pl.Proxy.ClinicId ==
                            patient.ClinicId.Value
                )
                .OrderByDescending(
                    pl =>
                        pl.AssignedAt
                )
                .Select(
                    pl =>
                        new PatientAssignedWorkerDto
                        {
                            ProxyLinkId =
                                pl.Id,

                            ProxyId =
                                pl.ProxyId,

                            FullName =
                                pl.Proxy
                                    .User
                                    .FullName,

                            PhoneNumber =
                                pl.Proxy
                                    .User
                                    .PhoneNumber,

                            Email =
                                pl.Proxy
                                    .Email,

                            AssignedAt =
                                pl.AssignedAt,

                            IsActive =
                                pl.IsActive
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

                RelationshipToPatient =
                    proxy.RelationshipToPatient,

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
                    proxy.EmergencyContactName,

                EmergencyContactPhone =
                    proxy.EmergencyContactPhone,

                EmergencyContactRelationship =
                    proxy.EmergencyContactRelationship,

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
            var user =
                await _context.Users
                    .Include(
                        u =>
                            u.Nurse
                    )
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
                user ==
                    null ||
                !user.IsActive
            )
            {
                throw new UnauthorizedAccessException();
            }

            if (
                user.Role ==
                    RoleNames.Nurse &&
                user.Nurse !=
                    null
            )
            {
                return new StaffActor
                {
                    ClinicId =
                        user.Nurse
                            .ClinicId,

                    NurseId =
                        user.Nurse
                            .Id
                };
            }

            if (
                user.Role ==
                    RoleNames.ClinicAdmin &&
                user.Admin?.ClinicId !=
                    null
            )
            {
                return new StaffActor
                {
                    ClinicId =
                        user.Admin
                            .ClinicId,

                    AdminId =
                        user.Admin
                            .Id
                };
            }

            if (
                user.Role ==
                    RoleNames.SuperAdmin &&
                user.Admin !=
                    null
            )
            {
                return new StaffActor
                {
                    ClinicId =
                        null,

                    AdminId =
                        user.Admin
                            .Id
                };
            }

            throw new UnauthorizedAccessException();
        }

        // =====================================================
        // INTERNAL MODEL
        // =====================================================

        private class StaffActor
        {
            public Guid? ClinicId { get; set; }

            public Guid? NurseId { get; set; }

            public int? AdminId { get; set; }
        }
    }
}