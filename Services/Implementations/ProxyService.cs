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

        public async Task AssignProxyAsync(
            Guid patientId,
            Guid proxyId,
            Guid performedByUserId
        )
        {
            var actor = await GetStaffActorAsync(performedByUserId);

            var patient =await _context.Patients
                            .Include(p => p.User)
                            .FirstOrDefaultAsync(
                                p => p.Id == patientId);

            if (patient == null)
            {
                throw new KeyNotFoundException(
                    "Patient not found."
                );
            }

            if (
                actor.ClinicId != null &&
                patient.ClinicId !=
                    actor.ClinicId
            )
            {
                throw new UnauthorizedAccessException(
                    "Patient does not belong to your clinic."
                );
            }

            var proxy =
                await _context.Proxies
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(
                        p => p.Id == proxyId
                    );

            if (
                proxy == null ||
                !proxy.User.IsActive ||
                proxy.User.Role !=
                    RoleNames.Proxy
            )
            {
                throw new KeyNotFoundException(
                    "Active proxy account not found."
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

                    IsActive = true,
                    EndedAt = null,
                    EndedByUserId = null
                };

            if (actor.NurseId != null)
            {
                link.AssignedByNurseId = actor.NurseId;
            }
            else if (actor.AdminId != null)
            {
                link.AssignedByAdminId = actor.AdminId;
            }

            _context.ProxyLinks.Add(link);

            await _context.SaveChangesAsync();

            await _audit.LogAsync(
                "ProxyAssigned",
                performedByUserId,
                $"Proxy {proxyId} assigned to patient {patientId}."
            );
        }

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
                link.Patient.ClinicId !=
                    actor.ClinicId
            )
            {
                throw new UnauthorizedAccessException(
                    "You cannot modify proxy links outside your clinic."
                );
            }

            if (!link.IsActive)
            {
                throw new InvalidOperationException(
                    "Proxy assignment is already inactive."
                );
            }

            link.IsActive = false;

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

        public async Task<
            List<PatientProxyResponseDto>>
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
                patient.ClinicId !=
                    actor.ClinicId
            )
            {
                throw new UnauthorizedAccessException(
                    "Patient does not belong to your clinic."
                );
            }

            return await _context.ProxyLinks
                .Include(pl => pl.Proxy)
                    .ThenInclude(p => p.User)
                .Where(
                    pl =>
                        pl.PatientId ==
                            patientId &&
                        pl.IsActive
                )
                .OrderByDescending(
                    pl => pl.AssignedAt
                )
                .Select(pl =>
                    new PatientProxyResponseDto
                    {
                        ProxyLinkId =
                            pl.Id,

                        ProxyId =
                            pl.ProxyId,

                        ProxyName =
                            pl.Proxy.User
                                .FullName,

                        PhoneNumber =
                            pl.Proxy.User
                                .PhoneNumber,

                        AssignedAt =
                            pl.AssignedAt
                    }
                )
                .ToListAsync();
        }

        public async Task<
            List<ProxyPatientResponseDto>>
            GetMyPatientsAsync(
                Guid proxyUserId
            )
        {
            var proxy =
                await _context.Proxies
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(
                        p =>
                            p.UserId ==
                                proxyUserId &&
                            p.User.Role ==
                                RoleNames.Proxy &&
                            p.User.IsActive
                    );

            if (proxy == null)
            {
                throw new UnauthorizedAccessException(
                    "Active proxy profile not found."
                );
            }

            return await _context.ProxyLinks
                .Include(pl => pl.Patient)
                    .ThenInclude(p => p.User)
                .Include(pl => pl.Patient)
                    .ThenInclude(p => p.Clinic)
                .Where(
                    pl =>
                        pl.ProxyId ==
                            proxy.Id &&
                        pl.IsActive
                )
                .OrderBy(
                    pl =>
                        pl.Patient.User
                            .FullName
                )
                .Select(pl =>
                    new ProxyPatientResponseDto
                    {
                        ProxyLinkId =
                            pl.Id,

                        PatientId =
                            pl.PatientId,

                        PatientName =
                            pl.Patient.User
                                .FullName,

                        PatientNumber =
                            pl.Patient
                                .PatientNumber,

                        ClinicId =
                            pl.Patient.ClinicId,

                        ClinicName =
                            pl.Patient.Clinic ==
                                    null
                                ? null
                                : pl.Patient
                                    .Clinic.Name,

                        AssignedAt =
                            pl.AssignedAt
                    }
                )
                .ToListAsync();
        }

        private async Task<StaffActor>GetStaffActorAsync(Guid userId)
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
                user.Role ==
                    RoleNames.Nurse &&
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
                user.Role ==
                    RoleNames.ClinicAdmin &&
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
                user.Role ==
                    RoleNames.SuperAdmin &&
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
                            p.UserId ==
                                patientUserId &&
                            p.User.Role ==
                                RoleNames.Patient &&
                            p.User.IsActive
                    );

            if (patient == null)
            {
                throw new UnauthorizedAccessException(
                    "Active patient profile not found."
                );
            }

            return await _context.ProxyLinks
                .Include(pl => pl.Proxy)
                    .ThenInclude(p => p.User)
                .Where(
                    pl =>
                        pl.PatientId ==
                            patient.Id &&
                        pl.IsActive &&
                        pl.Proxy.User.IsActive &&
                        pl.Proxy.User.Role ==
                            RoleNames.Proxy
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
                                pl.Proxy.User
                                    .FullName,

                            PhoneNumber =
                                pl.Proxy.User
                                    .PhoneNumber,

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

        private class StaffActor
        {
            public Guid? ClinicId { get; set; }

            public Guid? NurseId { get; set; }

            public int? AdminId { get; set; }
        }
    }
}