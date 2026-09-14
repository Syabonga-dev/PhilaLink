namespace PersonalProject.Models.Constants
{
    public static class RoleNames
    {
        public const string SuperAdmin = "SuperAdmin";
        public const string ClinicAdmin = "ClinicAdmin";
        public const string Nurse = "Nurse";
        public const string Proxy = "Proxy";
        public const string Patient = "Patient";

        public static readonly string[] All =
        {
            SuperAdmin,
            ClinicAdmin,
            Nurse,
            Proxy,
            Patient
        };

        public static readonly string[] AdminRoles =
        {
            SuperAdmin,
            ClinicAdmin
        };

        public static bool IsValid(string? role)
        {
            if (string.IsNullOrWhiteSpace(role))
                return false;

            return All.Contains(role, StringComparer.OrdinalIgnoreCase);
        }
    }
}