namespace PersonalProject.Models.DTOs
{
    public class ClinicAdminMeDto
    {
        public Guid UserId { get; set; }

        public int AdminId { get; set; }

        public string FullName { get; set; } =
            string.Empty;

        public string Email { get; set; } =
            string.Empty;

        public Guid ClinicId { get; set; }

        public string ClinicName { get; set; } =
            string.Empty;

        public bool IsActive { get; set; }
    }

    public class ClinicAdminOverviewDto
    {
        public Guid ClinicId { get; set; }

        public string ClinicName { get; set; } =
            string.Empty;

        public int ActivePatients { get; set; }

        public int ActiveNurses { get; set; }

        public int AppointmentsToday { get; set; }

        public int CollectionsDueToday { get; set; }

        public int OverdueCollections { get; set; }

        public int LowStockItems { get; set; }
    }
}