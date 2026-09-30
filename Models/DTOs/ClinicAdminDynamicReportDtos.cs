namespace PersonalProject.Models.DTOs
{
    public class ClinicAdminDynamicReportQueryDto
    {
        public string ReportType { get; set; } = "Appointments";
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public string? Status { get; set; }
        public string? Search { get; set; }
        public string? Role { get; set; }
        public string? Provider { get; set; }
        public string? AppointmentType { get; set; }
        public string? Mode { get; set; }
        public string? Medication { get; set; }
    }

    public class ClinicAdminDynamicReportColumnDto
    {
        public string Key { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string DataType { get; set; } = "text";
    }

    public class ClinicAdminDynamicReportSummaryDto
    {
        public string Key { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    public class ClinicAdminDynamicReportPreviewDto
    {
        public string ReportType { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string ClinicName { get; set; } = string.Empty;
        public string RequestedBy { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }

        public List<ClinicAdminDynamicReportColumnDto> Columns { get; set; } = new();
        public List<Dictionary<string, object?>> Rows { get; set; } = new();
        public List<ClinicAdminDynamicReportSummaryDto> Summary { get; set; } = new();

        public Dictionary<string, List<string>> FilterOptions { get; set; } =
            new(StringComparer.OrdinalIgnoreCase);
    }
}
