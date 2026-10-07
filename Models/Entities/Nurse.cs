namespace PersonalProject.Models.Entities
{
    public class Nurse
    {
        private DateTime _employmentDate =
            DateTime.SpecifyKind(
                DateTime.MinValue,
                DateTimeKind.Utc
            );

        public Guid Id { get; set; }

        // =====================================================
        // USER
        // =====================================================

        public Guid UserId { get; set; }

        public User User { get; set; } = null!;

        // =====================================================
        // PROFESSIONAL INFORMATION
        // =====================================================

        public string EmployeeNumber { get; set; } =
            string.Empty;

        public string RegistrationNumber { get; set; } =
            string.Empty;

        public string Qualification { get; set; } =
            string.Empty;

        // =====================================================
        // CLINIC
        // =====================================================

        public Guid ClinicId { get; set; }

        public Clinic Clinic { get; set; } = null!;

        // =====================================================
        // CONTACT
        // =====================================================

        public string Email { get; set; } =
            string.Empty;

        // =====================================================
        // ADDRESS
        // =====================================================

        public string AddressLine1 { get; set; } =
            string.Empty;

        public string? AddressLine2 { get; set; }

        public string Suburb { get; set; } =
            string.Empty;

        public string City { get; set; } =
            string.Empty;

        public string Province { get; set; } =
            string.Empty;

        public string PostalCode { get; set; } =
            string.Empty;

        // =====================================================
        // PERSONAL INFORMATION
        // =====================================================

        public DateOnly DateOfBirth { get; set; }

        public string Gender { get; set; } =
            string.Empty;

        // =====================================================
        // EMPLOYMENT
        // =====================================================

        /*
         * PostgreSQL stores this column as
         * "timestamp with time zone".
         *
         * Date values coming from the browser are normally
         * deserialized as DateTimeKind.Unspecified because an
         * HTML date input sends YYYY-MM-DD without a timezone.
         *
         * Npgsql expects UTC for timestamp-with-time-zone values.
         * Normalize every value here so Nurse creation cannot
         * fail because of DateTimeKind.Unspecified.
         */
        public DateTime EmploymentDate
        {
            get =>
                _employmentDate;

            set =>
                _employmentDate =
                    NormalizeUtc(
                        value
                    );
        }

        // =====================================================
        // EMERGENCY CONTACT
        // =====================================================

        public string EmergencyContactName { get; set; } =
            string.Empty;

        public string EmergencyContactPhone { get; set; } =
            string.Empty;

        public string EmergencyContactRelationship { get; set; } =
            string.Empty;

        // =====================================================
        // AUDIT
        // =====================================================

        public DateTime CreatedAt { get; set; } =
            DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        private static DateTime NormalizeUtc(
            DateTime value
        )
        {
            return value.Kind switch
            {
                DateTimeKind.Utc =>
                    value,

                DateTimeKind.Local =>
                    value.ToUniversalTime(),

                _ =>
                    DateTime.SpecifyKind(
                        value,
                        DateTimeKind.Utc
                    )
            };
        }
    }
}
