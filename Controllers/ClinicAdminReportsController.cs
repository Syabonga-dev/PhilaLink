using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalProject.Data;
using PersonalProject.Models.Constants;
using PersonalProject.Models.DTOs;
using PersonalProject.Models.Entities;
using PersonalProject.Utilities;
using System.Security.Claims;

namespace PersonalProject.Controllers
{
    [ApiController]
    [Route("api/clinic-admin/report-builder")]
    [Authorize(Roles = RoleNames.ClinicAdmin)]
    public class ClinicAdminReportsController : ControllerBase
    {
        private readonly PhilaLinkDbContext _context;

        public ClinicAdminReportsController(
            PhilaLinkDbContext context
        )
        {
            _context = context;
        }

        [HttpGet("preview")]
        public async Task<IActionResult> Preview(
            [FromQuery] ClinicAdminDynamicReportQueryDto query
        )
        {
            try
            {
                var scope = await GetScopeAsync(
                    GetCurrentUserId()
                );

                var report = await BuildPreviewAsync(
                    scope,
                    query
                );

                return Ok(report);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(
                    new { message = ex.Message }
                );
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpGet("export")]
        public async Task<IActionResult> Export(
            [FromQuery] ClinicAdminDynamicReportQueryDto query,
            [FromQuery] string format = "xlsx"
        )
        {
            try
            {
                var scope = await GetScopeAsync(
                    GetCurrentUserId()
                );

                var report = await BuildPreviewAsync(
                    scope,
                    query
                );

                var normalizedFormat =
                    (format ?? string.Empty)
                        .Trim()
                        .ToLowerInvariant();

                var safeClinic = SanitizeFileName(
                    scope.ClinicName
                );

                var safeType = SanitizeFileName(
                    report.ReportType
                );

                var stamp =
                    DateTime.UtcNow
                        .ToString("yyyyMMdd-HHmmss");

                if (
                    normalizedFormat is
                    "xlsx" or "excel"
                )
                {
                    var bytes =
                        ClinicAdminDynamicReportBuilder
                            .BuildExcel(report);

                    return File(
                        bytes,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        $"{safeClinic}-{safeType}-{stamp}.xlsx"
                    );
                }

                if (
                    normalizedFormat == "pdf"
                )
                {
                    var bytes =
                        ClinicAdminDynamicReportBuilder
                            .BuildPdf(report);

                    return File(
                        bytes,
                        "application/pdf",
                        $"{safeClinic}-{safeType}-{stamp}.pdf"
                    );
                }

                return BadRequest(
                    new
                    {
                        message =
                            "Format must be xlsx or pdf."
                    }
                );
            }
            catch (ArgumentException ex)
            {
                return BadRequest(
                    new { message = ex.Message }
                );
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        private async Task<ClinicAdminDynamicReportPreviewDto>
            BuildPreviewAsync(
                ClinicAdminReportScope scope,
                ClinicAdminDynamicReportQueryDto query
            )
        {
            var reportType = NormalizeReportType(
                query.ReportType
            );

            return reportType switch
            {
                "Patients" =>
                    await BuildPatientsAsync(
                        scope,
                        query
                    ),

                "Appointments" =>
                    await BuildAppointmentsAsync(
                        scope,
                        query
                    ),

                "Collections" =>
                    await BuildCollectionsAsync(
                        scope,
                        query
                    ),

                "Medication Adherence" =>
                    await BuildMedicationAdherenceAsync(
                        scope,
                        query
                    ),

                "Inventory" =>
                    await BuildInventoryAsync(
                        scope,
                        query
                    ),

                "Staff" =>
                    await BuildStaffAsync(
                        scope,
                        query
                    ),

                _ =>
                    throw new ArgumentException(
                        "Unsupported report type."
                    )
            };
        }

        private async Task<ClinicAdminDynamicReportPreviewDto>
            BuildPatientsAsync(
                ClinicAdminReportScope scope,
                ClinicAdminDynamicReportQueryDto query
            )
        {
            var (from, to, endExclusive) =
                ResolveDateRange(query);

            var source =
                await _context.Patients
                    .AsNoTracking()
                    .Where(
                        patient =>
                            patient.ClinicId ==
                                scope.ClinicId &&
                            patient.CreatedAt >=
                                from &&
                            patient.CreatedAt <
                                endExclusive
                    )
                    .Select(
                        patient =>
                            new
                            {
                                patient.PatientNumber,
                                patient.DateOfBirth,
                                patient.Gender,
                                patient.Email,
                                patient.Suburb,
                                patient.City,
                                patient.Province,
                                patient.CreatedAt,
                                patient.User.FullName,
                                patient.User.PhoneNumber,
                                patient.User.IsActive
                            }
                    )
                    .OrderByDescending(
                        item =>
                            item.CreatedAt
                    )
                    .ToListAsync();

            var filtered =
                source
                    .Where(
                        item =>
                            MatchesStatus(
                                query.Status,
                                item.IsActive
                                    ? "Active"
                                    : "Inactive"
                            )
                    )
                    .Where(
                        item =>
                            MatchesSearch(
                                query.Search,
                                item.PatientNumber,
                                item.FullName,
                                item.Email,
                                item.PhoneNumber,
                                item.Suburb,
                                item.City,
                                item.Province
                            )
                    )
                    .ToList();

            var preview =
                BasePreview(
                    "Patients",
                    "Patient Registration Report",
                    scope,
                    from,
                    to
                );

            preview.Columns =
                new()
                {
                    Col(
                        "patientNumber",
                        "Patient No."
                    ),
                    Col(
                        "fullName",
                        "Patient"
                    ),
                    Col(
                        "gender",
                        "Gender"
                    ),
                    Col(
                        "dateOfBirth",
                        "Date of Birth",
                        "date"
                    ),
                    Col(
                        "phone",
                        "Phone"
                    ),
                    Col(
                        "email",
                        "Email"
                    ),
                    Col(
                        "location",
                        "Location"
                    ),
                    Col(
                        "status",
                        "Status",
                        "status"
                    ),
                    Col(
                        "registered",
                        "Registered",
                        "date"
                    )
                };

            preview.Rows =
                filtered
                    .Select(
                        item =>
                            Row(
                                ("patientNumber", item.PatientNumber),
                                ("fullName", item.FullName),
                                ("gender", item.Gender),
                                ("dateOfBirth", item.DateOfBirth.ToString("yyyy-MM-dd")),
                                ("phone", item.PhoneNumber),
                                ("email", item.Email),
                                ("location",
                                    string.Join(
                                        ", ",
                                        new[]
                                        {
                                            item.Suburb,
                                            item.City,
                                            item.Province
                                        }
                                        .Where(
                                            value =>
                                                !string.IsNullOrWhiteSpace(value)
                                        )
                                    )
                                ),
                                ("status",
                                    item.IsActive
                                        ? "Active"
                                        : "Inactive"
                                ),
                                ("registered", item.CreatedAt)
                            )
                    )
                    .ToList();

            preview.Summary =
                new()
                {
                    Sum(
                        "shown",
                        "Patients shown",
                        filtered.Count
                    ),
                    Sum(
                        "active",
                        "Active",
                        filtered.Count(
                            item =>
                                item.IsActive
                        )
                    ),
                    Sum(
                        "inactive",
                        "Inactive",
                        filtered.Count(
                            item =>
                                !item.IsActive
                        )
                    )
                };

            preview.FilterOptions["status"] =
                new()
                {
                    "All",
                    "Active",
                    "Inactive"
                };

            return preview;
        }

        private async Task<ClinicAdminDynamicReportPreviewDto>
            BuildAppointmentsAsync(
                ClinicAdminReportScope scope,
                ClinicAdminDynamicReportQueryDto query
            )
        {
            var (from, to, endExclusive) =
                ResolveDateRange(query);

            var source =
                await _context.Appointments
                    .AsNoTracking()
                    .Include(
                        appointment =>
                            appointment.Patient
                    )
                    .ThenInclude(
                        patient =>
                            patient.User
                    )
                    .Include(
                        appointment =>
                            appointment.Nurse
                    )
                    .ThenInclude(
                        nurse =>
                            nurse!.User
                    )
                    .Where(
                        appointment =>
                            appointment.ClinicId ==
                                scope.ClinicId &&
                            appointment.ScheduledAt >=
                                from &&
                            appointment.ScheduledAt <
                                endExclusive
                    )
                    .OrderByDescending(
                        appointment =>
                            appointment.ScheduledAt
                    )
                    .ToListAsync();

            var statusOptions =
                source
                    .Select(
                        item =>
                            item.Status
                    )
                    .Where(
                        value =>
                            !string.IsNullOrWhiteSpace(value)
                    )
                    .Distinct(
                        StringComparer
                            .OrdinalIgnoreCase
                    )
                    .OrderBy(
                        value =>
                            value
                    )
                    .ToList();

            var typeOptions =
                source
                    .Select(
                        item =>
                            item.Type
                    )
                    .Where(
                        value =>
                            !string.IsNullOrWhiteSpace(value)
                    )
                    .Distinct(
                        StringComparer
                            .OrdinalIgnoreCase
                    )
                    .OrderBy(
                        value =>
                            value
                    )
                    .ToList();

            var providerOptions =
                source
                    .Select(
                        GetAppointmentProvider
                    )
                    .Where(
                        value =>
                            !string.IsNullOrWhiteSpace(value)
                    )
                    .Distinct(
                        StringComparer
                            .OrdinalIgnoreCase
                    )
                    .OrderBy(
                        value =>
                            value
                    )
                    .ToList();

            var modeOptions =
                source
                    .Select(
                        item =>
                            item.Mode
                    )
                    .Where(
                        value =>
                            !string.IsNullOrWhiteSpace(value)
                    )
                    .Distinct(
                        StringComparer
                            .OrdinalIgnoreCase
                    )
                    .OrderBy(
                        value =>
                            value
                    )
                    .ToList();

            var filtered =
                source
                    .Where(
                        item =>
                            MatchesStatus(
                                query.Status,
                                item.Status
                            )
                    )
                    .Where(
                        item =>
                            MatchesOption(
                                query.AppointmentType,
                                item.Type
                            )
                    )
                    .Where(
                        item =>
                            MatchesOption(
                                query.Provider,
                                GetAppointmentProvider(item)
                            )
                    )
                    .Where(
                        item =>
                            MatchesOption(
                                query.Mode,
                                item.Mode
                            )
                    )
                    .Where(
                        item =>
                            MatchesSearch(
                                query.Search,
                                item.Patient.PatientNumber,
                                item.Patient.User.FullName,
                                item.Type,
                                item.Reason,
                                item.Status,
                                GetAppointmentProvider(item)
                            )
                    )
                    .ToList();

            var preview =
                BasePreview(
                    "Appointments",
                    "Appointment Report",
                    scope,
                    from,
                    to
                );

            preview.Columns =
                new()
                {
                    Col(
                        "patientNumber",
                        "Patient No."
                    ),
                    Col(
                        "patient",
                        "Patient"
                    ),
                    Col(
                        "type",
                        "Appointment"
                    ),
                    Col(
                        "provider",
                        "Provider"
                    ),
                    Col(
                        "mode",
                        "Mode"
                    ),
                    Col(
                        "status",
                        "Status",
                        "status"
                    ),
                    Col(
                        "scheduled",
                        "Scheduled",
                        "datetime"
                    ),
                    Col(
                        "duration",
                        "Duration",
                        "number"
                    ),
                    Col(
                        "reason",
                        "Reason"
                    )
                };

            preview.Rows =
                filtered
                    .Select(
                        item =>
                            Row(
                                ("patientNumber", item.Patient.PatientNumber),
                                ("patient", item.Patient.User.FullName),
                                ("type", item.Type),
                                ("provider", GetAppointmentProvider(item)),
                                ("mode", item.Mode),
                                ("status", item.Status),
                                ("scheduled", item.ScheduledAt),
                                ("duration", item.DurationMinutes),
                                ("reason", item.Reason)
                            )
                    )
                    .ToList();

            preview.Summary =
                new()
                {
                    Sum(
                        "shown",
                        "Appointments shown",
                        filtered.Count
                    ),
                    Sum(
                        "completed",
                        "Completed",
                        filtered.Count(
                            item =>
                                item.Status.Equals(
                                    "Completed",
                                    StringComparison.OrdinalIgnoreCase
                                )
                        )
                    ),
                    Sum(
                        "pending",
                        "Pending",
                        filtered.Count(
                            item =>
                                item.Status.Equals(
                                    "Pending",
                                    StringComparison.OrdinalIgnoreCase
                                )
                        )
                    ),
                    Sum(
                        "cancelled",
                        "Cancelled",
                        filtered.Count(
                            item =>
                                item.Status.Equals(
                                    "Cancelled",
                                    StringComparison.OrdinalIgnoreCase
                                )
                        )
                    )
                };

            preview.FilterOptions["status"] =
                WithAll(statusOptions);

            preview.FilterOptions["appointmentType"] =
                WithAll(typeOptions);

            preview.FilterOptions["provider"] =
                WithAll(providerOptions);

            preview.FilterOptions["mode"] =
                WithAll(modeOptions);

            return preview;
        }

        private async Task<ClinicAdminDynamicReportPreviewDto>
            BuildCollectionsAsync(
                ClinicAdminReportScope scope,
                ClinicAdminDynamicReportQueryDto query
            )
        {
            var (from, to, endExclusive) =
                ResolveDateRange(query);

            var source =
                await _context.MedicationCollections
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
                            collection.Proxy
                    )
                    .ThenInclude(
                        proxy =>
                            proxy!.User
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
                            collection.ClinicId ==
                                scope.ClinicId &&
                            collection.ScheduledCollectionDate >=
                                from &&
                            collection.ScheduledCollectionDate <
                                endExclusive
                    )
                    .OrderByDescending(
                        collection =>
                            collection.ScheduledCollectionDate
                    )
                    .ToListAsync();

            var statusOptions =
                source
                    .Select(
                        item =>
                            item.Status
                    )
                    .Where(
                        value =>
                            !string.IsNullOrWhiteSpace(value)
                    )
                    .Distinct(
                        StringComparer
                            .OrdinalIgnoreCase
                    )
                    .OrderBy(
                        value =>
                            value
                    )
                    .ToList();

            var medicationOptions =
                source
                    .SelectMany(
                        item =>
                            item.Items
                    )
                    .Select(
                        item =>
                            item.Medication.Name
                    )
                    .Where(
                        value =>
                            !string.IsNullOrWhiteSpace(value)
                    )
                    .Distinct(
                        StringComparer
                            .OrdinalIgnoreCase
                    )
                    .OrderBy(
                        value =>
                            value
                    )
                    .ToList();

            var filtered =
                source
                    .Where(
                        item =>
                            MatchesStatus(
                                query.Status,
                                item.Status
                            )
                    )
                    .Where(
                        item =>
                            string.IsNullOrWhiteSpace(
                                query.Medication
                            ) ||
                            IsAll(query.Medication) ||
                            item.Items.Any(
                                line =>
                                    line.Medication.Name.Equals(
                                        query.Medication,
                                        StringComparison.OrdinalIgnoreCase
                                    )
                            )
                    )
                    .Where(
                        item =>
                            MatchesSearch(
                                query.Search,
                                item.Patient.PatientNumber,
                                item.Patient.User.FullName,
                                item.Status,
                                item.Proxy?.User.FullName,
                                item.ProcessedByNurse?.User.FullName,
                                string.Join(
                                    ", ",
                                    item.Items.Select(
                                        line =>
                                            line.Medication.Name
                                    )
                                )
                            )
                    )
                    .ToList();

            var preview =
                BasePreview(
                    "Collections",
                    "Medication Collection Report",
                    scope,
                    from,
                    to
                );

            preview.Columns =
                new()
                {
                    Col(
                        "patientNumber",
                        "Patient No."
                    ),
                    Col(
                        "patient",
                        "Patient"
                    ),
                    Col(
                        "medication",
                        "Medication"
                    ),
                    Col(
                        "quantity",
                        "Quantity",
                        "number"
                    ),
                    Col(
                        "collector",
                        "Collection By"
                    ),
                    Col(
                        "scheduled",
                        "Scheduled",
                        "date"
                    ),
                    Col(
                        "collected",
                        "Collected",
                        "datetime"
                    ),
                    Col(
                        "status",
                        "Status",
                        "status"
                    ),
                    Col(
                        "processedBy",
                        "Processed By"
                    )
                };

            preview.Rows =
                filtered
                    .Select(
                        item =>
                            Row(
                                ("patientNumber", item.Patient.PatientNumber),
                                ("patient", item.Patient.User.FullName),
                                ("medication",
                                    string.Join(
                                        ", ",
                                        item.Items
                                            .Select(
                                                line =>
                                                    line.Medication.Name
                                            )
                                            .Distinct()
                                    )
                                ),
                                ("quantity",
                                    item.Items.Sum(
                                        line =>
                                            line.Quantity
                                    )
                                ),
                                ("collector",
                                    item.Proxy != null
                                        ? $"Proxy: {item.Proxy.User.FullName}"
                                        : "Patient"
                                ),
                                ("scheduled", item.ScheduledCollectionDate),
                                ("collected", item.CollectedAt),
                                ("status", item.Status),
                                ("processedBy",
                                    item.ProcessedByNurse?.User.FullName ??
                                    "—"
                                )
                            )
                    )
                    .ToList();

            preview.Summary =
                new()
                {
                    Sum(
                        "shown",
                        "Collections shown",
                        filtered.Count
                    ),
                    Sum(
                        "collected",
                        "Collected",
                        filtered.Count(
                            item =>
                                item.Status.Equals(
                                    "Collected",
                                    StringComparison.OrdinalIgnoreCase
                                )
                        )
                    ),
                    Sum(
                        "missed",
                        "Missed",
                        filtered.Count(
                            item =>
                                item.Status.Equals(
                                    "Missed",
                                    StringComparison.OrdinalIgnoreCase
                                )
                        )
                    ),
                    Sum(
                        "scheduled",
                        "Scheduled",
                        filtered.Count(
                            item =>
                                item.Status.Equals(
                                    "Scheduled",
                                    StringComparison.OrdinalIgnoreCase
                                )
                        )
                    )
                };

            preview.FilterOptions["status"] =
                WithAll(statusOptions);

            preview.FilterOptions["medication"] =
                WithAll(medicationOptions);

            return preview;
        }

        private async Task<ClinicAdminDynamicReportPreviewDto>
            BuildMedicationAdherenceAsync(
                ClinicAdminReportScope scope,
                ClinicAdminDynamicReportQueryDto query
            )
        {
            var (from, to, endExclusive) =
                ResolveDateRange(query);

            var source =
                await _context.MedicationLogs
                    .AsNoTracking()
                    .Include(
                        log =>
                            log.Medication
                    )
                    .ThenInclude(
                        medication =>
                            medication.Patient
                    )
                    .ThenInclude(
                        patient =>
                            patient.User
                    )
                    .Where(
                        log =>
                            log.Medication.Patient.ClinicId ==
                                scope.ClinicId &&
                            log.TakenAt >=
                                from &&
                            log.TakenAt <
                                endExclusive
                    )
                    .OrderByDescending(
                        log =>
                            log.TakenAt
                    )
                    .ToListAsync();

            var medicationOptions =
                source
                    .Select(
                        item =>
                            item.Medication.Name
                    )
                    .Where(
                        value =>
                            !string.IsNullOrWhiteSpace(value)
                    )
                    .Distinct(
                        StringComparer
                            .OrdinalIgnoreCase
                    )
                    .OrderBy(
                        value =>
                            value
                    )
                    .ToList();

            var filtered =
                source
                    .Where(
                        item =>
                            MatchesStatus(
                                query.Status,
                                item.Taken
                                    ? "Taken"
                                    : "Missed"
                            )
                    )
                    .Where(
                        item =>
                            string.IsNullOrWhiteSpace(
                                query.Medication
                            ) ||
                            IsAll(query.Medication) ||
                            item.Medication.Name.Equals(
                                query.Medication,
                                StringComparison.OrdinalIgnoreCase
                            )
                    )
                    .Where(
                        item =>
                            MatchesSearch(
                                query.Search,
                                item.Medication.Patient.PatientNumber,
                                item.Medication.Patient.User.FullName,
                                item.Medication.Name,
                                item.Medication.Dosage,
                                item.Medication.Form,
                                item.Notes
                            )
                    )
                    .ToList();

            var taken =
                filtered.Count(
                    item =>
                        item.Taken
                );

            var missed =
                filtered.Count -
                taken;

            var adherenceRate =
                filtered.Count > 0
                    ? Math.Round(
                        taken * 100d /
                        filtered.Count,
                        1
                    )
                    : 0;

            var preview =
                BasePreview(
                    "Medication Adherence",
                    "Medication Adherence Report",
                    scope,
                    from,
                    to
                );

            preview.Columns =
                new()
                {
                    Col(
                        "patientNumber",
                        "Patient No."
                    ),
                    Col(
                        "patient",
                        "Patient"
                    ),
                    Col(
                        "medication",
                        "Medication"
                    ),
                    Col(
                        "dosage",
                        "Dosage"
                    ),
                    Col(
                        "form",
                        "Form"
                    ),
                    Col(
                        "result",
                        "Result",
                        "status"
                    ),
                    Col(
                        "recorded",
                        "Recorded",
                        "datetime"
                    ),
                    Col(
                        "notes",
                        "Notes"
                    )
                };

            preview.Rows =
                filtered
                    .Select(
                        item =>
                            Row(
                                ("patientNumber", item.Medication.Patient.PatientNumber),
                                ("patient", item.Medication.Patient.User.FullName),
                                ("medication", item.Medication.Name),
                                ("dosage", item.Medication.Dosage),
                                ("form", item.Medication.Form),
                                ("result",
                                    item.Taken
                                        ? "Taken"
                                        : "Missed"
                                ),
                                ("recorded", item.TakenAt),
                                ("notes", item.Notes ?? "")
                            )
                    )
                    .ToList();

            preview.Summary =
                new()
                {
                    Sum(
                        "shown",
                        "Logs shown",
                        filtered.Count
                    ),
                    Sum(
                        "taken",
                        "Taken",
                        taken
                    ),
                    Sum(
                        "missed",
                        "Missed",
                        missed
                    ),
                    Sum(
                        "rate",
                        "Adherence",
                        $"{adherenceRate:0.#}%"
                    )
                };

            preview.FilterOptions["status"] =
                new()
                {
                    "All",
                    "Taken",
                    "Missed"
                };

            preview.FilterOptions["medication"] =
                WithAll(medicationOptions);

            return preview;
        }

        private async Task<ClinicAdminDynamicReportPreviewDto>
            BuildInventoryAsync(
                ClinicAdminReportScope scope,
                ClinicAdminDynamicReportQueryDto query
            )
        {
            var source =
                await _context.ClinicStocks
                    .AsNoTracking()
                    .Where(
                        item =>
                            item.ClinicId ==
                            scope.ClinicId
                    )
                    .OrderBy(
                        item =>
                            item.MedicationName
                    )
                    .ThenBy(
                        item =>
                            item.Strength
                    )
                    .ToListAsync();

            string GetStatus(
                ClinicStock item
            )
            {
                if (!item.IsActive)
                {
                    return "Inactive";
                }

                return item.QuantityOnHand <=
                    item.ReorderLevel
                        ? "Low stock"
                        : "Healthy";
            }

            var filtered =
                source
                    .Where(
                        item =>
                            MatchesStatus(
                                query.Status,
                                GetStatus(item)
                            )
                    )
                    .Where(
                        item =>
                            string.IsNullOrWhiteSpace(
                                query.Medication
                            ) ||
                            IsAll(query.Medication) ||
                            item.MedicationName.Equals(
                                query.Medication,
                                StringComparison.OrdinalIgnoreCase
                            )
                    )
                    .Where(
                        item =>
                            MatchesSearch(
                                query.Search,
                                item.MedicationName,
                                item.Strength,
                                item.Form,
                                item.Unit
                            )
                    )
                    .ToList();

            var preview =
                BasePreview(
                    "Inventory",
                    "Medication Inventory Report",
                    scope,
                    null,
                    null
                );

            preview.Columns =
                new()
                {
                    Col(
                        "medication",
                        "Medication"
                    ),
                    Col(
                        "strength",
                        "Strength"
                    ),
                    Col(
                        "form",
                        "Form"
                    ),
                    Col(
                        "quantity",
                        "On Hand",
                        "number"
                    ),
                    Col(
                        "unit",
                        "Unit"
                    ),
                    Col(
                        "reorderLevel",
                        "Reorder Level",
                        "number"
                    ),
                    Col(
                        "status",
                        "Stock Status",
                        "status"
                    ),
                    Col(
                        "updated",
                        "Last Updated",
                        "datetime"
                    )
                };

            preview.Rows =
                filtered
                    .Select(
                        item =>
                            Row(
                                ("medication", item.MedicationName),
                                ("strength", item.Strength),
                                ("form", item.Form),
                                ("quantity", item.QuantityOnHand),
                                ("unit", item.Unit),
                                ("reorderLevel", item.ReorderLevel),
                                ("status", GetStatus(item)),
                                ("updated", item.UpdatedAt ?? item.CreatedAt)
                            )
                    )
                    .ToList();

            preview.Summary =
                new()
                {
                    Sum(
                        "shown",
                        "Items shown",
                        filtered.Count
                    ),
                    Sum(
                        "units",
                        "Units on hand",
                        filtered.Sum(
                            item =>
                                item.QuantityOnHand
                        )
                    ),
                    Sum(
                        "low",
                        "Low stock",
                        filtered.Count(
                            item =>
                                GetStatus(item) ==
                                "Low stock"
                        )
                    ),
                    Sum(
                        "inactive",
                        "Inactive",
                        filtered.Count(
                            item =>
                                !item.IsActive
                        )
                    )
                };

            preview.FilterOptions["status"] =
                new()
                {
                    "All",
                    "Healthy",
                    "Low stock",
                    "Inactive"
                };

            preview.FilterOptions["medication"] =
                WithAll(
                    source
                        .Select(
                            item =>
                                item.MedicationName
                        )
                        .Distinct(
                            StringComparer
                                .OrdinalIgnoreCase
                        )
                        .OrderBy(
                            value =>
                                value
                        )
                        .ToList()
                );

            return preview;
        }

        private async Task<ClinicAdminDynamicReportPreviewDto>
            BuildStaffAsync(
                ClinicAdminReportScope scope,
                ClinicAdminDynamicReportQueryDto query
            )
        {
            var (from, to, endExclusive) =
                ResolveDateRange(query);

            var source =
                await _context.Users
                    .AsNoTracking()
                    .Include(
                        user =>
                            user.Nurse
                    )
                    .Include(
                        user =>
                            user.Proxy
                    )
                    .Where(
                        user =>
                            user.CreatedAt >=
                                from &&
                            user.CreatedAt <
                                endExclusive &&
                            (
                                (
                                    user.Role ==
                                        RoleNames.Nurse &&
                                    user.Nurse != null &&
                                    user.Nurse.ClinicId ==
                                        scope.ClinicId
                                ) ||
                                (
                                    user.Role ==
                                        RoleNames.Proxy &&
                                    user.Proxy != null &&
                                    user.Proxy.ClinicId ==
                                        scope.ClinicId
                                )
                            )
                    )
                    .OrderBy(
                        user =>
                            user.Role
                    )
                    .ThenBy(
                        user =>
                            user.FullName
                    )
                    .ToListAsync();

            var filtered =
                source
                    .Where(
                        item =>
                            MatchesOption(
                                query.Role,
                                item.Role
                            )
                    )
                    .Where(
                        item =>
                            MatchesStatus(
                                query.Status,
                                item.IsActive
                                    ? "Active"
                                    : "Inactive"
                            )
                    )
                    .Where(
                        item =>
                            MatchesSearch(
                                query.Search,
                                item.FullName,
                                item.Email,
                                item.PhoneNumber,
                                item.Role,
                                item.Nurse?.EmployeeNumber,
                                item.Nurse?.RegistrationNumber
                            )
                    )
                    .ToList();

            var preview =
                BasePreview(
                    "Staff",
                    "Clinic Staff Report",
                    scope,
                    from,
                    to
                );

            preview.Columns =
                new()
                {
                    Col(
                        "name",
                        "Staff Member"
                    ),
                    Col(
                        "role",
                        "Role"
                    ),
                    Col(
                        "email",
                        "Email"
                    ),
                    Col(
                        "phone",
                        "Phone"
                    ),
                    Col(
                        "employeeNumber",
                        "Employee No."
                    ),
                    Col(
                        "registrationNumber",
                        "Registration No."
                    ),
                    Col(
                        "status",
                        "Status",
                        "status"
                    ),
                    Col(
                        "registered",
                        "Registered",
                        "date"
                    )
                };

            preview.Rows =
                filtered
                    .Select(
                        item =>
                            Row(
                                ("name", item.FullName),
                                ("role", item.Role),
                                ("email", item.Email),
                                ("phone", item.PhoneNumber),
                                ("employeeNumber", item.Nurse?.EmployeeNumber ?? "—"),
                                ("registrationNumber", item.Nurse?.RegistrationNumber ?? "—"),
                                ("status",
                                    item.IsActive
                                        ? "Active"
                                        : "Inactive"
                                ),
                                ("registered", item.CreatedAt)
                            )
                    )
                    .ToList();

            preview.Summary =
                new()
                {
                    Sum(
                        "shown",
                        "Staff shown",
                        filtered.Count
                    ),
                    Sum(
                        "nurses",
                        "Nurses",
                        filtered.Count(
                            item =>
                                item.Role ==
                                RoleNames.Nurse
                        )
                    ),
                    Sum(
                        "proxies",
                        "Proxies",
                        filtered.Count(
                            item =>
                                item.Role ==
                                RoleNames.Proxy
                        )
                    ),
                    Sum(
                        "active",
                        "Active",
                        filtered.Count(
                            item =>
                                item.IsActive
                        )
                    )
                };

            preview.FilterOptions["role"] =
                new()
                {
                    "All",
                    RoleNames.Nurse,
                    RoleNames.Proxy
                };

            preview.FilterOptions["status"] =
                new()
                {
                    "All",
                    "Active",
                    "Inactive"
                };

            return preview;
        }

        private static ClinicAdminDynamicReportPreviewDto
            BasePreview(
                string reportType,
                string title,
                ClinicAdminReportScope scope,
                DateTime? from,
                DateTime? to
            )
        {
            return new ClinicAdminDynamicReportPreviewDto
            {
                ReportType =
                    reportType,

                Title =
                    title,

                ClinicName =
                    scope.ClinicName,

                RequestedBy =
                    scope.AdminName,

                GeneratedAt =
                    DateTime.UtcNow,

                DateFrom =
                    from,

                DateTo =
                    to
            };
        }

        private static ClinicAdminDynamicReportColumnDto
            Col(
                string key,
                string label,
                string dataType = "text"
            )
        {
            return new ClinicAdminDynamicReportColumnDto
            {
                Key = key,
                Label = label,
                DataType = dataType
            };
        }

        private static ClinicAdminDynamicReportSummaryDto
            Sum(
                string key,
                string label,
                object value
            )
        {
            return new ClinicAdminDynamicReportSummaryDto
            {
                Key = key,
                Label = label,
                Value = Convert.ToString(
                    value,
                    System.Globalization
                        .CultureInfo
                        .InvariantCulture
                ) ?? string.Empty
            };
        }

        private static Dictionary<string, object?>
            Row(
                params (string Key, object? Value)[]
                    values
            )
        {
            return values.ToDictionary(
                item =>
                    item.Key,
                item =>
                    item.Value
            );
        }

        private static (
            DateTime From,
            DateTime To,
            DateTime EndExclusive
        ) ResolveDateRange(
            ClinicAdminDynamicReportQueryDto query
        )
        {
            var to =
                (query.DateTo ??
                    DateTime.UtcNow)
                    .Date;

            var from =
                (query.DateFrom ??
                    to.AddDays(-29))
                    .Date;

            if (from > to)
            {
                throw new ArgumentException(
                    "The From date cannot be after the To date."
                );
            }

            if (
                (to - from).TotalDays >
                730
            )
            {
                throw new ArgumentException(
                    "A report date range cannot exceed 730 days."
                );
            }

            return (
                DateTime.SpecifyKind(
                    from,
                    DateTimeKind.Utc
                ),
                DateTime.SpecifyKind(
                    to,
                    DateTimeKind.Utc
                ),
                DateTime.SpecifyKind(
                    to.AddDays(1),
                    DateTimeKind.Utc
                )
            );
        }

        private static bool MatchesStatus(
            string? filter,
            string? value
        )
        {
            return
                string.IsNullOrWhiteSpace(
                    filter
                ) ||
                IsAll(filter) ||
                string.Equals(
                    filter,
                    value,
                    StringComparison
                        .OrdinalIgnoreCase
                );
        }

        private static bool MatchesOption(
            string? filter,
            string? value
        )
        {
            return MatchesStatus(
                filter,
                value
            );
        }

        private static bool MatchesSearch(
            string? search,
            params string?[] values
        )
        {
            if (
                string.IsNullOrWhiteSpace(
                    search
                )
            )
            {
                return true;
            }

            var term =
                search.Trim();

            return values.Any(
                value =>
                    !string.IsNullOrWhiteSpace(
                        value
                    ) &&
                    value.Contains(
                        term,
                        StringComparison
                            .OrdinalIgnoreCase
                    )
            );
        }

        private static bool IsAll(
            string value
        )
        {
            return value.Equals(
                "All",
                StringComparison
                    .OrdinalIgnoreCase
            );
        }

        private static List<string> WithAll(
            List<string> values
        )
        {
            return new[]
                {
                    "All"
                }
                .Concat(
                    values
                )
                .Distinct(
                    StringComparer
                        .OrdinalIgnoreCase
                )
                .ToList();
        }

        private static string GetAppointmentProvider(
            Appointment appointment
        )
        {
            if (
                !string.IsNullOrWhiteSpace(
                    appointment.ProviderName
                )
            )
            {
                return appointment.ProviderName;
            }

            if (
                appointment.Nurse?.User !=
                null
            )
            {
                return appointment.Nurse.User.FullName;
            }

            return "Clinic provider";
        }

        private static string NormalizeReportType(
            string? value
        )
        {
            var normalized =
                (value ?? "Appointments")
                    .Trim();

            if (
                normalized.Equals(
                    "MedicationAdherence",
                    StringComparison.OrdinalIgnoreCase
                ) ||
                normalized.Equals(
                    "Medication Adherence",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return "Medication Adherence";
            }

            var allowed =
                new[]
                {
                    "Patients",
                    "Appointments",
                    "Collections",
                    "Inventory",
                    "Staff"
                };

            var match =
                allowed.FirstOrDefault(
                    item =>
                        item.Equals(
                            normalized,
                            StringComparison.OrdinalIgnoreCase
                        )
                );

            if (
                string.IsNullOrWhiteSpace(
                    match
                )
            )
            {
                throw new ArgumentException(
                    "Report type must be Patients, Appointments, Collections, Medication Adherence, Inventory, or Staff."
                );
            }

            return match;
        }

        private async Task<ClinicAdminReportScope>
            GetScopeAsync(
                Guid userId
            )
        {
            var scope =
                await _context.Admins
                    .AsNoTracking()
                    .Where(
                        admin =>
                            admin.UserId ==
                                userId &&
                            admin.User.Role ==
                                RoleNames.ClinicAdmin &&
                            admin.User.IsActive &&
                            admin.ClinicId !=
                                null
                    )
                    .Select(
                        admin =>
                            new ClinicAdminReportScope
                            {
                                ClinicId =
                                    admin.ClinicId!.Value,

                                ClinicName =
                                    admin.Clinic!.Name,

                                AdminName =
                                    admin.FullName
                            }
                    )
                    .FirstOrDefaultAsync();

            if (scope == null)
            {
                throw new UnauthorizedAccessException(
                    "Assigned clinic could not be resolved."
                );
            }

            return scope;
        }

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
                throw new UnauthorizedAccessException(
                    "Invalid authentication token."
                );
            }

            return userId;
        }

        private static string SanitizeFileName(
            string value
        )
        {
            var invalid =
                Path.GetInvalidFileNameChars();

            return new string(
                    value
                        .Select(
                            character =>
                                invalid.Contains(
                                    character
                                )
                                    ? '-'
                                    : character
                        )
                        .ToArray()
                )
                .Trim()
                .Replace(
                    " ",
                    "-"
                );
        }

        private sealed class ClinicAdminReportScope
        {
            public Guid ClinicId { get; init; }

            public string ClinicName { get; init; } =
                string.Empty;

            public string AdminName { get; init; } =
                string.Empty;
        }
    }
}
