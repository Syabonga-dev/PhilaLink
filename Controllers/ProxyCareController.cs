using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalProject.Data;
using PersonalProject.Models.Constants;
using PersonalProject.Models.Entities;
using System.Security.Claims;

namespace PersonalProject.Controllers
{
    [ApiController]
    [Route("api/proxies/me")]
    [Authorize(Policy = "ProxyOnly")]
    public class ProxyCareController : ControllerBase
    {
        private readonly PhilaLinkDbContext _context;

        public ProxyCareController(
            PhilaLinkDbContext context
        )
        {
            _context = context;
        }

        // =====================================================
        // PROXY CARE DASHBOARD
        // =====================================================

        [HttpGet("care")]
        public async Task<IActionResult> GetCare()
        {
            var proxy =
                await GetActiveProxyAsync();

            /*
             * CRITICAL CLINIC BOUNDARY
             *
             * A Proxy can only see active links for patients
             * belonging to the Proxy's registered clinic.
             *
             * This also protects against legacy/bad ProxyLinks
             * that may already exist in the database.
             */
            var links =
                await _context.ProxyLinks
                    .AsNoTracking()
                    .Include(link => link.Patient)
                        .ThenInclude(patient => patient.User)
                    .Include(link => link.Patient)
                        .ThenInclude(patient => patient.Clinic)
                    .Where(
                        link =>
                            link.ProxyId == proxy.Id &&
                            link.IsActive &&
                            link.Patient.ClinicId ==
                                proxy.ClinicId
                    )
                    .OrderBy(
                        link =>
                            link.Patient.User.FullName
                    )
                    .ToListAsync();

            var patientIds =
                links
                    .Select(
                        link =>
                            link.PatientId
                    )
                    .Distinct()
                    .ToList();

            /*
             * Collections are restricted twice:
             *
             * 1. Patient must be one of this Proxy's valid
             *    same-clinic linked patients.
             *
             * 2. Collection itself must belong to the Proxy's
             *    registered clinic.
             */
            var activeCollections =
                patientIds.Count == 0
                    ? new List<MedicationCollection>()
                    : await _context
                        .MedicationCollections
                        .AsNoTracking()
                        .Where(
                            collection =>
                                patientIds.Contains(
                                    collection.PatientId
                                ) &&
                                collection.ClinicId ==
                                    proxy.ClinicId &&
                                collection.Status !=
                                    MedicationCollectionStatuses.Collected &&
                                collection.Status !=
                                    MedicationCollectionStatuses.Cancelled
                        )
                        .OrderBy(
                            collection =>
                                collection.ScheduledCollectionDate
                        )
                        .ToListAsync();

            var nextCollectionByPatient =
                activeCollections
                    .GroupBy(
                        collection =>
                            collection.PatientId
                    )
                    .ToDictionary(
                        group =>
                            group.Key,

                        group =>
                            group
                                .OrderBy(
                                    collection =>
                                        collection.ScheduledCollectionDate
                                )
                                .First()
                    );

            var today =
                DateTime.UtcNow.Date;

            var dueSoonCutoff =
                today.AddDays(2);

            var patients =
                links
                    .Select(
                        link =>
                        {
                            nextCollectionByPatient
                                .TryGetValue(
                                    link.PatientId,
                                    out var nextCollection
                                );

                            return new
                            {
                                proxyLinkId =
                                    link.Id,

                                patientId =
                                    link.PatientId,

                                patientName =
                                    link.Patient.User.FullName,

                                patientNumber =
                                    link.Patient.PatientNumber,

                                clinicId =
                                    link.Patient.ClinicId,

                                clinicName =
                                    link.Patient.Clinic == null
                                        ? null
                                        : link.Patient.Clinic.Name,

                                assignedAt =
                                    link.AssignedAt,

                                nextCollectionId =
                                    nextCollection?.Id,

                                nextCollectionDate =
                                    nextCollection?
                                        .ScheduledCollectionDate,

                                collectionStatus =
                                    nextCollection == null
                                        ? "None"
                                        : GetDisplayStatus(
                                            nextCollection
                                        )
                            };
                        }
                    )
                    .ToList();

            var nextCollections =
                nextCollectionByPatient
                    .Values
                    .ToList();

            var dueSoon =
                nextCollections.Count(
                    collection =>
                        collection
                            .ScheduledCollectionDate
                            .Date >= today &&
                        collection
                            .ScheduledCollectionDate
                            .Date <= dueSoonCutoff
                );

            var overdue =
                nextCollections.Count(
                    collection =>
                        collection
                            .ScheduledCollectionDate
                            .Date < today
                );

            return Ok(
                new
                {
                    clinicId =
                        proxy.ClinicId,

                    clinicName =
                        proxy.Clinic?.Name,

                    totalPatients =
                        patients.Count,

                    dueSoon,

                    overdue,

                    patients
                }
            );
        }

        // =====================================================
        // PROXY COLLECTIONS
        // =====================================================

