using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalProject.Data;
using PersonalProject.Models.Constants;
using PersonalProject.Models.DTOs;
using PersonalProject.Models.Entities;
using System.Security.Claims;

namespace PersonalProject.Controllers
{
    [ApiController]
    [Route("api/notifications/me")]
    [Authorize]
    public class MyNotificationsController :
        ControllerBase
    {
        private static readonly TimeZoneInfo
            SouthAfricaTimeZone =
                ResolveSouthAfricaTimeZone();

        private readonly PhilaLinkDbContext
            _context;

        public MyNotificationsController(
            PhilaLinkDbContext context
        )
        {
            _context =
                context;
        }

        // =====================================================
        // GET MY NOTIFICATIONS
        // =====================================================

        [HttpGet]
        public async Task<IActionResult>
            GetMine()
        {
            var userId =
                GetCurrentUserId();

            /*
             * Patient reminders are generated when the patient
             * notification feed is refreshed.
             *
             * The Patient frontend polls this endpoint while the
             * portal is open.
             */
            await EnsurePatientRemindersAsync(
                userId
            );

            /*
             * Preserve the existing Proxy collection reminder
             * behaviour.
             */
            await EnsureProxyCollectionRemindersAsync(
                userId
            );

            var notifications =
                await _context.Notifications
                    .AsNoTracking()
                    .Where(
                        notification =>
                            notification.UserId ==
                                userId
                    )
                    .OrderByDescending(
                        notification =>
                            notification.CreatedAt
                    )
                    .Take(50)
                    .Select(
                        notification =>
                            new NotificationResponseDto
                            {
                                Id =
                                    notification.Id,

                                UserId =
                                    notification.UserId,

                                Message =
                                    notification.Message,

                                IsRead =
                                    notification.IsRead,

                                CreatedAt =
                                    notification.CreatedAt
                            }
                    )
                    .ToListAsync();

            return Ok(
                notifications
            );
        }

        // =====================================================
        // MARK ONE READ
        // =====================================================

        [HttpPatch("{id:guid}/read")]
        public async Task<IActionResult>
            MarkRead(
                Guid id
            )
        {
            var userId =
                GetCurrentUserId();

            var notification =
                await _context.Notifications
                    .FirstOrDefaultAsync(
                        item =>
                            item.Id ==
                                id &&
                            item.UserId ==
                                userId
                    );

            if (notification == null)
            {
                return NotFound(
                    new
                    {
                        message =
                            "Notification not found."
                    }
                );
            }

            if (
                !notification.IsRead
            )
            {
                notification.IsRead =
                    true;

                await _context
                    .SaveChangesAsync();
            }

            return NoContent();
        }

        // =====================================================
        // MARK ALL READ
        // =====================================================

        [HttpPatch("read-all")]
        public async Task<IActionResult>
            MarkAllRead()
        {
            var userId =
                GetCurrentUserId();

            var unread =
                await _context.Notifications
                    .Where(
                        notification =>
                            notification.UserId ==
                                userId &&
                            !notification.IsRead
                    )
                    .ToListAsync();

            foreach (
                var notification
                in unread
            )
            {
                notification.IsRead =
                    true;
            }

            if (
                unread.Count >
                    0
            )
            {
                await _context
                    .SaveChangesAsync();
            }

            return Ok(
                new
                {
                    updated =
                        unread.Count
                }
            );
        }

        // =====================================================
        // PATIENT REMINDERS
        // =====================================================

        private async Task
            EnsurePatientRemindersAsync(
                Guid userId
            )
        {
            var patient =
                await _context.Patients
                    .AsNoTracking()
                    .Where(
                        item =>
                            item.UserId ==
                                userId &&
                            item.User.Role ==
                                RoleNames.Patient &&
                            item.User.IsActive
                    )
                    .Select(
                        item =>
                            new PatientReminderAccess
                            {
                                Id =
                                    item.Id,

                                UserId =
                                    item.UserId,

                                MedicationReminders =
                                    item.Preference ==
                                        null
                                        ? true
                                        : item
                                            .Preference
                                            .MedicationReminders,

                                AppointmentReminders =
                                    item.Preference ==
                                        null
                                        ? true
                                        : item
                                            .Preference
                                            .AppointmentReminders
                            }
                    )
                    .FirstOrDefaultAsync();

            if (patient == null)
            {
                return;
            }

            var nowUtc =
                DateTime.UtcNow;

            /*
             * Messages contain their relevant date/time.
             * Looking back seven days is enough to stop repeated
             * generation while allowing a new medication reminder
             * on the next day.
             */
            var recentMessageCutoff =
                nowUtc.AddDays(-7);

            var existingMessages =
                (
                    await _context
                        .Notifications
                        .AsNoTracking()
                        .Where(
                            notification =>
                                notification.UserId ==
                                    userId &&
                                notification.CreatedAt >=
                                    recentMessageCutoff
                        )
                        .Select(
                            notification =>
                                notification.Message
                        )
                        .ToListAsync()
                )
                .ToHashSet(
                    StringComparer.Ordinal
                );

            if (
                patient
                    .MedicationReminders
            )
            {
                await AddMedicationRemindersAsync(
                    patient,
                    existingMessages,
                    nowUtc
                );
            }

            if (
                patient
                    .AppointmentReminders
            )
            {
                await AddAppointmentNotificationsAsync(
                    patient,
                    existingMessages,
                    nowUtc
                );
            }

            if (
                _context
                    .ChangeTracker
                    .HasChanges()
            )
            {
                await _context
                    .SaveChangesAsync();
            }
        }

        // =====================================================
        // MEDICATION DOSE REMINDERS
        // =====================================================

        private async Task
            AddMedicationRemindersAsync(
                PatientReminderAccess patient,
                HashSet<string> existingMessages,
                DateTime nowUtc
            )
        {
            var localNow =
                TimeZoneInfo
                    .ConvertTimeFromUtc(
                        DateTime.SpecifyKind(
                            nowUtc,
                            DateTimeKind.Utc
                        ),
                        SouthAfricaTimeZone
                    );

            var recentLogCutoff =
                nowUtc.AddDays(-1);

            var medications =
                await _context.Medications
                    .AsNoTracking()
                    .AsSplitQuery()
                    .Include(
                        medication =>
                            medication.Schedules
                                .Where(
                                    schedule =>
                                        schedule.IsActive
                                )
                    )
                    .Include(
                        medication =>
                            medication.Logs
                                .Where(
                                    log =>
                                        log.TakenAt >=
                                            recentLogCutoff
                                )
                    )
                    .Where(
                        medication =>
                            medication.PatientId ==
                                patient.Id &&
                            medication.IsActive &&
                            medication.StartDate <=
                                nowUtc &&
                            (
                                medication.EndDate ==
                                    null ||
                                medication.EndDate >=
                                    nowUtc
                            )
                    )
                    .ToListAsync();

            foreach (
                var medication
                in medications
            )
            {
                foreach (
                    var schedule
                    in medication
                        .Schedules
                        .Where(
                            item =>
                                item.IsActive
                        )
                )
                {
                    var scheduledLocal =
                        DateTime.SpecifyKind(
                            localNow.Date +
                                schedule
                                    .TimeOfDay,
                            DateTimeKind.Unspecified
                        );

                    /*
                     * Generate the reminder from the scheduled
                     * time until 30 minutes afterwards.
                     *
                     * This gives the one-minute frontend polling
                     * enough room to catch the scheduled dose.
                     */
                    if (
                        localNow <
                            scheduledLocal ||
                        localNow >
                            scheduledLocal
                                .AddMinutes(30)
                    )
                    {
                        continue;
                    }

                    var scheduledUtc =
                        TimeZoneInfo
                            .ConvertTimeToUtc(
                                scheduledLocal,
                                SouthAfricaTimeZone
                            );

                    /*
                     * If the patient already recorded Taken or
                     * Skipped around this scheduled dose, there is
                     * no reason to remind them again.
                     */
                    var alreadyResponded =
                        medication.Logs.Any(
                            log =>
                                log.TakenAt >=
                                    scheduledUtc
                                        .AddMinutes(-45) &&
                                log.TakenAt <=
                                    scheduledUtc
                                        .AddMinutes(45)
                        );

                    if (
                        alreadyResponded
                    )
                    {
                        continue;
                    }

                    var medicationLabel =
                        string.Join(
                            " ",
                            new[]
                            {
                                medication.Name,
                                medication.Dosage
                            }
                            .Where(
                                value =>
                                    !string
                                        .IsNullOrWhiteSpace(
                                            value
                                        )
                            )
                        );

                    var message =
                        "Medication reminder: " +
                        $"{medicationLabel} is scheduled for " +
                        $"{scheduledLocal:HH:mm} on " +
                        $"{scheduledLocal:dd MMM yyyy}.";

                    AddNotificationIfNew(
                        patient.UserId,
                        message,
                        existingMessages,
                        nowUtc
                    );
                }
            }
        }

        // =====================================================
        // APPOINTMENT NOTIFICATIONS
        // =====================================================

        private async Task
            AddAppointmentNotificationsAsync(
                PatientReminderAccess patient,
                HashSet<string> existingMessages,
                DateTime nowUtc
            )
        {
            var changeCutoff =
                nowUtc.AddHours(-24);

            /*
             * Notify the patient when an appointment was newly
             * created or changed.
             *
             * This also captures staff-side rescheduling,
             * confirmation, cancellation and status changes.
             */
            var changedAppointments =
                await _context.Appointments
                    .AsNoTracking()
                    .Where(
                        appointment =>
                            appointment.PatientId ==
                                patient.Id &&
                            (
                                appointment.CreatedAt >=
                                    changeCutoff ||
                                (
                                    appointment.UpdatedAt !=
                                        null &&
                                    appointment.UpdatedAt >=
                                        changeCutoff
                                )
                            )
                    )
                    .Select(
                        appointment =>
                            new AppointmentReminderRow
                            {
                                Id =
                                    appointment.Id,

                                ScheduledAt =
                                    appointment.ScheduledAt,

                                Type =
                                    appointment.Type,

                                Status =
                                    appointment.Status,

                                ClinicName =
                                    appointment
                                        .Clinic
                                        .Name
                            }
                    )
                    .ToListAsync();

            foreach (
                var appointment
                in changedAppointments
            )
            {
                var localTime =
                    ToSouthAfricaTime(
                        appointment
                            .ScheduledAt
                    );

                var type =
                    string.IsNullOrWhiteSpace(
                        appointment.Type
                    )
                        ? "appointment"
                        : appointment.Type;

                var message =
                    "Appointment update: " +
                    $"Your {type} at " +
                    $"{appointment.ClinicName} is " +
                    $"{appointment.Status} for " +
                    $"{localTime:ddd, dd MMM yyyy 'at' HH:mm}.";

                AddNotificationIfNew(
                    patient.UserId,
                    message,
                    existingMessages,
                    nowUtc
                );
            }

            /*
             * Upcoming appointment reminders.
             *
             * One reminder is created within 24 hours and a
             * separate "soon" reminder inside 90 minutes.
             */
            var upcomingLimit =
                nowUtc.AddHours(24);

            var upcomingAppointments =
                await _context.Appointments
                    .AsNoTracking()
                    .Where(
                        appointment =>
                            appointment.PatientId ==
                                patient.Id &&
                            appointment.ScheduledAt >=
                                nowUtc &&
                            appointment.ScheduledAt <=
                                upcomingLimit &&
                            appointment.Status !=
                                AppointmentStatuses.Cancelled &&
                            appointment.Status !=
                                AppointmentStatuses.Completed &&
                            appointment.Status !=
                                AppointmentStatuses.Missed
                    )
                    .Select(
                        appointment =>
                            new AppointmentReminderRow
                            {
                                Id =
                                    appointment.Id,

                                ScheduledAt =
                                    appointment.ScheduledAt,

                                Type =
                                    appointment.Type,

                                Status =
                                    appointment.Status,

                                ClinicName =
                                    appointment
                                        .Clinic
                                        .Name
                            }
                    )
                    .ToListAsync();

            foreach (
                var appointment
                in upcomingAppointments
            )
            {
                var remaining =
                    appointment.ScheduledAt -
                    nowUtc;

                var localTime =
                    ToSouthAfricaTime(
                        appointment
                            .ScheduledAt
                    );

                var type =
                    string.IsNullOrWhiteSpace(
                        appointment.Type
                    )
                        ? "appointment"
                        : appointment.Type;

                string message;

                if (
                    remaining <=
                        TimeSpan
                            .FromMinutes(90)
                )
                {
                    message =
                        "Appointment reminder - soon: " +
                        $"Your {type} at " +
                        $"{appointment.ClinicName} starts at " +
                        $"{localTime:HH:mm} today.";
                }
                else
                {
                    message =
                        "Appointment reminder: " +
                        $"Your {type} at " +
                        $"{appointment.ClinicName} is scheduled for " +
                        $"{localTime:ddd, dd MMM yyyy 'at' HH:mm}.";
                }

                AddNotificationIfNew(
                    patient.UserId,
                    message,
                    existingMessages,
                    nowUtc
                );
            }
        }

        // =====================================================
        // ADD NOTIFICATION
        // =====================================================

        private void
            AddNotificationIfNew(
                Guid userId,
                string message,
                HashSet<string> existingMessages,
                DateTime createdAt
            )
        {
            if (
                existingMessages
                    .Contains(
                        message
                    )
            )
            {
                return;
            }

            _context.Notifications.Add(
                new Notification
                {
                    Id =
                        Guid.NewGuid(),

                    UserId =
                        userId,

                    Message =
                        message,

                    IsRead =
                        false,

                    CreatedAt =
                        createdAt
                }
            );

            existingMessages.Add(
                message
            );
        }

        // =====================================================
        // PROXY COLLECTION REMINDERS
        // =====================================================

        private async Task
            EnsureProxyCollectionRemindersAsync(
                Guid userId
            )
        {
            var proxy =
                await _context.Proxies
                    .AsNoTracking()
                    .Include(
                        proxyEntity =>
                            proxyEntity.User
                    )
                    .FirstOrDefaultAsync(
                        proxyEntity =>
                            proxyEntity.UserId ==
                                userId &&
                            proxyEntity.User.Role ==
                                RoleNames.Proxy &&
                            proxyEntity.User.IsActive
                    );

            if (proxy == null)
            {
                return;
            }

            var patientIds =
                await _context.ProxyLinks
                    .AsNoTracking()
                    .Where(
                        link =>
                            link.ProxyId ==
                                proxy.Id &&
                            link.IsActive
                    )
                    .Select(
                        link =>
                            link.PatientId
                    )
                    .Distinct()
                    .ToListAsync();

            if (
                patientIds.Count ==
                    0
            )
            {
                return;
            }

            var now =
                DateTime.UtcNow;

            var reminderCutoff =
                now.AddHours(48);

            var collections =
                await _context
                    .MedicationCollections
                    .AsNoTracking()
                    .Include(
                        collection =>
                            collection.Patient
                    )
                    .ThenInclude(
                        patient =>
                            patient.User
                    )
                    .Where(
                        collection =>
                            patientIds.Contains(
                                collection.PatientId
                            ) &&
                            collection.Status !=
                                MedicationCollectionStatuses
                                    .Collected &&
                            collection.Status !=
                                MedicationCollectionStatuses
                                    .Cancelled &&
                            collection
                                .ScheduledCollectionDate <=
                                    reminderCutoff
                    )
                    .OrderBy(
                        collection =>
                            collection
                                .ScheduledCollectionDate
                    )
                    .ToListAsync();

            if (
                collections.Count ==
                    0
            )
            {
                return;
            }

            var existingMessages =
                await _context.Notifications
                    .AsNoTracking()
                    .Where(
                        notification =>
                            notification.UserId ==
                                userId
                    )
                    .Select(
                        notification =>
                            notification.Message
                    )
                    .ToListAsync();

            var existing =
                existingMessages
                    .ToHashSet(
                        StringComparer.Ordinal
                    );

            foreach (
                var collection
                in collections
            )
            {
                var date =
                    collection
                        .ScheduledCollectionDate
                        .ToString(
                            "dd MMM yyyy"
                        );

                var message =
                    collection
                        .ScheduledCollectionDate <
                        now
                        ? $"{collection.Patient.User.FullName}'s medication collection is overdue. It was due on {date}."
                        : $"{collection.Patient.User.FullName}'s medication collection is due on {date}.";

                if (
                    existing.Contains(
                        message
                    )
                )
                {
                    continue;
                }

                _context.Notifications.Add(
                    new Notification
                    {
                        Id =
                            Guid.NewGuid(),

                        UserId =
                            userId,

                        Message =
                            message,

                        IsRead =
                            false,

                        CreatedAt =
                            DateTime.UtcNow
                    }
                );

                existing.Add(
                    message
                );
            }

            if (
                _context
                    .ChangeTracker
                    .HasChanges()
            )
            {
                await _context
                    .SaveChangesAsync();
            }
        }

        // =====================================================
        // TIMEZONE
        // =====================================================

        private static TimeZoneInfo
            ResolveSouthAfricaTimeZone()
        {
            try
            {
                return TimeZoneInfo
                    .FindSystemTimeZoneById(
                        "Africa/Johannesburg"
                    );
            }
            catch (
                TimeZoneNotFoundException
            )
            {
                try
                {
                    return TimeZoneInfo
                        .FindSystemTimeZoneById(
                            "South Africa Standard Time"
                        );
                }
                catch
                {
                    return TimeZoneInfo
                        .CreateCustomTimeZone(
                            "PhilaLink-SAST",
                            TimeSpan
                                .FromHours(2),
                            "South Africa Standard Time",
                            "South Africa Standard Time"
                        );
                }
            }
        }

        private static DateTime
            ToSouthAfricaTime(
                DateTime value
            )
        {
            var utc =
                value.Kind ==
                    DateTimeKind.Utc
                    ? value
                    : DateTime
                        .SpecifyKind(
                            value,
                            DateTimeKind.Utc
                        );

            return TimeZoneInfo
                .ConvertTimeFromUtc(
                    utc,
                    SouthAfricaTimeZone
                );
        }

        // =====================================================
        // CURRENT USER
        // =====================================================

        private Guid
            GetCurrentUserId()
        {
            var value =
                User.FindFirstValue(
                    ClaimTypes
                        .NameIdentifier
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
                throw new
                    UnauthorizedAccessException();
            }

            return userId;
        }

        // =====================================================
        // INTERNAL MODELS
        // =====================================================

        private sealed class
            PatientReminderAccess
        {
            public Guid Id
            {
                get;
                init;
            }

            public Guid UserId
            {
                get;
                init;
            }

            public bool MedicationReminders
            {
                get;
                init;
            }

            public bool AppointmentReminders
            {
                get;
                init;
            }
        }

        private sealed class
            AppointmentReminderRow
        {
            public Guid Id
            {
                get;
                init;
            }

            public DateTime ScheduledAt
            {
                get;
                init;
            }

            public string Type
            {
                get;
                init;
            } = string.Empty;

            public string Status
            {
                get;
                init;
            } = string.Empty;

            public string ClinicName
            {
                get;
                init;
            } = string.Empty;
        }
    }
}
