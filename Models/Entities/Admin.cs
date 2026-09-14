using PersonalProject.Models.Entities;

namespace PersonalProject.Models
{
    public class Admin
    {
        public int Id { get; set; }

        // =====================================================
        // USER ACCOUNT
        // =====================================================

        public Guid UserId { get; set; }

        public User User { get; set; } = null!;

        // =====================================================
        // PROFILE
        // =====================================================

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        // =====================================================
        // CLINIC SCOPE
        // =====================================================

        /*
         * SuperAdmin:
         * ClinicId = null
         *
         * ClinicAdmin:
         * ClinicId = clinic they are responsible for
         *
         * The service layer will enforce that a ClinicAdmin
         * must have a clinic assigned.
         */
        public Guid? ClinicId { get; set; }

        public Clinic? Clinic { get; set; }

        // =====================================================
        // AUDIT
        // =====================================================

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}