        [HttpGet("collections")]
        public async Task<IActionResult>
            GetCollections()
        {
            var proxy =
                await GetActiveProxyAsync();

            /*
             * Only obtain patients who:
             *
             * - are actively linked to this Proxy; AND
             * - belong to the Proxy's registered clinic.
             */
            var patientIds =
                await _context.ProxyLinks
                    .AsNoTracking()
                    .Where(
                        link =>
                            link.ProxyId == proxy.Id &&
                            link.IsActive &&
                            link.Patient.ClinicId ==
                                proxy.ClinicId
                    )
                    .Select(
                        link =>
                            link.PatientId
                    )
                    .Distinct()
                    .ToListAsync();

            if (patientIds.Count == 0)
            {
                return Ok(
                    Array.Empty<object>()
                );
            }

            /*
             * Collection access is also restricted by ClinicId.
             *
             * Even if malformed data contains a collection for
             * one of these PatientIds at another clinic, that
             * collection will not be exposed to this Proxy.
             */
            var collections =
                await _context
                    .MedicationCollections
                    .AsNoTracking()
                    .Include(
                        collection =>
                            collection.Patient
                    )
                        .ThenInclude(
                            patient =>
                                patient.User
                        )
                    .Include(
                        collection =>
                            collection.Clinic
                    )
                    .Include(
                        collection =>
                            collection.Proxy
                    )
                        .ThenInclude(
                            proxyEntity =>
                                proxyEntity!.User
                        )
                    .Include(
                        collection =>
                            collection.ProcessedByNurse
                    )
                        .ThenInclude(
                            nurse =>
                                nurse!.User
                        )
                    .Include(
                        collection =>
                            collection.Items
                    )
                        .ThenInclude(
                            item =>
                                item.Medication
                        )
                    .Where(
                        collection =>
                            patientIds.Contains(
                                collection.PatientId
                            ) &&
                            collection.ClinicId ==
                                proxy.ClinicId
                    )
                    .OrderByDescending(
                        collection =>
                            collection.ScheduledCollectionDate
                    )
                    .ToListAsync();

            var response =
                collections.Select(
                    collection =>
                        new
                        {
                            id =
                                collection.Id,

                            patientId =
                                collection.PatientId,

                            patientName =
                                collection.Patient.User.FullName,

                            patientNumber =
                                collection.Patient.PatientNumber,

                            clinicId =
                                collection.ClinicId,

                            clinicName =
                                collection.Clinic.Name,

                            proxyId =
                                collection.ProxyId,

                            proxyName =
                                collection.Proxy == null
                                    ? null
                                    : collection.Proxy
                                        .User.FullName,

                            processedByNurseId =
                                collection.ProcessedByNurseId,

                            processedByNurseName =
                                collection.ProcessedByNurse == null
                                    ? null
                                    : collection
                                        .ProcessedByNurse
                                        .User.FullName,

                            scheduledCollectionDate =
                                collection
                                    .ScheduledCollectionDate,

                            collectedAt =
                                collection.CollectedAt,

                            status =
                                GetDisplayStatus(
                                    collection
                                ),

                            medicationName =
                                string.Join(
                                    ", ",
                                    collection.Items
                                        .Select(
                                            item =>
                                                item.Medication.Name
                                        )
                                        .Where(
                                            name =>
                                                !string.IsNullOrWhiteSpace(
                                                    name
                                                )
                                        )
                                        .Distinct()
                                ),

                            notes =
                                collection.Notes
                        }
                );

            return Ok(response);
        }

        // =====================================================
        // CURRENT PROXY
        // =====================================================

        private async Task<Proxy>
            GetActiveProxyAsync()
        {
            var userId =
                GetCurrentUserId();

            /*
             * Clinic is deliberately loaded here because
             * ClinicId is now part of the Proxy's authorization
             * boundary.
             */
            var proxy =
                await _context.Proxies
                    .Include(
                        proxyEntity =>
                            proxyEntity.User
                    )
                    .Include(
                        proxyEntity =>
                            proxyEntity.Clinic
                    )
                    .FirstOrDefaultAsync(
                        proxyEntity =>
                            proxyEntity.UserId ==
                                userId &&
                            proxyEntity.User.Role ==
                                RoleNames.Proxy &&
                            proxyEntity.User.IsActive
                    );

            if (proxy == null)
            {
                throw new UnauthorizedAccessException(
                    "Active proxy profile not found."
                );
            }

            /*
             * Guid.Empty must never behave as a valid clinic.
             * This also catches incorrectly migrated legacy
             * Proxy accounts.
             */
            if (
                proxy.ClinicId ==
                Guid.Empty
            )
            {
                throw new InvalidOperationException(
                    "Proxy account is not assigned to a clinic."
                );
            }

            if (proxy.Clinic == null)
            {
                throw new InvalidOperationException(
                    "Proxy clinic could not be found."
                );
            }

            return proxy;
        }

        // =====================================================
        // COLLECTION DISPLAY STATUS
        // =====================================================

        private static string GetDisplayStatus(
            MedicationCollection collection
        )
        {
            if (
                collection.Status ==
                MedicationCollectionStatuses.Collected
            )
            {
                return MedicationCollectionStatuses
                    .Collected;
            }

            if (
                collection.Status ==
                MedicationCollectionStatuses.Cancelled
            )
            {
                return MedicationCollectionStatuses
                    .Cancelled;
            }

            return collection
                .ScheduledCollectionDate
                .Date <
                DateTime.UtcNow.Date
                    ? MedicationCollectionStatuses
                        .Overdue
                    : MedicationCollectionStatuses
                        .Pending;
        }

        // =====================================================
        // CURRENT USER ID
        // =====================================================

        private Guid GetCurrentUserId()
        {
            var value =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier
                );

            if (
                string.IsNullOrWhiteSpace(
                    value
                ) ||
                !Guid.TryParse(
                    value,
                    out var userId
                )
            )
            {
                throw new UnauthorizedAccessException();
            }

            return userId;
        }
    }
}