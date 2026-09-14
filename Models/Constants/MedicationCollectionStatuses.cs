namespace PersonalProject.Models.Constants
{
    public static class MedicationCollectionStatuses
    {
        // Stored database statuses
        public const string Scheduled = "Scheduled";
        public const string Collected = "Collected";
        public const string Cancelled = "Cancelled";

        // Calculated display statuses
        public const string Pending = "Pending";
        public const string Overdue = "Overdue";

        public static bool IsFinal(
            string? status
        )
        {
            return
                status == Collected ||
                status == Cancelled;
        }
    }
}