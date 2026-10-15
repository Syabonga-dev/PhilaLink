using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace PersonalProject.Models.Validation
{
    public sealed class AppointmentBookingWindowAttribute :
        ValidationAttribute
    {
        private const int MaximumAdvanceDays = 14;

        private static readonly TimeZoneInfo
            SouthAfricaTimeZone =
                ResolveSouthAfricaTimeZone();

        private static readonly HashSet<string>
            TerminalStatuses =
                new(
                    StringComparer.OrdinalIgnoreCase
                )
                {
                    "Completed",
                    "Cancelled",
                    "Canceled",
                    "Missed",
                    "NoShow",
                    "No-Show"
                };

        /*
         * UpdateAppointmentDto is also used for status changes
         * on historical appointments.
         *
         * When this option is enabled, completed, cancelled and
         * missed appointments can still be updated even though
         * their ScheduledAt value is naturally in the past.
         *
         * Active appointments such as Pending, Scheduled,
         * Confirmed and Rescheduled must still obey the
         * 14-day booking window.
         */
        public bool SkipTerminalStatuses
        {
            get;
            set;
        }

        public AppointmentBookingWindowAttribute()
        {
            ErrorMessage =
                "Appointments can only be booked from today up to 14 days in advance.";
        }

        protected override ValidationResult?
            IsValid(
                object? value,
                ValidationContext validationContext
            )
        {
            if (value == null)
            {
                return ValidationResult.Success;
            }

            if (value is not DateTime scheduledAt)
            {
                return new ValidationResult(
                    "Invalid appointment date."
                );
            }

            /*
             * A general appointment update can be used to mark
             * an old appointment as Completed, Cancelled or
             * Missed.
             *
             * Those updates should not fail simply because the
             * original appointment date is already in the past.
             */
            if (
                SkipTerminalStatuses &&
                HasTerminalStatus(
                    validationContext
                        .ObjectInstance
                )
            )
            {
                return ValidationResult.Success;
            }

            var nowUtc =
                DateTime.UtcNow;

            var nowSouthAfrica =
                TimeZoneInfo
                    .ConvertTimeFromUtc(
                        nowUtc,
                        SouthAfricaTimeZone
                    );

            var scheduledUtc =
                ConvertToUtc(
                    scheduledAt
                );

            var scheduledSouthAfrica =
                TimeZoneInfo
                    .ConvertTimeFromUtc(
                        scheduledUtc,
                        SouthAfricaTimeZone
                    );

            var today =
                nowSouthAfrica.Date;

            var maximumDate =
                today.AddDays(
                    MaximumAdvanceDays
                );

            var selectedDate =
                scheduledSouthAfrica.Date;

            /*
             * Users cannot select any calendar day that has
             * already passed in South Africa.
             */
            if (
                selectedDate <
                today
            )
            {
                return new ValidationResult(
                    "You cannot book an appointment for a date that has already passed."
                );
            }

            /*
             * The final permitted calendar day is exactly
             * fourteen days after today's date.
             *
             * Example:
             * 15 October -> 29 October is allowed.
             * 30 October is not allowed.
             */
            if (
                selectedDate >
                maximumDate
            )
            {
                return new ValidationResult(
                    "Appointments can only be booked up to 14 days in advance."
                );
            }

            /*
             * Today itself is allowed, but a time earlier than
             * the current time is not.
             */
            if (
                scheduledUtc <=
                nowUtc
            )
            {
                return new ValidationResult(
                    "Please choose an appointment time that has not already passed."
                );
            }

            return ValidationResult.Success;
        }

        private static bool
            HasTerminalStatus(
                object instance
            )
        {
            PropertyInfo? statusProperty =
                instance
                    .GetType()
                    .GetProperty(
                        "Status",
                        BindingFlags.Public |
                        BindingFlags.Instance
                    );

            if (
                statusProperty == null
            )
            {
                return false;
            }

            var status =
                statusProperty
                    .GetValue(
                        instance
                    )
                    ?.ToString()
                    ?.Trim();

            if (
                string.IsNullOrWhiteSpace(
                    status
                )
            )
            {
                return false;
            }

            return TerminalStatuses
                .Contains(
                    status
                );
        }

        private static DateTime
            ConvertToUtc(
                DateTime value
            )
        {
            if (
                value.Kind ==
                DateTimeKind.Utc
            )
            {
                return value;
            }

            if (
                value.Kind ==
                DateTimeKind.Local
            )
            {
                return value
                    .ToUniversalTime();
            }

            /*
             * If ASP.NET receives a DateTime without timezone
             * information, PhilaLink treats that value as South
             * African local time.
             */
            return TimeZoneInfo
                .ConvertTimeToUtc(
                    DateTime
                        .SpecifyKind(
                            value,
                            DateTimeKind
                                .Unspecified
                        ),
                    SouthAfricaTimeZone
                );
        }

        private static TimeZoneInfo
            ResolveSouthAfricaTimeZone()
        {
            try
            {
                /*
                 * Linux / Render / Docker timezone ID.
                 */
                return TimeZoneInfo
                    .FindSystemTimeZoneById(
                        "Africa/Johannesburg"
                    );
            }
            catch (
                TimeZoneNotFoundException
            )
            {
                /*
                 * Windows timezone ID.
                 */
                return TimeZoneInfo
                    .FindSystemTimeZoneById(
                        "South Africa Standard Time"
                    );
            }
        }
    }
}