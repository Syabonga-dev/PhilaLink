using Microsoft.EntityFrameworkCore;
using PersonalProject.Data;
using PersonalProject.Models.Constants;
using PersonalProject.Models.DTOs;
using PersonalProject.Models.Entities;
using PersonalProject.Services.Interfaces;

namespace PersonalProject.Services.Implementations
{
    public class AppointmentService : IAppointmentService
    {
        private readonly PhilaLinkDbContext _context;
        private readonly IAuditLogService _audit;

        public AppointmentService(
            PhilaLinkDbContext context,
            IAuditLogService audit
        )
        {
            _context = context;
            _audit = audit;
        }

        public async Task<AppointmentResponseDto> CreateAsync(
            CreateAppointmentDto dto,
            Guid performedByUserId
        )
        {
            var clinicId =
                await GetStaffClinicIdAsync(
                    performedByUserId
                );

            if (dto.ClinicId != clinicId)
            {
                throw new UnauthorizedAccessException(
                    "You can only create appointments for your clinic."
                );
            }

            if (dto.ScheduledAt <= DateTime.UtcNow)
            {
                throw new InvalidOperationException(
                    "Appointment date must be in the future."
                );
            }

            if (dto.DurationMinutes <= 0)
            {
                throw new InvalidOperationException(
                    "Appointment duration must be greater than zero."
                );
            }

            var patient = await _context.Patients
                .Include(p => p.User)
                .FirstOrDefaultAsync(
                    p => p.Id == dto.PatientId
                );

            if (patient == null)
            {
                throw new KeyNotFoundException(
                    "Patient not found."
                );
            }

            if (patient.ClinicId != clinicId)
            {
                throw new UnauthorizedAccessException(
                    "The patient does not belong to your clinic."
                );
            }

            await ValidateNurseAsync(
                dto.NurseId,
                clinicId
            );

            var appointment = new Appointment
            {
                Id = Guid.NewGuid(),

                PatientId = dto.PatientId,

                ClinicId = clinicId,

                NurseId = dto.NurseId,

                ScheduledAt = dto.ScheduledAt,

                DurationMinutes = dto.DurationMinutes,

                Type = dto.Type.Trim(),

                Reason = dto.Reason.Trim(),

                ProviderName =
                    string.IsNullOrWhiteSpace(
                        dto.ProviderName
                    )
                        ? null
                        : dto.ProviderName.Trim(),

                Mode = AppointmentModes.Normalize(dto.Mode),

                Status = AppointmentStatuses.Scheduled,

                Notes = dto.Notes,

                CreatedAt = DateTime.UtcNow
            };

            _context.Appointments.Add(appointment);

            await _context.SaveChangesAsync();

            await _audit.LogAsync(
                "AppointmentCreated",
                performedByUserId,
                $"Appointment {appointment.Id} created " +
                $"for patient {patient.Id}."
            );

            return (
                await GetAppointmentQuery()
                    .FirstAsync(
                        a => a.Id == appointment.Id
                    )
            ).ToDto();
        }

        public async Task<AppointmentResponseDto> UpdateAsync(
            Guid appointmentId,
            UpdateAppointmentDto dto,
            Guid performedByUserId
        )
        {
            var clinicId = await GetStaffClinicIdAsync(performedByUserId);

            var appointment = await _context.Appointments.FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null)
            {
                throw new KeyNotFoundException(
                    "Appointment not found."
                );
            }

            if (appointment.ClinicId != clinicId)
            {
                throw new UnauthorizedAccessException(
                    "You cannot manage appointments from another clinic."
                );
            }

            if (dto.DurationMinutes <= 0)
            {
                throw new InvalidOperationException(
                    "Appointment duration must be greater than zero."
                );
            }

            await ValidateNurseAsync(
                dto.NurseId,
                clinicId
            );

            appointment.ScheduledAt = dto.ScheduledAt;
            appointment.DurationMinutes = dto.DurationMinutes;
            appointment.NurseId = dto.NurseId;
            appointment.Type = dto.Type.Trim();
            appointment.Reason = dto.Reason.Trim();
            appointment.ProviderName = string.IsNullOrWhiteSpace(dto.ProviderName) ? null : dto.ProviderName.Trim();
            appointment.Mode = NormalizeMode(dto.Mode);
            appointment.Status = AppointmentStatuses.Normalize(dto.Status);
            appointment.Notes = dto.Notes;
            appointment.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _audit.LogAsync(
                "AppointmentUpdated",
                performedByUserId,
                $"Appointment {appointment.Id} updated."
            );

