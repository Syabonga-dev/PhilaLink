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
        private readonly PhilaLinkDbContext _context;

        public NotificationService(
            PhilaLinkDbContext context
        )
        {
            _context = context;
        }

        public async Task CreateForPatientAsync(
            Guid patientUserId,
            string message,
            Guid performedByUserId
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

            /*
             * Only retrieve the staff information required
             * for authorization.
             *
             * Do not load complete User/Admin/Nurse graphs.
             */
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
                                    user.Admin == null
                                        ? null
                                        : user
                                            .Admin
                                            .ClinicId,

                                NurseClinicId =
                                    user.Nurse == null
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
                    staff.AdminClinicId.Value;
            }
            else if (
                staff.Role ==
                    RoleNames.Nurse &&
                staff.NurseClinicId !=
                    null
            )
            {
                staffClinicId =
                    staff.NurseClinicId.Value;
            }
            else
            {
                throw new UnauthorizedAccessException(
                    "Clinic staff privileges are required."
                );
            }

            /*
             * Only the patient's user ID and clinic ID
             * are required here.
             *
             * This preserves the existing active-patient
             * and same-clinic validation.
             */
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
                                    item.ClinicId
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

            var notification =
                new Notification
                {
                    Id =
                        Guid.NewGuid(),

                    UserId =
                        patient.UserId,

                    Message =
                        message.Trim(),

                    IsRead =
                        false,

                    CreatedAt =
                        DateTime.UtcNow
                };

            _context.Notifications.Add(
                notification
            );

            await _context.SaveChangesAsync();
        }

        public async Task<bool>
            CreateSystemForPatientAsync(
                Guid patientUserId,
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

            /*
             * AnyAsync generates an EXISTS query.
             * No Include is required.
             */
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

            var trimmedMessage =
                message.Trim();

            var duplicateCutoff =
                DateTime.UtcNow
                    .AddHours(-12);

            var duplicateExists =
                await _context.Notifications
                    .AsNoTracking()
                    .AnyAsync(
                        notification =>
                            notification.UserId ==
                                patientUserId &&
                            notification.Message ==
                                trimmedMessage &&
                            notification.CreatedAt >=
                                duplicateCutoff
                    );

            if (duplicateExists)
            {
                return false;
            }

            var notification =
                new Notification
                {
                    Id =
                        Guid.NewGuid(),

                    UserId =
                        patientUserId,

                    Message =
                        trimmedMessage,

                    IsRead =
                        false,

                    CreatedAt =
                        DateTime.UtcNow
                };

            _context.Notifications.Add(
                notification
            );

            await _context.SaveChangesAsync();

            return true;
        }

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
        }
    }
}