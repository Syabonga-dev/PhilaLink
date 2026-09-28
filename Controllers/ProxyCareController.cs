using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalProject.Data;
using PersonalProject.Models.Constants;
using PersonalProject.Models.DTOs;
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
            var (
                proxy,
                error
            ) = await ResolveActiveProxyAsync();

            if (error != null)
            {
                return error;
            }

            var activeProxy =
                proxy!;

            /*
             * Only active, valid Patient links from the
             * Proxy's own clinic are exposed.
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
                            link.ProxyId ==
                                activeProxy.Id &&
                            link.IsActive &&
                            link.Patient.ClinicId ==
                                activeProxy.ClinicId &&
                            link.Patient.User.IsActive &&
                            link.Patient.User.Role ==
                                RoleNames.Patient
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
                                    activeProxy.ClinicId &&
                                collection.Status !=
                                    MedicationCollectionStatuses
                                        .Collected &&
                                collection.Status !=
                                    MedicationCollectionStatuses
                                        .Cancelled
                        )
                        .OrderBy(
                            collection =>
                                collection
                                    .ScheduledCollectionDate
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
                                        collection
                                            .ScheduledCollectionDate
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

                            return new ProxyCarePatientDto
                            {
                                ProxyLinkId =
                                    link.Id,

                                PatientId =
                                    link.PatientId,

                                PatientName =
                                    link.Patient.User.FullName,

                                PatientNumber =
                                    link.Patient.PatientNumber,

                                ClinicId =
                                    link.Patient.ClinicId,

                                ClinicName =
                                    link.Patient.Clinic?.Name,

                                AssignedAt =
                                    link.AssignedAt,

                                NextCollectionId =
                                    nextCollection?.Id,

                                NextCollectionDate =
                                    nextCollection?
                                        .ScheduledCollectionDate,

                                CollectionStatus =
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

            var response =
                new ProxyCareResponseDto
                {
                    ClinicId =
                        activeProxy.ClinicId,

                    ClinicName =
                        activeProxy.Clinic?.Name ??
                        string.Empty,

                    TotalPatients =
                        patients.Count,

                    DueSoon =
                        dueSoon,

                    Overdue =
                        overdue,

                    Patients =
                        patients
                };

            return Ok(response);
        }

        // =====================================================
        // PROXY COLLECTIONS
        // =====================================================

        [HttpGet("collections")]
        public async Task<IActionResult>
            GetCollections()
        {
            var (
                proxy,
                error
            ) = await ResolveActiveProxyAsync();

            if (error != null)
            {
                return error;
            }

            var activeProxy =
                proxy!;

            var patientIds =
                await GetAccessiblePatientIdsAsync(
                    activeProxy
                );

            if (patientIds.Count == 0)
            {
                return Ok(
                    Array.Empty<
                        ProxyCollectionResponseDto
                    >()
                );
            }

            var collections =
                await BuildCollectionQuery(
                        activeProxy,
                        patientIds
                    )
                    .OrderByDescending(
                        collection =>
                            collection
                                .ScheduledCollectionDate
                    )
                    .ToListAsync();

            var response =
                collections
                    .Select(
                        ToProxyCollectionDto
                    )
                    .ToList();

            return Ok(response);
        }

        // =====================================================
        // SINGLE COLLECTION DETAILS
        // =====================================================

        [HttpGet(
            "collections/{collectionId:guid}"
        )]
        public async Task<IActionResult>
            GetCollection(
                Guid collectionId
            )
        {
            var (
                proxy,
                error
            ) = await ResolveActiveProxyAsync();

            if (error != null)
            {
                return error;
            }

            var activeProxy =
                proxy!;

            var patientIds =
                await GetAccessiblePatientIdsAsync(
                    activeProxy
                );

            if (patientIds.Count == 0)
            {
                return NotFound(
                    new
                    {
                        message =
                            "Collection not found."
                    }
                );
            }

            var collection =
                await BuildCollectionQuery(
                        activeProxy,
                        patientIds
                    )
                    .FirstOrDefaultAsync(
                        item =>
                            item.Id ==
                            collectionId
                    );

            if (collection == null)
            {
                /*
                 * Deliberately return 404 rather than revealing
                 * whether a collection exists outside this
                 * Proxy's authorization boundary.
                 */
                return NotFound(
                    new
                    {
                        message =
                            "Collection not found."
                    }
                );
            }

            return Ok(
                ToProxyCollectionDto(
                    collection
                )
            );
        }

        // =====================================================
        // ACCESSIBLE PATIENT IDS
        // =====================================================

        private async Task<List<Guid>>
            GetAccessiblePatientIdsAsync(
                Proxy proxy
            )
        {
            return await _context.ProxyLinks
                .AsNoTracking()
                .Where(
                    link =>
                        link.ProxyId ==
                            proxy.Id &&
                        link.IsActive &&
                        link.Patient.ClinicId ==
                            proxy.ClinicId &&
                        link.Patient.User.IsActive &&
                        link.Patient.User.Role ==
                            RoleNames.Patient
                )
                .Select(
                    link =>
                        link.PatientId
                )
                .Distinct()
                .ToListAsync();
        }

        // =====================================================
        // COLLECTION QUERY
        // =====================================================

        private IQueryable<MedicationCollection>
            BuildCollectionQuery(
                Proxy proxy,
                List<Guid> patientIds
            )
        {
            return _context
                .MedicationCollections
                .AsNoTracking()
                .AsSplitQuery()
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
                );
        }

        // =====================================================
        // COLLECTION DTO
        // =====================================================

        private static ProxyCollectionResponseDto
            ToProxyCollectionDto(
                MedicationCollection collection
            )
        {
            var items =
                collection.Items
                    .OrderBy(
                        item =>
                            item.Medication.Name
                    )
                    .Select(
                        item =>
                            new ProxyCollectionItemDto
                            {
                                Id =
                                    item.Id,

                                MedicationId =
                                    item.MedicationId,

                                MedicationName =
                                    item.Medication.Name,

                                Dosage =
                                    item.Medication.Dosage,

                                Form =
                                    item.Medication.Form,

                                Quantity =
                                    item.Quantity
                            }
                    )
                    .ToList();

            return new ProxyCollectionResponseDto
            {
                Id =
                    collection.Id,

                PatientId =
                    collection.PatientId,

                PatientName =
                    collection.Patient.User.FullName,

                PatientNumber =
                    collection.Patient.PatientNumber,

                ClinicId =
                    collection.ClinicId,

                ClinicName =
                    collection.Clinic.Name,

                ProxyId =
                    collection.ProxyId,

                ProxyName =
                    collection.Proxy?.User.FullName,

                ProcessedByNurseId =
                    collection.ProcessedByNurseId,

                ProcessedByNurseName =
                    collection
                        .ProcessedByNurse?
                        .User.FullName,

                ScheduledCollectionDate =
                    collection
                        .ScheduledCollectionDate,

                CollectedAt =
                    collection.CollectedAt,

                Status =
                    GetDisplayStatus(
                        collection
                    ),

                MedicationName =
                    string.Join(
                        ", ",
                        items
                            .Select(
                                item =>
                                    item.MedicationName
                            )
                            .Where(
                                name =>
                                    !string.IsNullOrWhiteSpace(
                                        name
                                    )
                            )
                            .Distinct()
                    ),

                Notes =
                    collection.Notes,

                Items =
                    items
            };
        }

        // =====================================================
        // CURRENT PROXY
        // =====================================================

        private async Task<(
            Proxy? Proxy,
            IActionResult? Error
        )>
            ResolveActiveProxyAsync()
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
                return (
                    null,
                    Unauthorized(
                        new
                        {
                            message =
                                "Authenticated user identifier is missing or invalid."
                        }
                    )
                );
            }

            var proxy =
                await _context.Proxies
                    .AsNoTracking()
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
                return (
                    null,
                    StatusCode(
                        StatusCodes
                            .Status403Forbidden,
                        new
                        {
                            message =
                                "Active proxy profile not found."
                        }
                    )
                );
            }

            if (
                proxy.ClinicId ==
                Guid.Empty
            )
            {
                return (
                    null,
                    Conflict(
                        new
                        {
                            message =
                                "Proxy account is not assigned to a clinic."
                        }
                    )
                );
            }

            if (proxy.Clinic == null)
            {
                return (
                    null,
                    Conflict(
                        new
                        {
                            message =
                                "Proxy clinic could not be found."
                        }
                    )
                );
            }

            return (
                proxy,
                null
            );
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
                MedicationCollectionStatuses
                    .Collected
            )
            {
                return MedicationCollectionStatuses
                    .Collected;
            }

            if (
                collection.Status ==
                MedicationCollectionStatuses
                    .Cancelled
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
    }
}