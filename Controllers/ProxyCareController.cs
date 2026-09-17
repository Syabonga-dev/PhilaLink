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

        public ProxyCareController(PhilaLinkDbContext context)
        {
            _context = context;
        }

        [HttpGet("care")]
        public async Task<IActionResult> GetCare()
        {
            var proxy = await GetActiveProxyAsync();

            var links = await _context.ProxyLinks
                .AsNoTracking()
                .Include(link => link.Patient)
                    .ThenInclude(patient => patient.User)
                .Include(link => link.Patient)
                    .ThenInclude(patient => patient.Clinic)
                .Where(link =>
                    link.ProxyId == proxy.Id &&
                    link.IsActive)
                .OrderBy(link => link.Patient.User.FullName)
                .ToListAsync();

            var patientIds = links
                .Select(link => link.PatientId)
                .Distinct()
                .ToList();

            var activeCollections = patientIds.Count == 0
                ? new List<MedicationCollection>()
                : await _context.MedicationCollections
                    .AsNoTracking()
                    .Where(collection =>
                        patientIds.Contains(collection.PatientId) &&
                        collection.Status != MedicationCollectionStatuses.Collected &&
                        collection.Status != MedicationCollectionStatuses.Cancelled)
                    .OrderBy(collection => collection.ScheduledCollectionDate)
                    .ToListAsync();

            var nextCollectionByPatient = activeCollections
                .GroupBy(collection => collection.PatientId)
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .OrderBy(collection => collection.ScheduledCollectionDate)
                        .First());

            var today = DateTime.UtcNow.Date;
            var dueSoonCutoff = today.AddDays(2);

            var patients = links
                .Select(link =>
                {
                    nextCollectionByPatient.TryGetValue(
                        link.PatientId,
                        out var nextCollection);

                    return new
                    {
                        proxyLinkId = link.Id,
                        patientId = link.PatientId,
                        patientName = link.Patient.User.FullName,
                        patientNumber = link.Patient.PatientNumber,
                        clinicId = link.Patient.ClinicId,
                        clinicName = link.Patient.Clinic == null
                            ? null
                            : link.Patient.Clinic.Name,
                        assignedAt = link.AssignedAt,
                        nextCollectionId = nextCollection?.Id,
                        nextCollectionDate = nextCollection?.ScheduledCollectionDate,
                        collectionStatus = nextCollection == null
                            ? "None"
                            : GetDisplayStatus(nextCollection)
                    };
                })
                .ToList();

            var nextCollections = nextCollectionByPatient.Values.ToList();

            var dueSoon = nextCollections.Count(collection =>
                collection.ScheduledCollectionDate.Date >= today &&
                collection.ScheduledCollectionDate.Date <= dueSoonCutoff);

            var overdue = nextCollections.Count(collection =>
                collection.ScheduledCollectionDate.Date < today);

            return Ok(new
            {
                totalPatients = patients.Count,
                dueSoon,
                overdue,
                patients
            });
        }

        [HttpGet("collections")]
        public async Task<IActionResult> GetCollections()
        {
            var proxy = await GetActiveProxyAsync();

            var patientIds = await _context.ProxyLinks
                .AsNoTracking()
                .Where(link =>
                    link.ProxyId == proxy.Id &&
                    link.IsActive)
                .Select(link => link.PatientId)
                .Distinct()
                .ToListAsync();

            if (patientIds.Count == 0)
            {
                return Ok(Array.Empty<object>());
            }

            var collections = await _context.MedicationCollections
                .AsNoTracking()
                .Include(collection => collection.Patient)
                    .ThenInclude(patient => patient.User)
                .Include(collection => collection.Clinic)
                .Include(collection => collection.Proxy)
                    .ThenInclude(proxyEntity => proxyEntity!.User)
                .Include(collection => collection.ProcessedByNurse)
                    .ThenInclude(nurse => nurse!.User)
                .Include(collection => collection.Items)
                    .ThenInclude(item => item.Medication)
                .Where(collection =>
                    patientIds.Contains(collection.PatientId))
                .OrderByDescending(collection =>
                    collection.ScheduledCollectionDate)
                .ToListAsync();

            var response = collections.Select(collection => new
            {
                id = collection.Id,
                patientId = collection.PatientId,
                patientName = collection.Patient.User.FullName,
                patientNumber = collection.Patient.PatientNumber,
                clinicId = collection.ClinicId,
                clinicName = collection.Clinic.Name,
                proxyId = collection.ProxyId,
                proxyName = collection.Proxy == null
                    ? null
                    : collection.Proxy.User.FullName,
                processedByNurseId = collection.ProcessedByNurseId,
                processedByNurseName = collection.ProcessedByNurse == null
                    ? null
                    : collection.ProcessedByNurse.User.FullName,
                scheduledCollectionDate = collection.ScheduledCollectionDate,
                collectedAt = collection.CollectedAt,
                status = GetDisplayStatus(collection),
                medicationName = string.Join(
                    ", ",
                    collection.Items
                        .Select(item => item.Medication.Name)
                        .Where(name => !string.IsNullOrWhiteSpace(name))
                        .Distinct()),
                notes = collection.Notes
            });

            return Ok(response);
        }

        private async Task<Proxy> GetActiveProxyAsync()
        {
            var userId = GetCurrentUserId();

            var proxy = await _context.Proxies
                .Include(proxyEntity => proxyEntity.User)
                .FirstOrDefaultAsync(proxyEntity =>
                    proxyEntity.UserId == userId &&
                    proxyEntity.User.Role == RoleNames.Proxy &&
                    proxyEntity.User.IsActive);

            if (proxy == null)
            {
                throw new UnauthorizedAccessException(
                    "Active proxy profile not found.");
            }

            return proxy;
        }

        private static string GetDisplayStatus(
            MedicationCollection collection)
        {
            if (collection.Status == MedicationCollectionStatuses.Collected)
            {
                return MedicationCollectionStatuses.Collected;
            }

            if (collection.Status == MedicationCollectionStatuses.Cancelled)
            {
                return MedicationCollectionStatuses.Cancelled;
            }

            return collection.ScheduledCollectionDate.Date <
                   DateTime.UtcNow.Date
                ? MedicationCollectionStatuses.Overdue
                : MedicationCollectionStatuses.Pending;
        }

        private Guid GetCurrentUserId()
        {
            var value = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(value) ||
                !Guid.TryParse(value, out var userId))
            {
                throw new UnauthorizedAccessException();
            }

            return userId;
        }
    }
}
