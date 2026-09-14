namespace PersonalProject.Models.DTOs
{
    public class NurseMeDto
    {
        public Guid NurseId { get; set; }

        public Guid UserId { get; set; }

        public string FullName { get; set; } =
            string.Empty;

        public string EmployeeNumber { get; set; } =
            string.Empty;

        public string RegistrationNumber { get; set; } =
            string.Empty;

        public string Qualification { get; set; } =
            string.Empty;

        public Guid ClinicId { get; set; }

        public string ClinicName { get; set; } =
            string.Empty;

        public string Email { get; set; } =
            string.Empty;
    }

    public class NurseDashboardDto
    {
        public int ClinicPatients { get; set; }

        public int AppointmentsToday { get; set; }

        public int CollectionsDueToday { get; set; }

        public int OverdueCollections { get; set; }

        public int LowStockItems { get; set; }
    }

    public class NursePatientDto
    {
        public Guid PatientId { get; set; }

        public Guid UserId { get; set; }

        public string PatientNumber { get; set; } =
            string.Empty;

        public string FullName { get; set; } =
            string.Empty;

        public DateOnly? DateOfBirth { get; set; }

        public string? Gender { get; set; }

        public string PhoneNumber { get; set; } =
            string.Empty;
    }


}