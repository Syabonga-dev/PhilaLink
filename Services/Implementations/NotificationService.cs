using Microsoft.EntityFrameworkCore;
using PersonalProject.Data;
using PersonalProject.Models.Constants;
using PersonalProject.Models.Entities;
using PersonalProject.Services.Interfaces;

namespace PersonalProject.Services.Implementations
{
    public class NotificationService :
        INotificationService
    {
        private readonly PhilaLinkDbContext
            _context;

        public NotificationService(
            PhilaLinkDbContext context
        )
        {
            _context =
                context;
        }

        // =====================================================
        // CLINIC NOTIFICATION
        // Controlled by ClinicNotifications
        // =====================================================

        public async Task<bool>
            CreateForPatientAsync(
                Guid patientUserId,
                string message,
                Guid performedByUserId
            )
        {
            ValidateMessage(
                message
            );

            var staff =
                await _context.Users
                    .AsNoTracking()
                    .Where(
                        user =>
                            user.Id ==
                                performedByUserId &&
                            user.IsActive
                    )
                    .Select(
                        user =>
                            new StaffNotificationAccess
                            {
                                Role =
                                    user.Role,

                                AdminClinicId =
                                    user.Admin ==
                                        null
                                        ? null
                                        : user
                                            .Admin
                                            .ClinicId,

                                NurseClinicId =
                                    user.Nurse ==
                                        null
                                        ? null
                                        : (Guid?)user
                                            .Nurse
                                            .ClinicId
                            }
                    )
                    .FirstOrDefaultAsync();

            if (staff == null)
            {
                throw new UnauthorizedAccessException(
                    "Active staff account required."
                );
            }

            Guid? staffClinicId =
                null;

            if (
                staff.Role ==
                    RoleNames.ClinicAdmin &&
                staff.AdminClinicId !=
                    null
            )
            {
                staffClinicId =
                    staff
                        .AdminClinicId
                        .Value;
            }
            else if (
                staff.Role ==
                    RoleNames.Nurse &&
                staff.NurseClinicId !=
                    null
            )
            {
                staffClinicId =
                    staff
                        .NurseClinicId
                        .Value;
            }
            else
            {
                throw new UnauthorizedAccessException(
                    "Clinic staff privileges are required."
                );
            }

            var patient =
                await _context.Patients
                    .AsNoTracking()
                    .Where(
                        item =>
                            item.UserId ==
                                patientUserId &&
                            item.User.Role ==
                                RoleNames.Patient &&
                            item.User.IsActive
                    )
                    .Select(
                        item =>
                            new PatientNotificationAccess
                            {
                                UserId =
                                    item.UserId,

                                ClinicId =
                                    item.ClinicId,

                                ClinicNotifications =
                                    item.Preference ==
                                        null
                                        ? true
                                        : item
                                            .Preference
                                            .ClinicNotifications,

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
                throw new KeyNotFoundException(
                    "Active patient account not found."
                );
            }

            if (
                patient.ClinicId ==
                    null ||
                patient.ClinicId !=
                    staffClinicId
            )
            {
                throw new UnauthorizedAccessException(
                    "Patient does not belong to your clinic."
                );
            }

            if (
                !patient
                    .ClinicNotifications
            )
            {
                return false;
            }

            return await
                AddNotificationAsync(
                    patient.UserId,
                    message,
                    duplicateWindow:
                        TimeSpan
                            .FromMinutes(2)
                );
        }

        // =====================================================
        // APPOINTMENT NOTIFICATION
        // Controlled by AppointmentReminders
        // =====================================================

        public async Task<bool>
            CreateAppointmentForPatientAsync(
                Guid patientUserId,
                string message
            )
        {
            ValidateMessage(
                message
            );

            var patient =
                await _context.Patients
                    .AsNoTracking()
                    .Where(
                        item =>
                            item.UserId ==
                                patientUserId &&
                            item.User.Role ==
                                RoleNames.Patient &&
                            item.User.IsActive
                    )
                    .Select(
                        item =>
                            new PatientNotificationAccess
                            {
                                UserId =
                                    item.UserId,

                                ClinicId =
                                    item.ClinicId,

                                ClinicNotifications =
                                    item.Preference ==
                                        null
                                        ? true
                                        : item
                                            .Preference
                                            .ClinicNotifications,

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
                throw new KeyNotFoundException(
                    "Active patient account not found."
                );
            }

            /*
             * Appointment creation, approval/confirmation,
             * rescheduling, cancellation, deletion and upcoming
             * reminders all respect AppointmentReminders.
             */
            if (
                !patient
                    .AppointmentReminders
            )
            {
                return false;
            }

            return await
                AddNotificationAsync(
                    patient.UserId,
                    message,
                    duplicateWindow:
                        TimeSpan
                            .FromMinutes(2)
                );
        }

        // =====================================================
        // GENERAL SYSTEM NOTIFICATION
        // =====================================================

        public async Task<bool>
            CreateSystemForPatientAsync(
                Guid patientUserId,
                string message
            )
        {
            ValidateMessage(
                message
            );

            var patientExists =
                await _context.Patients
                    .AsNoTracking()
                    .AnyAsync(
                        patient =>
                            patient.UserId ==
                                patientUserId &&
                            patient.User.Role ==
                                RoleNames.Patient &&
                            patient.User.IsActive
                    );

            if (!patientExists)
            {
                throw new KeyNotFoundException(
                    "Active patient account not found."
                );
            }

            /*
             * Preserve the existing system-notification
             * duplicate protection.
             */
            return await
                AddNotificationAsync(
                    patientUserId,
                    message,
                    duplicateWindow:
                        TimeSpan
                            .FromHours(12)
                );
        }

        // =====================================================
        // ADD NOTIFICATION
        // =====================================================

        private async Task<bool>
            AddNotificationAsync(
                Guid userId,
                string message,
                TimeSpan?
                    duplicateWindow
            )
        {
            var trimmedMessage =
                message.Trim();

            if (
                duplicateWindow !=
                    null
            )
            {
                var cutoff =
                    DateTime.UtcNow -
                    duplicateWindow.Value;

                var duplicateExists =
                    await _context
                        .Notifications
                        .AsNoTracking()
                        .AnyAsync(
                            notification =>
                                notification.UserId ==
                                    userId &&
                                notification.Message ==
                                    trimmedMessage &&
                                notification.CreatedAt >=
                                    cutoff
                        );

                if (
                    duplicateExists
                )
                {
                    return false;
                }
            }

            _context.Notifications.Add(
                new Notification
                {
                    Id =
                        Guid.NewGuid(),

                    UserId =
                        userId,

                    Message =
                        trimmedMessage,

                    IsRead =
                        false,

                    CreatedAt =
                        DateTime.UtcNow
                }
            );

            await _context
                .SaveChangesAsync();

            return true;
        }

        // =====================================================
        // VALIDATION
        // =====================================================

        private static void
            ValidateMessage(
                string message
            )
        {
            if (
                string.IsNullOrWhiteSpace(
                    message
                )
            )
            {
                throw new InvalidOperationException(
                    "Notification message is required."
                );
            }
        }

        // =====================================================
        // INTERNAL MODELS
        // =====================================================

        private sealed class
            StaffNotificationAccess
        {
            public string Role
            {
                get;
                init;
            } = string.Empty;

            public Guid? AdminClinicId
            {
                get;
                init;
            }

            public Guid? NurseClinicId
            {
                get;
                init;
            }
        }

        private sealed class
            PatientNotificationAccess
        {
            public Guid UserId
            {
                get;
                init;
            }

            public Guid? ClinicId
            {
                get;
                init;
            }

            public bool ClinicNotifications
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
    }
}