            return (
                await GetAppointmentQuery()
                    .FirstAsync(
                        a => a.Id == appointment.Id
                    )
            ).ToDto();
        }

        public async Task<AppointmentResponseDto?>
            GetByIdAsync(
                Guid appointmentId,
                Guid performedByUserId
            )
        {
            var clinicId = await GetStaffClinicIdAsync(performedByUserId);

            var appointment = await GetAppointmentQuery().FirstOrDefaultAsync(a => a.Id == appointmentId && a.ClinicId == clinicId);

            return appointment?.ToDto();
        }

        public async Task<List<AppointmentResponseDto>>
            GetClinicAppointmentsAsync(
                Guid performedByUserId
            )
        {
            var clinicId = await GetStaffClinicIdAsync(performedByUserId);

            var appointments = await GetAppointmentQuery().Where(a => a.ClinicId == clinicId).OrderBy(a => a.ScheduledAt).ToListAsync();

            return appointments
                .Select(a => a.ToDto())
                .ToList();
        }

        private IQueryable<Appointment>
            GetAppointmentQuery()
        {
            return _context.Appointments
                .Include(a => a.Patient)
                    .ThenInclude(p => p.User)
                .Include(a => a.Clinic)
                .Include(a => a.Nurse)
                    .ThenInclude(n => n!.User);
        }

        private async Task<Guid>
            GetStaffClinicIdAsync(
                Guid userId
            )
        {
            var user = await _context.Users
                .Include(u => u.Admin)
                .Include(u => u.Nurse)
                .FirstOrDefaultAsync(
                    u => u.Id == userId
                );

            if (user == null || !user.IsActive)
            {
                throw new UnauthorizedAccessException(
                    "Active staff account required."
                );
            }

            if (
                user.Role == RoleNames.ClinicAdmin &&
                user.Admin?.ClinicId != null
            )
            {
                return user.Admin.ClinicId.Value;
            }

            if (
                user.Role == RoleNames.Nurse &&
                user.Nurse != null
            )
            {
                return user.Nurse.ClinicId;
            }

            throw new UnauthorizedAccessException(
                "Clinic staff privileges are required."
            );
        }

        private async Task ValidateNurseAsync(
            Guid? nurseId,
            Guid clinicId
        )
        {
            if (nurseId == null)
                return;

            var nurse = await _context.Nurses
                .Include(n => n.User)
                .FirstOrDefaultAsync(
                    n => n.Id == nurseId
                );

            if (nurse == null)
            {
                throw new KeyNotFoundException(
                    "Assigned nurse not found."
                );
            }

            if (
                nurse.ClinicId != clinicId ||
                !nurse.User.IsActive
            )
            {
                throw new InvalidOperationException(
                    "Assigned nurse must be an active nurse at the appointment clinic."
                );
            }
        }

        private static string NormalizeMode(
            string mode
        )
        {
            if (
                string.Equals(
                    mode,
                    "Telehealth",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return "Telehealth";
            }

            return "InPerson";
        }

    }

    internal static class AppointmentMappings
    {
        public static AppointmentResponseDto ToDto(
            this Appointment appointment
        )
        {
            return new AppointmentResponseDto
            {
                Id = appointment.Id,
                PatientId = appointment.PatientId,
                PatientName = appointment.Patient.User.FullName,
                ClinicId = appointment.ClinicId,
                ClinicName = appointment.Clinic.Name,
                NurseId = appointment.NurseId,
                NurseName = appointment.Nurse?.User.FullName,
                ScheduledAt = appointment.ScheduledAt,
                DurationMinutes = appointment.DurationMinutes,
                Type = appointment.Type,
                Reason = appointment.Reason,
                ProviderName = appointment.ProviderName,
                Mode = appointment.Mode,
                Status = appointment.Status,
                Notes = appointment.Notes
            };
        }
    }
}