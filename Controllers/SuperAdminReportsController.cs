using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalProject.Data;
using PersonalProject.Models.Constants;
using PersonalProject.Models.DTOs;
using PersonalProject.Models.Entities;
using PersonalProject.Services.Interfaces;
using PersonalProject.Utilities;
using System.Globalization;
using System.Security.Claims;

namespace PersonalProject.Controllers
{
    [ApiController]
    [Route("api/super-admin/report-builder")]
    [Authorize(Policy = "SuperAdminOnly")]
    public class SuperAdminReportsController :
        ControllerBase
    {
        private readonly PhilaLinkDbContext
            _context;

        private readonly IAuditLogService
            _audit;

        public SuperAdminReportsController(
            PhilaLinkDbContext context,
            IAuditLogService audit
        )
        {
            _context =
                context;

            _audit =
                audit;
        }

        [HttpGet("preview")]
        public async Task<IActionResult>
            Preview(
                [FromQuery]
                ClinicAdminDynamicReportQueryDto
                    query
            )
        {
            try
            {
                var scope =
                    await GetScopeAsync(
                        query.ClinicId
                    );

                return Ok(
                    await BuildPreviewAsync(
                        scope,
                        query
                    )
                );
            }
            catch (
                ArgumentException ex
            )
            {
                return BadRequest(
                    new
                    {
                        message =
                            ex.Message
                    }
                );
            }
            catch (
                KeyNotFoundException ex
            )
            {
                return NotFound(
                    new
                    {
                        message =
                            ex.Message
                    }
                );
            }
        }

        [HttpGet("export")]
        public async Task<IActionResult>
            Export(
                [FromQuery]
                ClinicAdminDynamicReportQueryDto
                    query,

                [FromQuery]
                string format =
                    "xlsx"
            )
        {
            try
            {
                var normalized =
                    (
                        format ??
                        string.Empty
                    )
                    .Trim()
                    .ToLowerInvariant();

                if (
                    normalized is not
                    (
                        "xlsx" or
                        "excel"
                    )
                )
                {
                    return BadRequest(
                        new
                        {
                            message =
                                "System report export format must be xlsx. Use the secure PDF endpoint for PDF exports."
                        }
                    );
                }

                var scope =
                    await GetScopeAsync(
                        query.ClinicId
                    );

                var report =
                    await BuildPreviewAsync(
                        scope,
                        query
                    );

                var bytes =
                    ClinicAdminDynamicReportBuilder
                        .BuildExcel(
                            report
                        );

                await _audit
                    .LogAsync(
                        "SystemReportExported",
                        GetCurrentUserId(),
                        $"Excel report '{report.ReportType}' exported for {report.ClinicName}.",
                        scope.ClinicId
                    );

                var stamp =
                    DateTime.UtcNow
                        .ToString(
                            "yyyyMMdd-HHmmss"
                        );

                return File(
                    bytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"PhilaLink-{SanitizeFileName(report.ClinicName)}-{SanitizeFileName(report.ReportType)}-{stamp}.xlsx"
                );
            }
            catch (
                ArgumentException ex
            )
            {
                return BadRequest(
                    new
                    {
                        message =
                            ex.Message
                    }
                );
            }
            catch (
                KeyNotFoundException ex
            )
            {
                return NotFound(
                    new
                    {
                        message =
                            ex.Message
                    }
                );
            }
        }

        [HttpPost("export/pdf")]
        [RequestSizeLimit(4_000_000)]
        public async Task<IActionResult>
            ExportSecurePdf(
                [FromBody]
                ClinicAdminSecurePdfExportRequestDto
                    request
            )
        {
            if (
                request ==
                null
            )
            {
                return BadRequest(
                    new
                    {
                        message =
                            "A PDF export request is required."
                    }
                );
            }

            if (
                string.IsNullOrWhiteSpace(
                    request.Password
                ) ||
                request.Password.Length <
                8
            )
            {
                return BadRequest(
                    new
                    {
                        message =
                            "The PDF password must contain at least 8 characters."
                    }
                );
            }

            if (
                request.Password.Length >
                128
            )
            {
                return BadRequest(
                    new
                    {
                        message =
                            "The PDF password cannot exceed 128 characters."
                    }
                );
            }

            try
            {
                var filters =
                    request.Filters ??
                    new ClinicAdminDynamicReportQueryDto();

                var scope =
                    await GetScopeAsync(
                        filters.ClinicId
                    );

                var report =
                    await BuildPreviewAsync(
                        scope,
                        filters
                    );

                var logo =
                    DecodeLogo(
                        request
                            .LogoJpegBase64
                    );

                var bytes =
                    ClinicAdminSecurePdfBuilder
                        .Build(
                            report,
                            request.Password,
                            logo
                        );

                await _audit
                    .LogAsync(
                        "SystemSecurePdfExported",
                        GetCurrentUserId(),
                        $"Password-protected PDF report '{report.ReportType}' exported for {report.ClinicName}.",
                        scope.ClinicId
                    );

                var stamp =
                    DateTime.UtcNow
                        .ToString(
                            "yyyyMMdd-HHmmss"
                        );

                return File(
                    bytes,
                    "application/pdf",
                    $"PhilaLink-{SanitizeFileName(report.ClinicName)}-{SanitizeFileName(report.ReportType)}-{stamp}-protected.pdf"
                );
            }
            catch (
                ArgumentException ex
            )
            {
                return BadRequest(
                    new
                    {
                        message =
                            ex.Message
                    }
                );
            }
            catch (
                KeyNotFoundException ex
            )
            {
                return NotFound(
                    new
                    {
                        message =
                            ex.Message
                    }
                );
            }
            catch (
                FormatException
            )
            {
                return BadRequest(
                    new
                    {
                        message =
                            "The supplied report logo is not valid base64 image data."
                    }
                );
            }
        }

        private async Task<ClinicAdminDynamicReportPreviewDto>
            BuildPreviewAsync(
                SuperAdminReportScope scope,
                ClinicAdminDynamicReportQueryDto query
            )
        {
            var reportType =
                NormalizeReportType(
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

                "Clinics" =>
                    await BuildClinicsAsync(
                        scope,
                        query
                    ),

                "Audit Activity" =>
                    await BuildAuditAsync(
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
                SuperAdminReportScope scope,
                ClinicAdminDynamicReportQueryDto query
            )
        {
            var (
                from,
                to,
                endExclusive
            ) =
                ResolveDateRange(
                    query
                );

            var sourceQuery =
                _context.Patients
                    .AsNoTracking()
                    .Where(
                        item =>
                            item.CreatedAt >=
                                from &&
                            item.CreatedAt <
                                endExclusive
                    );

            if (
                scope.ClinicId
                    .HasValue
            )
            {
                var clinicId =
                    scope.ClinicId
                        .Value;

                sourceQuery =
                    sourceQuery.Where(
                        item =>
                            item.ClinicId ==
                            clinicId
                    );
            }

            var source =
                await sourceQuery
                    .Select(
                        item =>
                            new
                            {
                                Clinic =
                                    item.Clinic !=
                                    null
                                        ? item
                                            .Clinic
                                            .Name
                                        : "Unassigned",

                                item.PatientNumber,

                                item.DateOfBirth,

                                item.Gender,

                                item.User.FullName,

                                item.User.Email,

                                item.User.PhoneNumber,

                                item.User.IsActive,

                                item.CreatedAt
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
                                item.Clinic,
                                item.PatientNumber,
                                item.FullName,
                                item.Email,
                                item.PhoneNumber,
                                item.Gender
                            )
                    )
                    .ToList();

            var report =
                BasePreview(
                    "Patients",
                    "System Patient Report",
                    scope,
                    from,
                    to
                );

            report.Columns =
                new()
                {
                    Col(
                        "clinic",
                        "Clinic"
                    ),

                    Col(
                        "patientNumber",
                        "Patient No."
                    ),

                    Col(
                        "patient",
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

            report.Rows =
                filtered
                    .Select(
                        item =>
                            Row(
                                (
                                    "clinic",
                                    item.Clinic
                                ),

                                (
                                    "patientNumber",
                                    item.PatientNumber
                                ),

                                (
                                    "patient",
                                    item.FullName
                                ),

                                (
                                    "gender",
                                    item.Gender
                                ),

                                (
                                    "dateOfBirth",
                                    item.DateOfBirth
                                        .ToString(
                                            "yyyy-MM-dd",
                                            CultureInfo.InvariantCulture
                                        )
                                ),

                                (
                                    "phone",
                                    item.PhoneNumber
                                ),

                                (
                                    "status",
                                    item.IsActive
                                        ? "Active"
                                        : "Inactive"
                                ),

                                (
                                    "registered",
                                    item.CreatedAt
                                )
                            )
                    )
                    .ToList();

            report.Summary =
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
                    ),

                    Sum(
                        "clinics",
                        "Clinics represented",
                        filtered
                            .Select(
                                item =>
                                    item.Clinic
                            )
                            .Distinct()
                            .Count()
                    )
                };

            report.FilterOptions[
                "status"
            ] =
                new()
                {
                    "All",
                    "Active",
                    "Inactive"
                };

            return report;
        }

        private async Task<ClinicAdminDynamicReportPreviewDto>
            BuildAppointmentsAsync(
                SuperAdminReportScope scope,
                ClinicAdminDynamicReportQueryDto query
            )
        {
            var (
                from,
                to,
                endExclusive
            ) =
                ResolveDateRange(
                    query
                );

            var sourceQuery =
                _context.Appointments
                    .AsNoTracking()
                    .Where(
                        item =>
                            item.ScheduledAt >=
                                from &&
                            item.ScheduledAt <
                                endExclusive
                    );

            if (
                scope.ClinicId
                    .HasValue
            )
            {
                var clinicId =
                    scope.ClinicId
                        .Value;

                sourceQuery =
                    sourceQuery.Where(
                        item =>
                            item.ClinicId ==
                            clinicId
                    );
            }

            var source =
                await sourceQuery
                    .Select(
                        item =>
                            new
                            {
                                Clinic =
                                    item.Clinic
                                        .Name,

                                item.Patient
                                    .PatientNumber,

                                Patient =
                                    item.Patient
                                        .User
                                        .FullName,

                                item.Type,

                                Provider =
                                    item.Nurse !=
                                    null
                                        ? item
                                            .Nurse
                                            .User
                                            .FullName
                                        : item.ProviderName ??
                                            "Unassigned",

                                item.Mode,

                                item.Status,

                                item.ScheduledAt,

                                item.DurationMinutes,

                                item.Reason
                            }
                    )
                    .OrderByDescending(
                        item =>
                            item.ScheduledAt
                    )
                    .ToListAsync();

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
                                query.Provider,
                                item.Provider
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
                                query.Mode,
                                item.Mode
                            )
                    )
                    .Where(
                        item =>
                            MatchesSearch(
                                query.Search,
                                item.Clinic,
                                item.PatientNumber,
                                item.Patient,
                                item.Type,
                                item.Provider,
                                item.Status,
                                item.Reason
                            )
                    )
                    .ToList();

            var report =
                BasePreview(
                    "Appointments",
                    "System Appointment Report",
                    scope,
                    from,
                    to
                );

            report.Columns =
                new()
                {
                    Col(
                        "clinic",
                        "Clinic"
                    ),

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
                        "mode",
                        "Mode"
                    )
                };

            report.Rows =
                filtered
                    .Select(
                        item =>
                            Row(
                                (
                                    "clinic",
                                    item.Clinic
                                ),

                                (
                                    "patientNumber",
                                    item.PatientNumber
                                ),

                                (
                                    "patient",
                                    item.Patient
                                ),

                                (
                                    "type",
                                    item.Type
                                ),

                                (
                                    "provider",
                                    item.Provider
                                ),

                                (
                                    "status",
                                    item.Status
                                ),

                                (
                                    "scheduled",
                                    item.ScheduledAt
                                ),

                                (
                                    "mode",
                                    item.Mode
                                )
                            )
                    )
                    .ToList();

            report.Summary =
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
                                item.Status
                                    .Equals(
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
                                item.Status
                                    .Equals(
                                        "Pending",
                                        StringComparison.OrdinalIgnoreCase
                                    )
                        )
                    ),

