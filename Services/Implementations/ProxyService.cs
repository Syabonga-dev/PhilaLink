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
            _context = context;
            _audit = audit;
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
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(
                        p => p.Id == patientId
                    );

            if (patient == null)
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
                actor.ClinicId != null &&
                patient.ClinicId != actor.ClinicId
            )
            {
                throw new UnauthorizedAccessException(
                    "Patient does not belong to your clinic."
                );
            }

            if (patient.ClinicId == null)
            {
                throw new InvalidOperationException(
                    "The patient must be assigned to a clinic before a proxy can be assigned."
                );
            }

            var proxy =
                await _context.Proxies
                    .Include(p => p.User)
                    .Include(p => p.Clinic)
                    .FirstOrDefaultAsync(
                        p => p.Id == proxyId
                    );

            if (
                proxy == null ||
                !proxy.User.IsActive ||
                proxy.User.Role != RoleNames.Proxy
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
                proxy.ClinicId != patient.ClinicId.Value
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
                actor.ClinicId != null &&
                proxy.ClinicId != actor.ClinicId.Value
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
                            p.PatientId == patientId &&
                            p.ProxyId == proxyId &&
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

            if (actor.NurseId != null)
            {
                link.AssignedByNurseId =
                    actor.NurseId;
            }
            else if (actor.AdminId != null)
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
                    .Include(p => p.Patient)
                    .Include(p => p.Proxy)
                    .FirstOrDefaultAsync(
                        p => p.Id == proxyLinkId
                    );

            if (link == null)
            {
                throw new KeyNotFoundException(
                    "Proxy link not found."
                );
            }

            if (
                actor.ClinicId != null &&
                link.Patient.ClinicId != actor.ClinicId
            )
            {
                throw new UnauthorizedAccessException(
                    "You cannot modify proxy links outside your clinic."
                );
            }

            if (
                actor.ClinicId != null &&
                link.Proxy.ClinicId != actor.ClinicId.Value
            )
            {
                throw new UnauthorizedAccessException(
                    "You cannot modify proxy links for a proxy outside your clinic."
                );
            }

            if (!link.IsActive)
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
                        p => p.Id == patientId
                    );

            if (patient == null)
            {
                throw new KeyNotFoundException(
                    "Patient not found."
                );
            }

            if (
                actor.ClinicId != null &&
                patient.ClinicId != actor.ClinicId
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
                .Include(pl => pl.Proxy)
                    .ThenInclude(p => p.User)
                .Where(
                    pl =>
                        pl.PatientId == patientId &&
                        pl.IsActive &&
                        patient.ClinicId != null &&
                        pl.Proxy.ClinicId ==
                            patient.ClinicId.Value
                )
                .OrderByDescending(
                    pl => pl.AssignedAt
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
                                pl.Proxy.User.FullName,

                            PhoneNumber =
                                pl.Proxy.User.PhoneNumber,

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
                    .Include(p => p.User)
                    .Include(p => p.Clinic)
                    .FirstOrDefaultAsync(
                        p =>
                            p.UserId == proxyUserId &&
                            p.User.Role == RoleNames.Proxy &&
                            p.User.IsActive
                    );

            if (proxy == null)
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
                .Include(pl => pl.Patient)
                    .ThenInclude(p => p.User)
                .Include(pl => pl.Patient)
                    .ThenInclude(p => p.Clinic)
                .Where(
                    pl =>
                        pl.ProxyId == proxy.Id &&
                        pl.IsActive &&
                        pl.Patient.ClinicId ==
                            proxy.ClinicId
                )
                .OrderBy(
                    pl =>
                        pl.Patient.User.FullName
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
                                pl.Patient.User.FullName,

                            PatientNumber =
                                pl.Patient.PatientNumber,

                            ClinicId =
                                pl.Patient.ClinicId,

                            ClinicName =
                                pl.Patient.Clinic == null
                                    ? null
                                    : pl.Patient.Clinic.Name,

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
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(
                        p =>
                            p.UserId == patientUserId &&
                            p.User.Role == RoleNames.Patient &&
                            p.User.IsActive
                    );

            if (patient == null)
            {
                throw new UnauthorizedAccessException(
                    "Active patient profile not found."
                );
            }

            if (patient.ClinicId == null)
            {
                return null;
            }

            /*
             * Do not expose a legacy Proxy assignment where
             * the Proxy belongs to another clinic.
             */
            return await _context.ProxyLinks
                .Include(pl => pl.Proxy)
                    .ThenInclude(p => p.User)
                .Where(
                    pl =>
                        pl.PatientId == patient.Id &&
                        pl.IsActive &&
                        pl.Proxy.User.IsActive &&
                        pl.Proxy.User.Role ==
                            RoleNames.Proxy &&
                        pl.Proxy.ClinicId ==
                            patient.ClinicId.Value
                )
                .OrderByDescending(
                    pl => pl.AssignedAt
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
                                pl.Proxy.User.FullName,

                            PhoneNumber =
                                pl.Proxy.User.PhoneNumber,

                            Email =
                                pl.Proxy.Email,

                            AssignedAt =
                                pl.AssignedAt,

                            IsActive =
                                pl.IsActive
                        }
                )
                .FirstOrDefaultAsync();
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
                    .Include(u => u.Nurse)
                    .Include(u => u.Admin)
                    .FirstOrDefaultAsync(
                        u => u.Id == userId
                    );

            if (
                user == null ||
                !user.IsActive
            )
            {
                throw new UnauthorizedAccessException();
            }

            if (
                user.Role == RoleNames.Nurse &&
                user.Nurse != null
            )
            {
                return new StaffActor
                {
                    ClinicId =
                        user.Nurse.ClinicId,

                    NurseId =
                        user.Nurse.Id
                };
            }

            if (
                user.Role == RoleNames.ClinicAdmin &&
                user.Admin?.ClinicId != null
            )
            {
                return new StaffActor
                {
                    ClinicId =
                        user.Admin.ClinicId,

                    AdminId =
                        user.Admin.Id
                };
            }

            if (
                user.Role == RoleNames.SuperAdmin &&
                user.Admin != null
            )
            {
                return new StaffActor
                {
                    ClinicId =
                        null,

                    AdminId =
                        user.Admin.Id
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