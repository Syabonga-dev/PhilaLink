namespace PersonalProject.Models.Constants
{
    public static class AppointmentStatuses
    {
        public const string Scheduled = "Scheduled";
        public const string Confirmed = "Confirmed";
        public const string Pending = "Pending";
        public const string Completed = "Completed";
        public const string Cancelled = "Cancelled";
        public const string Missed = "Missed";
        public const string Rescheduled = "Rescheduled";

        public static readonly string[] All =
        {
            Scheduled,
            Confirmed,
            Pending,
            Completed,
            Cancelled,
            Missed,
            Rescheduled
        };

        public static bool IsValid(
            string? status
        )
        {
            return !string.IsNullOrWhiteSpace(status) &&
                   All.Any(
                       value =>
                           string.Equals(
                               value,
                               status,
                               StringComparison.OrdinalIgnoreCase
                           )
                   );
        }

        public static string Normalize(
            string status
        )
        {
            var match =
                All.FirstOrDefault(
                    value =>
                        string.Equals(
                            value,
                            status,
                            StringComparison.OrdinalIgnoreCase
                        )
                );

            if (match == null)
            {
                throw new InvalidOperationException(
                    "Invalid appointment status."
                );
            }

            return match;
        }
    }
}