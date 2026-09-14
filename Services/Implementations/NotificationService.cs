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
            if (string.IsNullOrWhiteSpace(message))
            {
                throw new InvalidOperationException(
                    "Notification message is required."
                );
            }

            var staffUser =
                await _context.Users
                    .Include(u => u.Admin)
                    .Include(u => u.Nurse)
                    .FirstOrDefaultAsync(
                        u =>
                            u.Id == performedByUserId &&
                            u.IsActive
                    );

            if (staffUser == null)
            {
                throw new UnauthorizedAccessException(
                    "Active staff account required."
                );
            }

            Guid? staffClinicId = null;

            if (
                staffUser.Role ==
                    RoleNames.ClinicAdmin &&
                staffUser.Admin?.ClinicId != null
            )
            {
                staffClinicId =
                    staffUser.Admin.ClinicId.Value;
            }
            else if (
                staffUser.Role ==
                    RoleNames.Nurse &&
                staffUser.Nurse != null
            )
            {
                staffClinicId =
                    staffUser.Nurse.ClinicId;
            }
            else
            {
                throw new UnauthorizedAccessException(
                    "Clinic staff privileges are required."
                );
            }

            var patient =
                await _context.Patients
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(
                        p =>
                            p.UserId ==
                                patientUserId &&
                            p.User.Role ==
                                RoleNames.Patient &&
                            p.User.IsActive
                    );

            if (patient == null)
            {
                throw new KeyNotFoundException(
                    "Active patient account not found."
                );
            }

            if (
                patient.ClinicId == null ||
                patient.ClinicId != staffClinicId
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
    }
}