                    Sum(
                        "missed",
                        "Missed",
                        filtered.Count(
                            item =>
                                item.Status
                                    .Equals(
                                        "Missed",
                                        StringComparison.OrdinalIgnoreCase
                                    )
                        )
                    )
                };

            report.FilterOptions[
                "status"
            ] =
                WithAll(
                    source.Select(
                        item =>
                            item.Status
                    )
                );

            report.FilterOptions[
                "provider"
            ] =
                WithAll(
                    source.Select(
                        item =>
                            item.Provider
                    )
                );

            report.FilterOptions[
                "appointmentType"
            ] =
                WithAll(
                    source.Select(
                        item =>
                            item.Type
                    )
                );

            report.FilterOptions[
                "mode"
            ] =
                WithAll(
                    source.Select(
                        item =>
                            item.Mode
                    )
                );

            return report;
        }

        private async Task<ClinicAdminDynamicReportPreviewDto>
            BuildCollectionsAsync(
                SuperAdminReportScope scope,
                ClinicAdminDynamicReportQueryDto query
            )
        {
            var (
                from,
                to,
                endExclusive
            ) =
                ResolveDateRange(
                    query
                );

            var sourceQuery =
                _context
                    .MedicationCollections
                    .AsNoTracking()
                    .Include(
                        item =>
                            item.Clinic
                    )
                    .Include(
                        item =>
                            item.Patient
                    )
                    .ThenInclude(
                        patient =>
                            patient.User
                    )
                    .Include(
                        item =>
                            item.Proxy
                    )
                    .ThenInclude(
                        proxy =>
                            proxy!.User
                    )
                    .Include(
                        item =>
                            item.ProcessedByNurse
                    )
                    .ThenInclude(
                        nurse =>
                            nurse!.User
                    )
                    .Include(
                        item =>
                            item.Items
                    )
                    .ThenInclude(
                        line =>
                            line.Medication
                    )
                    .Where(
                        item =>
                            item.ScheduledCollectionDate >=
                                from &&
                            item.ScheduledCollectionDate <
                                endExclusive
                    );

            if (
                scope.ClinicId
                    .HasValue
            )
            {
                var clinicId =
                    scope.ClinicId
                        .Value;

                sourceQuery =
                    sourceQuery.Where(
                        item =>
                            item.ClinicId ==
                            clinicId
                    );
            }

            var source =
                await sourceQuery
                    .OrderByDescending(
                        item =>
                            item.ScheduledCollectionDate
                    )
                    .ToListAsync();

            string MedicationText(
                MedicationCollection item
            )
            {
                return string.Join(
                    ", ",
                    item.Items
                        .Select(
                            line =>
                                line.Medication
                                    .Name
                        )
                        .Where(
                            value =>
                                !string.IsNullOrWhiteSpace(
                                    value
                                )
                        )
                        .Distinct(
                            StringComparer.OrdinalIgnoreCase
                        )
                );
            }

            var medicationOptions =
                source
                    .SelectMany(
                        item =>
                            item.Items
                                .Select(
                                    line =>
                                        line.Medication
                                            .Name
                                )
                    )
                    .Where(
                        value =>
                            !string.IsNullOrWhiteSpace(
                                value
                            )
                    )
                    .Distinct(
                        StringComparer.OrdinalIgnoreCase
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
                            IsAll(
                                query.Medication
                            ) ||
                            item.Items
                                .Any(
                                    line =>
                                        line.Medication
                                            .Name
                                            .Equals(
                                                query.Medication,
                                                StringComparison.OrdinalIgnoreCase
                                            )
                                )
                    )
                    .Where(
                        item =>
                            MatchesSearch(
                                query.Search,
                                item.Clinic.Name,
                                item.Patient.PatientNumber,
                                item.Patient.User.FullName,
                                item.Status,
                                MedicationText(
                                    item
                                ),
                                item.Proxy?.User.FullName,
                                item.ProcessedByNurse?.User.FullName
                            )
                    )
                    .ToList();

            var report =
                BasePreview(
                    "Collections",
                    "System Medication Collection Report",
                    scope,
                    from,
                    to
                );

            report.Columns =
                new()
                {
                    Col(
                        "clinic",
                        "Clinic"
                    ),

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
                        "status",
                        "Status",
                        "status"
                    )
                };

            report.Rows =
                filtered
                    .Select(
                        item =>
                            Row(
                                (
                                    "clinic",
                                    item.Clinic.Name
                                ),

                                (
                                    "patientNumber",
                                    item.Patient.PatientNumber
                                ),

                                (
                                    "patient",
                                    item.Patient.User.FullName
                                ),

                                (
                                    "medication",
                                    MedicationText(
                                        item
                                    )
                                ),

                                (
                                    "quantity",
                                    item.Items.Sum(
                                        line =>
                                            line.Quantity
                                    )
                                ),

                                (
                                    "collector",
                                    item.Proxy !=
                                    null
                                        ? $"Proxy: {item.Proxy.User.FullName}"
                                        : "Patient"
                                ),

                                (
                                    "scheduled",
                                    item.ScheduledCollectionDate
                                ),

                                (
                                    "status",
                                    item.Status
                                )
                            )
                    )
                    .ToList();

            report.Summary =
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
                                item.Status
                                    .Equals(
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
                                item.Status
                                    .Equals(
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
                                item.Status
                                    .Equals(
                                        "Scheduled",
                                        StringComparison.OrdinalIgnoreCase
                                    )
                        )
                    )
                };

            report.FilterOptions[
                "status"
            ] =
                WithAll(
                    source.Select(
                        item =>
                            item.Status
                    )
                );

            report.FilterOptions[
                "medication"
            ] =
                WithAll(
                    medicationOptions
                );

            return report;
        }

        private async Task<ClinicAdminDynamicReportPreviewDto>
            BuildMedicationAdherenceAsync(
                SuperAdminReportScope scope,
                ClinicAdminDynamicReportQueryDto query
            )
        {
            var (
                from,
                to,
                endExclusive
            ) =
                ResolveDateRange(
                    query
                );

            var sourceQuery =
                _context.MedicationLogs
                    .AsNoTracking()
                    .Include(
                        item =>
                            item.Medication
                    )
                    .ThenInclude(
                        medication =>
                            medication.Patient
                    )
                    .ThenInclude(
                        patient =>
                            patient.User
                    )
                    .Include(
                        item =>
                            item.Medication
                    )
                    .ThenInclude(
                        medication =>
                            medication.Patient
                    )
                    .ThenInclude(
                        patient =>
                            patient.Clinic
                    )
                    .Where(
                        item =>
                            item.TakenAt >=
                                from &&
                            item.TakenAt <
                                endExclusive
                    );

            if (
                scope.ClinicId
                    .HasValue
            )
            {
                var clinicId =
                    scope.ClinicId
                        .Value;

                sourceQuery =
                    sourceQuery.Where(
                        item =>
                            item.Medication
                                .Patient
                                .ClinicId ==
                            clinicId
                    );
            }

            var source =
                await sourceQuery
                    .OrderByDescending(
                        item =>
                            item.TakenAt
                    )
                    .ToListAsync();

            var medicationOptions =
                source
                    .Select(
                        item =>
                            item.Medication
                                .Name
                    )
                    .Where(
                        value =>
                            !string.IsNullOrWhiteSpace(
                                value
                            )
                    )
                    .Distinct(
                        StringComparer.OrdinalIgnoreCase
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
                            IsAll(
                                query.Medication
                            ) ||
                            item.Medication
                                .Name
                                .Equals(
                                    query.Medication,
                                    StringComparison.OrdinalIgnoreCase
                                )
                    )
                    .Where(
                        item =>
                            MatchesSearch(
                                query.Search,
                                item.Medication.Patient.Clinic?.Name,
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

            var rate =
                filtered.Count ==
                0
                    ? 0
                    : Math.Round(
                        taken *
                        100d /
                        filtered.Count,
                        1
                    );

            var report =
                BasePreview(
                    "Medication Adherence",
                    "System Medication Adherence Report",
                    scope,
                    from,
                    to
                );

            report.Columns =
                new()
                {
                    Col(
                        "clinic",
                        "Clinic"
                    ),

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
                    )
                };

            report.Rows =
                filtered
                    .Select(
                        item =>
                            Row(
                                (
                                    "clinic",
                                    item.Medication.Patient.Clinic?.Name ??
                                    "Unassigned"
                                ),

                                (
                                    "patientNumber",
                                    item.Medication.Patient.PatientNumber
                                ),

                                (
                                    "patient",
                                    item.Medication.Patient.User.FullName
                                ),

                                (
                                    "medication",
                                    item.Medication.Name
                                ),

                                (
                                    "dosage",
                                    item.Medication.Dosage
                                ),

                                (
                                    "form",
                                    item.Medication.Form
                                ),

                                (
                                    "result",
                                    item.Taken
                                        ? "Taken"
                                        : "Missed"
                                ),

                                (
                                    "recorded",
                                    item.TakenAt
                                )
                            )
                    )
                    .ToList();

            report.Summary =
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
                        $"{rate:0.#}%"
                    )
                };

            report.FilterOptions[
                "status"
            ] =
                new()
                {
                    "All",
                    "Taken",
                    "Missed"
                };

            report.FilterOptions[
                "medication"
            ] =
                WithAll(
                    medicationOptions
                );

            return report;
        }

        private async Task<ClinicAdminDynamicReportPreviewDto>
            BuildInventoryAsync(
                SuperAdminReportScope scope,
                ClinicAdminDynamicReportQueryDto query
            )
        {
            var sourceQuery =
                _context.ClinicStocks
                    .AsNoTracking()
                    .Include(
                        item =>
                            item.Clinic
                    )
                    .AsQueryable();

            if (
                scope.ClinicId
                    .HasValue
            )
            {
                var clinicId =
                    scope.ClinicId
                        .Value;

                sourceQuery =
                    sourceQuery.Where(
                        item =>
                            item.ClinicId ==
                            clinicId
                    );
            }

            var source =
                await sourceQuery
                    .OrderBy(
                        item =>
                            item.Clinic
                                .Name
                    )
                    .ThenBy(
                        item =>
                            item.MedicationName
                    )
                    .ToListAsync();

            string StockStatus(
                ClinicStock item
            )
            {
                if (
                    !item.IsActive
                )
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
                                StockStatus(
                                    item
                                )
                            )
                    )
                    .Where(
                        item =>
                            IsAll(
                                query.Medication
                            ) ||
                            item.MedicationName
                                .Equals(
                                    query.Medication,
                                    StringComparison.OrdinalIgnoreCase
                                )
                    )
                    .Where(
                        item =>
                            MatchesSearch(
                                query.Search,
                                item.Clinic.Name,
                                item.MedicationName,
                                item.Strength,
                                item.Form,
                                item.Unit
                            )
                    )
                    .ToList();

            var report =
                BasePreview(
                    "Inventory",
                    "System Medication Inventory Report",
                    scope,
                    null,
                    null
                );

            report.Columns =
                new()
                {
                    Col(
                        "clinic",
                        "Clinic"
                    ),

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

            report.Rows =
                filtered
                    .Select(
                        item =>
                            Row(
                                (
                                    "clinic",
                                    item.Clinic.Name
                                ),

                                (
                                    "medication",
                                    item.MedicationName
                                ),

                                (
                                    "strength",
                                    item.Strength
                                ),

                                (
                                    "form",
                                    item.Form
                                ),

                                (
                                    "quantity",
                                    item.QuantityOnHand
                                ),

                                (
                                    "reorderLevel",
                                    item.ReorderLevel
                                ),

                                (
                                    "status",
                                    StockStatus(
                                        item
                                    )
                                ),

                                (
                                    "updated",
                                    item.UpdatedAt ??
                                    item.CreatedAt
                                )
                            )
                    )
                    .ToList();

            report.Summary =
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
                                StockStatus(
                                    item
                                ) ==
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

            report.FilterOptions[
                "status"
            ] =
                new()
                {
                    "All",
                    "Healthy",
                    "Low stock",
                    "Inactive"
                };

            report.FilterOptions[
                "medication"
            ] =
                WithAll(
                    source.Select(
                        item =>
                            item.MedicationName
                    )
                );

            return report;
        }

        private async Task<ClinicAdminDynamicReportPreviewDto>
            BuildStaffAsync(
                SuperAdminReportScope scope,
                ClinicAdminDynamicReportQueryDto query
            )
        {
            var (
                from,
                to,
                endExclusive
            ) =
                ResolveDateRange(
                    query
                );

            var sourceQuery =
                _context.Users
                    .AsNoTracking()
                    .Include(
                        item =>
                            item.Nurse
                    )
                    .ThenInclude(
                        nurse =>
                            nurse!.Clinic
                    )
                    .Include(
                        item =>
                            item.Proxy
                    )
                    .ThenInclude(
                        proxy =>
                            proxy!.Clinic
                    )
                    .Include(
                        item =>
                            item.Admin
                    )
                    .ThenInclude(
                        admin =>
                            admin!.Clinic
                    )
                    .Where(
                        item =>
                            item.CreatedAt >=
                                from &&
                            item.CreatedAt <
                                endExclusive &&
                            (
                                item.Role ==
                                    RoleNames.Nurse ||
                                item.Role ==
                                    RoleNames.Proxy ||
                                item.Role ==
                                    RoleNames.ClinicAdmin ||
                                item.Role ==
                                    RoleNames.SuperAdmin
                            )
                    );

            if (
                scope.ClinicId
                    .HasValue
            )
            {
                var clinicId =
                    scope.ClinicId
                        .Value;

                sourceQuery =
                    sourceQuery.Where(
                        item =>
                            (
                                item.Role ==
                                    RoleNames.Nurse &&
                                item.Nurse !=
                                    null &&
                                item.Nurse.ClinicId ==
                                    clinicId
                            ) ||
                            (
                                item.Role ==
                                    RoleNames.Proxy &&
                                item.Proxy !=
                                    null &&
                                item.Proxy.ClinicId ==
                                    clinicId
                            ) ||
                            (
                                item.Role ==
                                    RoleNames.ClinicAdmin &&
                                item.Admin !=
                                    null &&
                                item.Admin.ClinicId ==
                                    clinicId
                            )
                    );
            }

            var source =
                await sourceQuery
                    .OrderBy(
                        item =>
                            item.Role
                    )
                    .ThenBy(
                        item =>
                            item.FullName
                    )
                    .ToListAsync();

            string ClinicName(
                User item
            )
            {
                return item.Role switch
                {
                    RoleNames.Nurse =>
                        item.Nurse?.Clinic?.Name ??
                        "Unassigned",

                    RoleNames.Proxy =>
                        item.Proxy?.Clinic?.Name ??
                        "Unassigned",

                    RoleNames.ClinicAdmin =>
                        item.Admin?.Clinic?.Name ??
                        "Unassigned",

                    RoleNames.SuperAdmin =>
                        "National administration",

                    _ =>
                        "Unassigned"
                };
            }

            string Identifier(
                User item
            )
            {
                if (
                    item.Role ==
                    RoleNames.Nurse
                )
                {
                    return item.Nurse
                        ?.EmployeeNumber ??
                        "—";
                }

                if (
                    item.Role ==
                    RoleNames.ClinicAdmin
                )
                {
                    return $"Admin #{item.Admin?.Id}";
                }

                return "—";
            }

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
                                ClinicName(
                                    item
                                ),
                                item.FullName,
                                item.Email,
                                item.PhoneNumber,
                                item.Role,
                                Identifier(
                                    item
                                )
                            )
                    )
                    .ToList();

            var report =
                BasePreview(
                    "Staff",
                    "System Staff Report",
                    scope,
                    from,
                    to
                );

            report.Columns =
                new()
                {
                    Col(
                        "clinic",
                        "Clinic"
                    ),

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
                        "identifier",
                        "Identifier"
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

            report.Rows =
                filtered
                    .Select(
                        item =>
                            Row(
                                (
                                    "clinic",
                                    ClinicName(
                                        item
                                    )
                                ),

                                (
                                    "name",
                                    item.FullName
                                ),

                                (
                                    "role",
                                    item.Role
                                ),

                                (
                                    "email",
                                    item.Email
                                ),

                                (
                                    "phone",
                                    item.PhoneNumber
                                ),

                                (
                                    "identifier",
                                    Identifier(
                                        item
                                    )
                                ),

                                (
                                    "status",
                                    item.IsActive
                                        ? "Active"
                                        : "Inactive"
                                ),

                                (
                                    "registered",
                                    item.CreatedAt
                                )
                            )
                    )
                    .ToList();

            report.Summary =
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
                        "clinicAdmins",
                        "Clinic admins",
                        filtered.Count(
                            item =>
                                item.Role ==
                                RoleNames.ClinicAdmin
                        )
                    )
                };

            report.FilterOptions[
                "role"
            ] =
                new()
                {
                    "All",
                    RoleNames.Nurse,
                    RoleNames.Proxy,
                    RoleNames.ClinicAdmin,
                    RoleNames.SuperAdmin
                };

            report.FilterOptions[
                "status"
            ] =
                new()
                {
                    "All",
                    "Active",
                    "Inactive"
                };

            return report;
        }

        private async Task<ClinicAdminDynamicReportPreviewDto>
            BuildClinicsAsync(
                SuperAdminReportScope scope,
                ClinicAdminDynamicReportQueryDto query
            )
        {
            var (
                from,
                to,
                endExclusive
            ) =
                ResolveDateRange(
                    query
                );

            var sourceQuery =
                _context.Clinics
                    .AsNoTracking()
                    .Where(
                        item =>
                            item.CreatedAt >=
                                from &&
                            item.CreatedAt <
                                endExclusive
                    );

            if (
                scope.ClinicId
                    .HasValue
            )
            {
                var clinicId =
                    scope.ClinicId
                        .Value;

                sourceQuery =
                    sourceQuery.Where(
                        item =>
                            item.Id ==
                            clinicId
                    );
            }

            var source =
                await sourceQuery
                    .Select(
                        item =>
                            new
                            {
                                item.Id,

                                item.Name,

                                item.Type,

                                item.Address,

                                item.ContactNumber,

                                item.IsActive,

                                item.CreatedAt,

                                Patients =
                                    _context.Patients
                                        .Count(
                                            patient =>
                                                patient.ClinicId ==
                                                item.Id
                                        ),

                                Nurses =
                                    _context.Nurses
                                        .Count(
                                            nurse =>
                                                nurse.ClinicId ==
                                                item.Id
                                        ),

                                ClinicAdmins =
                                    _context.Admins
                                        .Count(
                                            admin =>
                                                admin.ClinicId ==
                                                    item.Id &&
                                                admin.User.Role ==
                                                    RoleNames.ClinicAdmin
                                        )
                            }
                    )
                    .OrderBy(
                        item =>
                            item.Name
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
                                item.Name,
                                item.Type,
                                item.Address,
                                item.ContactNumber
                            )
                    )
                    .ToList();

            var report =
                BasePreview(
                    "Clinics",
                    "Clinic Directory Report",
                    scope,
                    from,
                    to
                );

            report.Columns =
                new()
                {
                    Col(
                        "clinic",
                        "Clinic"
                    ),

                    Col(
                        "type",
                        "Type"
                    ),

                    Col(
                        "address",
                        "Address"
                    ),

                    Col(
                        "contact",
                        "Contact"
                    ),

                    Col(
                        "patients",
                        "Patients",
                        "number"
                    ),

                    Col(
                        "nurses",
                        "Nurses",
                        "number"
                    ),

                    Col(
                        "clinicAdmins",
                        "Clinic Admins",
                        "number"
                    ),

                    Col(
                        "status",
                        "Status",
                        "status"
                    )
                };

            report.Rows =
                filtered
                    .Select(
                        item =>
                            Row(
                                (
                                    "clinic",
                                    item.Name
                                ),

                                (
                                    "type",
                                    item.Type
                                ),

                                (
                                    "address",
                                    item.Address
                                ),

                                (
                                    "contact",
                                    item.ContactNumber
                                ),

                                (
                                    "patients",
                                    item.Patients
                                ),

                                (
                                    "nurses",
                                    item.Nurses
                                ),

                                (
                                    "clinicAdmins",
                                    item.ClinicAdmins
                                ),

                                (
                                    "status",
                                    item.IsActive
                                        ? "Active"
                                        : "Inactive"
                                )
                            )
                    )
                    .ToList();

            report.Summary =
                new()
                {
                    Sum(
                        "shown",
                        "Clinics shown",
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
                        "patients",
                        "Patients",
                        filtered.Sum(
                            item =>
                                item.Patients
                        )
                    ),

                    Sum(
                        "nurses",
                        "Nurses",
                        filtered.Sum(
                            item =>
                                item.Nurses
                        )
                    )
                };

            report.FilterOptions[
                "status"
            ] =
                new()
                {
                    "All",
                    "Active",
                    "Inactive"
                };

            return report;
        }

        private async Task<ClinicAdminDynamicReportPreviewDto>
            BuildAuditAsync(
                SuperAdminReportScope scope,
                ClinicAdminDynamicReportQueryDto query
            )
        {
            var (
                from,
                to,
                endExclusive
            ) =
                ResolveDateRange(
                    query
                );

            var sourceQuery =
                _context.AuditLogs
                    .AsNoTracking()
                    .Include(
                        item =>
                            item.PerformedByUser
                    )
                    .Include(
                        item =>
                            item.Clinic
                    )
                    .Where(
                        item =>
                            item.Timestamp >=
                                from &&
                            item.Timestamp <
                                endExclusive
                    );

            if (
                scope.ClinicId
                    .HasValue
            )
            {
                var clinicId =
                    scope.ClinicId
                        .Value;

                sourceQuery =
                    sourceQuery.Where(
                        item =>
                            item.ClinicId ==
                            clinicId
                    );
            }

            var source =
                await sourceQuery
                    .OrderByDescending(
                        item =>
                            item.Timestamp
                    )
                    .ToListAsync();

            var filtered =
                source
                    .Where(
                        item =>
                            MatchesOption(
                                query.Role,
                                item.PerformedByUser?.Role ??
                                "System"
                            )
                    )
                    .Where(
                        item =>
                            MatchesSearch(
                                query.Search,
                                item.Action,
                                item.PerformedByUser?.FullName,
                                item.PerformedByUser?.Role,
                                item.Clinic?.Name,
                                item.Details
                            )
                    )
                    .ToList();

            var report =
                BasePreview(
                    "Audit Activity",
                    "System Audit Activity Report",
                    scope,
                    from,
                    to
                );

            report.Columns =
                new()
                {
                    Col(
                        "timestamp",
                        "Date / Time",
                        "datetime"
                    ),

                    Col(
                        "action",
                        "Action"
                    ),

                    Col(
                        "performedBy",
                        "Performed By"
                    ),

                    Col(
                        "role",
                        "Role"
                    ),

                    Col(
                        "clinic",
                        "Clinic"
                    ),

                    Col(
                        "details",
                        "Details"
                    )
                };

            report.Rows =
                filtered
                    .Select(
                        item =>
                            Row(
                                (
                                    "timestamp",
                                    item.Timestamp
                                ),

                                (
                                    "action",
                                    item.Action
                                ),

                                (
                                    "performedBy",
                                    item.PerformedByUser?.FullName ??
                                    "System"
                                ),

                                (
                                    "role",
                                    item.PerformedByUser?.Role ??
                                    "System"
                                ),

                                (
                                    "clinic",
                                    item.Clinic?.Name ??
                                    "System-wide"
                                ),

                                (
                                    "details",
                                    item.Details
                                )
                            )
                    )
                    .ToList();

            report.Summary =
                new()
                {
                    Sum(
                        "shown",
                        "Entries shown",
                        filtered.Count
                    ),

                    Sum(
                        "systemWide",
                        "System-wide",
                        filtered.Count(
                            item =>
                                item.ClinicId ==
                                null
                        )
                    ),

                    Sum(
                        "clinicScoped",
                        "Clinic-scoped",
                        filtered.Count(
                            item =>
                                item.ClinicId !=
                                null
                        )
                    ),

                    Sum(
                        "actors",
                        "Actors",
                        filtered
                            .Where(
                                item =>
                                    item.PerformedByUserId !=
                                    null
                            )
                            .Select(
                                item =>
                                    item.PerformedByUserId
                            )
                            .Distinct()
                            .Count()
                    )
                };

            report.FilterOptions[
                "role"
            ] =
                WithAll(
                    source.Select(
                        item =>
                            item.PerformedByUser?.Role ??
                            "System"
                    )
                );

            return report;
        }

        private async Task<SuperAdminReportScope>
            GetScopeAsync(
                Guid? clinicId
            )
        {
            var userId =
                GetCurrentUserId();

            var adminName =
                await _context.Users
                    .AsNoTracking()
                    .Where(
                        user =>
                            user.Id ==
                                userId &&
                            user.Role ==
                                RoleNames.SuperAdmin &&
                            user.IsActive
                    )
                    .Select(
                        user =>
                            user.FullName
                    )
                    .FirstOrDefaultAsync();

            if (
                string.IsNullOrWhiteSpace(
                    adminName
                )
            )
            {
                throw new UnauthorizedAccessException(
                    "Super Administrator account could not be resolved."
                );
            }

            if (
                !clinicId.HasValue
            )
            {
                return new SuperAdminReportScope
                {
                    ClinicId =
                        null,

                    ClinicName =
                        "All clinics",

                    AdminName =
                        adminName
                };
            }

            var clinicName =
                await _context.Clinics
                    .AsNoTracking()
                    .Where(
                        clinic =>
                            clinic.Id ==
                            clinicId.Value
                    )
                    .Select(
                        clinic =>
                            clinic.Name
                    )
                    .FirstOrDefaultAsync();

            if (
                string.IsNullOrWhiteSpace(
                    clinicName
                )
            )
            {
                throw new KeyNotFoundException(
                    "Clinic not found."
                );
            }

            return new SuperAdminReportScope
            {
                ClinicId =
                    clinicId,

                ClinicName =
                    clinicName,

                AdminName =
                    adminName
            };
        }

        private static ClinicAdminDynamicReportPreviewDto
            BasePreview(
                string reportType,
                string title,
                SuperAdminReportScope scope,
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
                string dataType =
                    "text"
            )
        {
            return new ClinicAdminDynamicReportColumnDto
            {
                Key =
                    key,

                Label =
                    label,

                DataType =
                    dataType
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
                Key =
                    key,

                Label =
                    label,

                Value =
                    Convert.ToString(
                        value,
                        CultureInfo.InvariantCulture
                    ) ??
                    string.Empty
            };
        }

        private static Dictionary<string, object?>
            Row(
                params (
                    string Key,
                    object? Value
                )[]
                values
            )
        {
            return values
                .ToDictionary(
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
                (
                    query.DateTo ??
                    DateTime.UtcNow
                )
                .Date;

            var from =
                (
                    query.DateFrom ??
                    to.AddDays(
                        -29
                    )
                )
                .Date;

            if (
                from >
                to
            )
            {
                throw new ArgumentException(
                    "The From date cannot be after the To date."
                );
            }

            if (
                (
                    to -
                    from
                )
                .TotalDays >
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
                    to.AddDays(
                        1
                    ),
                    DateTimeKind.Utc
                )
            );
        }

        private static string NormalizeReportType(
            string? value
        )
        {
            var supported =
                new[]
                {
                    "Patients",
                    "Appointments",
                    "Collections",
                    "Medication Adherence",
                    "Inventory",
                    "Staff",
                    "Clinics",
                    "Audit Activity"
                };

            var normalized =
                (
                    value ??
                    string.Empty
                )
                .Trim();

            var match =
                supported
                    .FirstOrDefault(
                        item =>
                            item.Equals(
                                normalized,
                                StringComparison.OrdinalIgnoreCase
                            )
                    );

            if (
                match ==
                null
            )
            {
                throw new ArgumentException(
                    "Report type must be Patients, Appointments, Collections, Medication Adherence, Inventory, Staff, Clinics, or Audit Activity."
                );
            }

            return match;
        }

        private static bool IsAll(
            string? value
        )
        {
            return string.IsNullOrWhiteSpace(
                    value
                ) ||
                value.Equals(
                    "All",
                    StringComparison.OrdinalIgnoreCase
                );
        }

        private static bool MatchesStatus(
            string? filter,
            string? value
        )
        {
            return IsAll(
                    filter
                ) ||
                string.Equals(
                    filter,
                    value,
                    StringComparison.OrdinalIgnoreCase
                );
        }

        private static bool MatchesOption(
            string? filter,
            string? value
        )
        {
            return IsAll(
                    filter
                ) ||
                string.Equals(
                    filter,
                    value,
                    StringComparison.OrdinalIgnoreCase
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
                        StringComparison.OrdinalIgnoreCase
                    )
            );
        }

        private static List<string>
            WithAll(
                IEnumerable<string?>
                    values
            )
        {
            return new[]
                {
                    "All"
                }
                .Concat(
                    values
                        .Where(
                            value =>
                                !string.IsNullOrWhiteSpace(
                                    value
                                )
                        )
                        .Select(
                            value =>
                                value!
                        )
                        .Distinct(
                            StringComparer.OrdinalIgnoreCase
                        )
                        .OrderBy(
                            value =>
                                value
                        )
                )
                .ToList();
        }

        private static byte[]?
            DecodeLogo(
                string?
                    logoJpegBase64
            )
        {
            if (
                string.IsNullOrWhiteSpace(
                    logoJpegBase64
                )
            )
            {
                return null;
            }

            var base64 =
                logoJpegBase64
                    .Trim();

            var comma =
                base64.IndexOf(
                    ','
                );

            if (
                comma >=
                0
            )
            {
                base64 =
                    base64[
                        (
                            comma +
                            1
                        )..
                    ];
            }

            var bytes =
                Convert
                    .FromBase64String(
                        base64
                    );

            if (
                bytes.Length >
                1_500_000
            )
            {
                throw new ArgumentException(
                    "The supplied report logo is too large."
                );
            }

            return bytes;
        }

        private static string
            SanitizeFileName(
                string value
            )
        {
            var invalid =
                Path.GetInvalidFileNameChars();

            var safe =
                new string(
                    (
                        value ??
                        string.Empty
                    )
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
                .Trim();

            if (
                string.IsNullOrWhiteSpace(
                    safe
                )
            )
            {
                return "PhilaLink";
            }

            return string.Join(
                "-",
                safe.Split(
                    new[]
                    {
                        ' ',
                        '\t',
                        '\r',
                        '\n'
                    },
                    StringSplitOptions
                        .RemoveEmptyEntries
                )
            );
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

        private sealed class
            SuperAdminReportScope
        {
            public Guid? ClinicId
            {
                get;
                init;
            }

            public string ClinicName
            {
                get;
                init;
            } =
                string.Empty;

            public string AdminName
            {
                get;
                init;
            } =
                string.Empty;
        }
    }
}
