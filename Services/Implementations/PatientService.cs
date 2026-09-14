using Microsoft.EntityFrameworkCore;
using PersonalProject.Data;
using PersonalProject.Models.Constants;
using PersonalProject.Models.DTOs;
using PersonalProject.Models.Entities;
using PersonalProject.Services.Interfaces;

namespace PersonalProject.Services.Implementations
{
    public class PatientService : IPatientService
    {
        private readonly PhilaLinkDbContext _context;
        private readonly IAuditLogService _audit;
        private readonly IMedicationService _medicationService;

        public PatientService(PhilaLinkDbContext context,IAuditLogService audit,IMedicationService medicationService)
        {
            _context = context;
            _audit = audit;
            _medicationService = medicationService;
        }

        public async Task LogMedicationAsync( Guid userId,Guid medicationId,bool taken,string? notes)
        {
            await _medicationService.LogPatientMedicationAsync(userId,medicationId,taken,notes);
        }

        // =====================================================
        // PROFILE
        // =====================================================

        public async Task<PatientMeDto> GetMeAsync(
            Guid userId
        )
        {
            var patient =
                await GetPatientByUserIdAsync(
                    userId
                );

            return ToMeDto(patient);
        }

        public async Task<PatientMeDto> UpdateMeAsync(
            Guid userId,
            UpdatePatientProfileDto dto
        )
        {
            var patient =
                await GetPatientByUserIdAsync(
                    userId
                );

            if (
                string.IsNullOrWhiteSpace(
                    dto.FullName
                )
            )
            {
                throw new InvalidOperationException(
                    "Full name is required."
                );
            }

            if (
                string.IsNullOrWhiteSpace(
                    dto.PhoneNumber
                )
            )
            {
                throw new InvalidOperationException(
                    "Phone number is required."
                );
            }

            var duplicatePhone =
                await _context.Users.AnyAsync(
                    u =>
                        u.Id != userId &&
                        u.PhoneNumber ==
                            dto.PhoneNumber.Trim()
                );

            if (duplicatePhone)
            {
                throw new InvalidOperationException(
                    "That phone number is already in use."
                );
            }

            patient.User.FullName = dto.FullName.Trim();
            patient.User.PhoneNumber = dto.PhoneNumber.Trim();
            patient.User.Email = dto.Email.Trim();
            patient.User.UpdatedAt = DateTime.UtcNow;
            patient.DateOfBirth = dto.DateOfBirth;
            patient.Gender = dto.Gender.Trim();
            patient.Email = dto.Email.Trim();
            patient.AddressLine1 = dto.AddressLine1.Trim();
            patient.AddressLine2 = string.IsNullOrWhiteSpace(dto.AddressLine2) ? null : dto.AddressLine2.Trim();
            patient.Suburb = dto.Suburb.Trim();
            patient.City = dto.City.Trim();
            patient.Province = dto.Province.Trim();
            patient.PostalCode = dto.PostalCode.Trim();
            patient.EmergencyContactName = dto.EmergencyContactName.Trim();
            patient.EmergencyContactPhone = dto.EmergencyContactPhone.Trim();
            patient.EmergencyContactRelationship = dto.EmergencyContactRelationship.Trim();
            patient.IsProfileComplete = IsProfileComplete(patient);

            if (patient.IsProfileComplete && patient.ProfileCompletedAt == null)
            {
                patient.ProfileCompletedAt = DateTime.UtcNow;
            }

            patient.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _audit.LogAsync(
                "PatientProfileUpdated",
                userId,
                $"Patient {patient.Id} updated their profile."
            );

            return ToMeDto(patient);
        }

        // =====================================================
        // DASHBOARD
        // =====================================================

        public async Task<PatientDashboardDto>
            GetDashboardAsync(
                Guid userId
            )
        {
            var patient =
                await GetPatientByUserIdAsync(
                    userId
                );

            var medications =
                await GetMedicationsForPatientAsync(
                    patient.Id
                );

            var appointments =
                await GetAppointmentsForPatientAsync(
                    patient.Id
                );

            var latestMetrics =
                await _context.HealthMetrics
                    .Where(
                        m =>
                            m.PatientId ==
                            patient.Id
                    )
                    .OrderByDescending(
                        m => m.RecordedAt
                    )
                    .ToListAsync();

            var metrics =
                latestMetrics
                    .GroupBy(
                        m => m.MetricType
                    )
                    .Select(
                        g => g.First()
                    )
                    .Select(ToHealthMetricDto)
                    .ToList();

            var unread =
                await _context.Notifications
                    .CountAsync(
                        n =>
                            n.UserId ==
                                userId &&
                            !n.IsRead
                    );

            var nextCollection =
                await GetNextCollectionForPatientAsync(
                    patient.Id
                );

            return new PatientDashboardDto
            {
                FullName =
                    patient.User.FullName,

                PatientNumber =
                    patient.PatientNumber,

                IsProfileComplete =
                    patient.IsProfileComplete,

                Clinic =
                    patient.Clinic == null
                        ? null
                        : new PatientClinicSummaryDto
                        {
                            Id =
                                patient.Clinic.Id,

                            Name =
                                patient.Clinic.Name,

                            Address =
                                patient.Clinic.Address,

                            ContactNumber =
                                patient.Clinic
                                    .ContactNumber,

                            OpeningTime =
                                patient.Clinic
                                    .OpeningTime,

                            ClosingTime =
                                patient.Clinic
                                    .ClosingTime
                        },

                Medications =
                    medications
                        .Take(3)
                        .ToList(),

                UpcomingAppointments =
                    appointments
                        .Where(
                            a =>
                                a.ScheduledAt >=
                                    DateTime.UtcNow &&
                                a.Status !=
                                    "Cancelled" &&
                                a.Status !=
                                    "Completed"
                        )
                        .OrderBy(
                            a => a.ScheduledAt
                        )
                        .Take(3)
                        .ToList(),

                HealthMetrics =
                    metrics,

                UnreadNotifications =
                    unread,

                NextCollection =
                    nextCollection
            };
        }

        // =====================================================
        // MEDICATIONS
        // =====================================================

        public async Task<List<PatientMedicationDto>>
            GetMedicationsAsync(
                Guid userId
            )
        {
            var patient =
                await GetPatientByUserIdAsync(
                    userId
                );

            return await
                GetMedicationsForPatientAsync(
                    patient.Id
                );
        }

        private async Task<List<PatientMedicationDto>>
            GetMedicationsForPatientAsync(
                Guid patientId
            )
        {
            var medications =
                await _context.Medications
                    .Include(m => m.Schedules)
                    .Where(
                        m =>
                            m.PatientId ==
                                patientId &&
                            m.IsActive
                    )
                    .OrderBy(m => m.Name)
                    .ToListAsync();

            return medications
                .Select(m =>
                    new PatientMedicationDto
                    {
                        Id =
                            m.Id,

                        Name =
                            m.Name,

                        Dosage =
                            m.Dosage,

                        Form =
                            m.Form,

                        Instructions =
                            m.Instructions,

                        PrescribedBy =
                            m.PrescribedBy,

                        ConditionName =
                            m.ConditionName,

                        StartDate =
                            m.StartDate,

                        EndDate =
                            m.EndDate,

                        IsActive =
                            m.IsActive,

                        ScheduleTimes =
                            m.Schedules
                                .Where(
                                    s => s.IsActive
                                )
                                .OrderBy(
                                    s => s.TimeOfDay
                                )
                                .Select(
                                    s =>
                                        s.TimeOfDay
                                            .ToString(
                                                @"hh\:mm"
                                            )
                                )
                                .ToList(),

                        NextDoseAt =
                            CalculateNextDose(
                                m.Schedules
                            ),

                        DaysRemaining =
                            m.EndDate == null
                                ? null
                                : Math.Max(
                                    0,
                                    (
                                        m.EndDate.Value.Date -
                                        DateTime.UtcNow.Date
                                    ).Days
                                )
                    }
                )
                .ToList();
        }

        // =====================================================
        // APPOINTMENTS
        // =====================================================

        public async Task<List<AppointmentResponseDto>>
            GetAppointmentsAsync(
                Guid userId
            )
        {
            var patient =
                await GetPatientByUserIdAsync(
                    userId
                );

            return await
                GetAppointmentsForPatientAsync(
                    patient.Id
                );
        }

        public async Task<AppointmentResponseDto>
            BookAppointmentAsync(
                Guid userId,
                PatientBookAppointmentDto dto
            )
        {
            var patient =
                await GetPatientByUserIdAsync(
                    userId
                );

            if (patient.ClinicId == null)
            {
                throw new InvalidOperationException(
                    "A clinic must be assigned before booking an appointment."
                );
            }

            if (
                dto.ScheduledAt <=
                DateTime.UtcNow
            )
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

            var appointment =
                new Appointment
                {
                    Id = Guid.NewGuid(),

                    PatientId =
                        patient.Id,

                    ClinicId =
                        patient.ClinicId.Value,

                    ScheduledAt =
                        dto.ScheduledAt,

                    DurationMinutes =
                        dto.DurationMinutes,

                    Type =
                        dto.Type.Trim(),

                    Reason =
                        dto.Reason.Trim(),

                    Mode =
                        NormalizeAppointmentMode(
                            dto.Mode
                        ),

                    Status =
                        "Pending",

                    Notes =
                        dto.Notes,

                    CreatedAt =
                        DateTime.UtcNow
                };

            _context.Appointments.Add(
                appointment
            );

            await _context.SaveChangesAsync();

            await _audit.LogAsync(
                "PatientAppointmentRequested",
                userId,
                $"Patient {patient.Id} requested appointment {appointment.Id}."
            );

            return await
                GetAppointmentDtoAsync(
                    appointment.Id,
                    patient.Id
                );
        }

        public async Task<AppointmentResponseDto>
            RescheduleAppointmentAsync(
                Guid userId,
                Guid appointmentId,
                PatientRescheduleAppointmentDto dto
            )
        {
            var patient =
                await GetPatientByUserIdAsync(
                    userId
                );

            if (
                dto.ScheduledAt <=
                DateTime.UtcNow
            )
            {
                throw new InvalidOperationException(
                    "Appointment date must be in the future."
                );
            }

            var appointment =
                await _context.Appointments
                    .FirstOrDefaultAsync(
                        a =>
                            a.Id ==
                                appointmentId &&
                            a.PatientId ==
                                patient.Id
                    );

            if (appointment == null)
            {
                throw new KeyNotFoundException(
                    "Appointment not found."
                );
            }

            if (
                appointment.Status ==
                    "Completed" ||
                appointment.Status ==
                    "Cancelled"
            )
            {
                throw new InvalidOperationException(
                    "This appointment cannot be rescheduled."
                );
            }

            appointment.ScheduledAt = dto.ScheduledAt;
            appointment.Status = "Rescheduled";
            appointment.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _audit.LogAsync(
                "PatientAppointmentRescheduled",
                userId,
                $"Appointment {appointment.Id} rescheduled."
            );

            return await
                GetAppointmentDtoAsync(
                    appointment.Id,
                    patient.Id
                );
        }

        public async Task CancelAppointmentAsync(
            Guid userId,
            Guid appointmentId
        )
        {
            var patient =
                await GetPatientByUserIdAsync(
                    userId
                );

            var appointment =
                await _context.Appointments
                    .FirstOrDefaultAsync(
                        a =>
                            a.Id ==
                                appointmentId &&
                            a.PatientId ==
                                patient.Id
                    );

            if (appointment == null)
            {
                throw new KeyNotFoundException(
                    "Appointment not found."
                );
            }

            if (
                appointment.Status ==
                "Completed"
            )
            {
                throw new InvalidOperationException(
                    "A completed appointment cannot be cancelled."
                );
            }

            appointment.Status =
                "Cancelled";

            appointment.UpdatedAt =
                DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _audit.LogAsync(
                "PatientAppointmentCancelled",
                userId,
                $"Appointment {appointment.Id} cancelled."
            );
        }

        private async Task<List<AppointmentResponseDto>>
            GetAppointmentsForPatientAsync(
                Guid patientId
            )
        {
            var appointments =
                await _context.Appointments
                    .Include(a => a.Patient)
                        .ThenInclude(p => p.User)
                    .Include(a => a.Clinic)
                    .Include(a => a.Nurse)
                        .ThenInclude(n => n!.User)
                    .Where(
                        a =>
                            a.PatientId ==
                            patientId
                    )
                    .OrderByDescending(
                        a => a.ScheduledAt
                    )
                    .ToListAsync();

            return appointments
                .Select(ToAppointmentDto)
                .ToList();
        }

        // =====================================================
        // HEALTH RECORDS
        // =====================================================

        public async Task<List<HealthRecordResponseDto>>
            GetRecordsAsync(
                Guid userId
            )
        {
            var patient =
                await GetPatientByUserIdAsync(
                    userId
                );

            return await _context.HealthRecords
                .Include(r => r.Clinic)
                .Where(
                    r =>
                        r.PatientId ==
                        patient.Id
                )
                .OrderByDescending(
                    r => r.RecordDate
                )
                .Select(r =>
                    new HealthRecordResponseDto
                    {
                        Id =
                            r.Id,

                        Title =
                            r.Title,

                        Type =
                            r.Type,

                        Category =
                            r.Category,

                        ProviderName =
                            r.ProviderName,

                        Facility =
                            r.Clinic == null
                                ? null
                                : r.Clinic.Name,

                        Summary =
                            r.Summary,

                        Status =
                            r.Status,

                        RecordDate =
                            r.RecordDate
                    }
                )
                .ToListAsync();
        }

        // =====================================================
        // COLLECTIONS
        // =====================================================

        public async Task<
            List<MedicationCollectionResponseDto>>
            GetCollectionsAsync(
                Guid userId
            )
        {
            var patient =
                await GetPatientByUserIdAsync(
                    userId
                );

            var collections =
                await GetCollectionQuery()
                    .Where(
                        c =>
                            c.PatientId ==
                            patient.Id
                    )
                    .OrderByDescending(
                        c =>
                            c.ScheduledCollectionDate
                    )
                    .ToListAsync();

            return collections
                .Select(ToCollectionDto)
                .ToList();
        }

        public async Task<
            MedicationCollectionResponseDto?>
            GetNextCollectionAsync(
                Guid userId
            )
        {
            var patient =
                await GetPatientByUserIdAsync(
                    userId
                );

            return await
                GetNextCollectionForPatientAsync(
                    patient.Id
                );
        }

        private async Task<
            MedicationCollectionResponseDto?>
            GetNextCollectionForPatientAsync(
                Guid patientId
            )
        {
            var collection =
                await GetCollectionQuery()
                    .Where(c =>
                        c.PatientId ==
                            patientId &&
                        c.Status !=
                            "Collected" &&
                        c.Status !=
                            "Cancelled"
                    )
                    .OrderBy(
                        c =>
                            c.ScheduledCollectionDate
                    )
                    .FirstOrDefaultAsync();

            return collection == null
                ? null
                : ToCollectionDto(
                    collection
                );
        }

        // =====================================================
        // NOTIFICATIONS
        // =====================================================

        public async Task<
            List<NotificationResponseDto>>
            GetNotificationsAsync(
                Guid userId
            )
        {
            await GetPatientByUserIdAsync(
                userId
            );

            return await _context.Notifications
                .Where(
                    n => n.UserId == userId
                )
                .OrderByDescending(
                    n => n.CreatedAt
                )
                .Select(n =>
                    new NotificationResponseDto
                    {
                        Id =
                            n.Id,

                        UserId =
                            n.UserId,

                        Message =
                            n.Message,

                        IsRead =
                            n.IsRead,

                        CreatedAt =
                            n.CreatedAt
                    }
                )
                .ToListAsync();
        }

        public async Task MarkNotificationReadAsync(
            Guid userId,
            Guid notificationId
        )
        {
            await GetPatientByUserIdAsync(
                userId
            );

            var notification =
                await _context.Notifications
                    .FirstOrDefaultAsync(
                        n =>
                            n.Id ==
                                notificationId &&
                            n.UserId ==
                                userId
                    );

            if (notification == null)
            {
                throw new KeyNotFoundException(
                    "Notification not found."
                );
            }

            notification.IsRead = true;

            await _context.SaveChangesAsync();
        }

        // =====================================================
        // PREFERENCES
        // =====================================================

        public async Task<PatientPreferenceDto>
            GetPreferencesAsync(
                Guid userId
            )
        {
            var patient =
                await GetPatientByUserIdAsync(
                    userId
                );

            var preference =
                await EnsurePreferenceAsync(
                    patient.Id
                );

            return ToPreferenceDto(
                preference
            );
        }

        public async Task<PatientPreferenceDto>
            UpdatePreferencesAsync(
                Guid userId,
                PatientPreferenceDto dto
            )
        {
            var patient =
                await GetPatientByUserIdAsync(
                    userId
                );

            var preference =
                await EnsurePreferenceAsync(
                    patient.Id
                );

            preference.MedicationReminders =
                dto.MedicationReminders;

            preference.AppointmentReminders =
                dto.AppointmentReminders;

            preference.ClinicNotifications =
                dto.ClinicNotifications;

            preference.HealthUpdates =
                dto.HealthUpdates;

            preference.ShareHealthData =
                dto.ShareHealthData;

            preference.AllowChatbotProfileAccess =
                dto.AllowChatbotProfileAccess;

            preference.UpdatedAt =
                DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _audit.LogAsync(
                "PatientPreferencesUpdated",
                userId,
                $"Preferences updated for patient {patient.Id}."
            );

            return ToPreferenceDto(
                preference
            );
        }

        // =====================================================
        // PATIENT RESOLUTION
        // =====================================================

        private async Task<Patient>
            GetPatientByUserIdAsync(
                Guid userId
            )
        {
            var patient =
                await _context.Patients
                    .Include(p => p.User)
                    .Include(p => p.Clinic)
                    .Include(p => p.Allergies)
                    .Include(p => p.MedicalConditions)
                    .FirstOrDefaultAsync(
                        p =>
                            p.UserId ==
                                userId &&
                            p.User.Role ==
                                RoleNames.Patient &&
                            p.User.IsActive
                    );

            if (patient == null)
            {
                throw new UnauthorizedAccessException(
                    "Active Patient profile not found."
                );
            }

            return patient;
        }

        // =====================================================
        // PREFERENCE CREATION
        // =====================================================

        private async Task<PatientPreference>
            EnsurePreferenceAsync(
                Guid patientId
            )
        {
            var preference =
                await _context.PatientPreferences
                    .FirstOrDefaultAsync(
                        p =>
                            p.PatientId ==
                            patientId
                    );

            if (preference != null)
            {
                return preference;
            }

            preference =
                new PatientPreference
                {
                    Id =
                        Guid.NewGuid(),

                    PatientId =
                        patientId,

                    MedicationReminders =
                        true,

                    AppointmentReminders =
                        true,

                    ClinicNotifications =
                        true,

                    HealthUpdates =
                        false,

                    ShareHealthData =
                        true,

                    AllowChatbotProfileAccess =
                        true,

                    CreatedAt =
                        DateTime.UtcNow
                };

            _context.PatientPreferences.Add(
                preference
            );

            await _context.SaveChangesAsync();

            return preference;
        }

        // =====================================================
        // HELPERS
        // =====================================================

        private static PatientMeDto ToMeDto(
            Patient patient
        )
        {
            return new PatientMeDto
            {
                PatientId =
                    patient.Id,

                UserId =
                    patient.UserId,

                PatientNumber =
                    patient.PatientNumber,

                FullName =
                    patient.User.FullName,

                IdNumber =
                    patient.User.IdNumber,

                PhoneNumber =
                    patient.User.PhoneNumber,

                Email =
                    patient.User.Email,

                DateOfBirth =
                    patient.DateOfBirth ==
                    default
                        ? null
                        : patient.DateOfBirth,

                Gender =
                    patient.Gender,

                AddressLine1 =
                    patient.AddressLine1,

                AddressLine2 =
                    patient.AddressLine2,

                Suburb =
                    patient.Suburb,

                City =
                    patient.City,

                Province =
                    patient.Province,

                PostalCode =
                    patient.PostalCode,

                EmergencyContactName =
                    patient.EmergencyContactName,

                EmergencyContactPhone =
                    patient.EmergencyContactPhone,

                EmergencyContactRelationship =
                    patient
                        .EmergencyContactRelationship,

                ClinicId =
                    patient.ClinicId,

                ClinicName =
                    patient.Clinic?.Name,

                IsProfileComplete =
                    patient.IsProfileComplete,

                Allergies =
                    patient.Allergies
                        .Select(a =>
                            new PatientAllergyDto
                            {
                                Id =
                                    a.Id,

                                Name =
                                    a.AllergyName,

                                Reaction =
                                    a.Reaction,

                                Severity =
                                    a.Severity,

                                Notes =
                                    a.Notes
                            }
                        )
                        .ToList(),

                Conditions =
                    patient.MedicalConditions
                        .Where(c => c.IsActive)
                        .Select(c =>
                            new PatientConditionDto
                            {
                                Id =
                                    c.Id,

                                Name =
                                    c.ConditionName,

                                DiagnosisDate =
                                    c.DiagnosisDate,

                                IsChronic =
                                    c.IsChronic,

                                Notes =
                                    c.Notes
                            }
                        )
                        .ToList()
            };
        }

        private static bool IsProfileComplete(
            Patient patient
        )
        {
            return
                patient.DateOfBirth != default &&
                !string.IsNullOrWhiteSpace(
                    patient.Gender
                ) &&
                !string.IsNullOrWhiteSpace(
                    patient.AddressLine1
                ) &&
                !string.IsNullOrWhiteSpace(
                    patient.City
                ) &&
                !string.IsNullOrWhiteSpace(
                    patient.Province
                ) &&
                !string.IsNullOrWhiteSpace(
                    patient.EmergencyContactName
                ) &&
                !string.IsNullOrWhiteSpace(
                    patient.EmergencyContactPhone
                );
        }

        private static DateTime? CalculateNextDose(
            IEnumerable<MedicationSchedule> schedules
        )
        {
            var active =
                schedules
                    .Where(s => s.IsActive)
                    .OrderBy(s => s.TimeOfDay)
                    .ToList();

            if (active.Count == 0)
                return null;

            var now =
                DateTime.UtcNow;

            foreach (var schedule in active)
            {
                var candidate =
                    now.Date +
                    schedule.TimeOfDay;

                if (candidate >= now)
                    return candidate;
            }

            return
                now.Date.AddDays(1) +
                active[0].TimeOfDay;
        }

        private static string
            NormalizeAppointmentMode(
                string mode
            )
        {
            return string.Equals(
                mode,
                "Telehealth",
                StringComparison.OrdinalIgnoreCase
            )
                ? "Telehealth"
                : "InPerson";
        }

        private static AppointmentResponseDto
            ToAppointmentDto(
                Appointment appointment
            )
        {
            return new AppointmentResponseDto
            {
                Id =
                    appointment.Id,

                PatientId =
                    appointment.PatientId,

                PatientName =
                    appointment.Patient
                        .User.FullName,

                ClinicId =
                    appointment.ClinicId,

                ClinicName =
                    appointment.Clinic.Name,

                NurseId =
                    appointment.NurseId,

                NurseName =
                    appointment.Nurse?
                        .User.FullName,

                ScheduledAt =
                    appointment.ScheduledAt,

                DurationMinutes =
                    appointment.DurationMinutes,

                Type =
                    appointment.Type,

                Reason =
                    appointment.Reason,

                ProviderName =
                    appointment.ProviderName,

                Mode =
                    appointment.Mode,

                Status =
                    appointment.Status,

                Notes =
                    appointment.Notes
            };
        }

        private async Task<AppointmentResponseDto>
            GetAppointmentDtoAsync(
                Guid appointmentId,
                Guid patientId
            )
        {
            var appointment =
                await _context.Appointments
                    .Include(a => a.Patient)
                        .ThenInclude(p => p.User)
                    .Include(a => a.Clinic)
                    .Include(a => a.Nurse)
                        .ThenInclude(n => n!.User)
                    .FirstAsync(
                        a =>
                            a.Id ==
                                appointmentId &&
                            a.PatientId ==
                                patientId
                    );

            return ToAppointmentDto(
                appointment
            );
        }

        private static HealthMetricResponseDto
            ToHealthMetricDto(
                HealthMetric metric
            )
        {
            return new HealthMetricResponseDto
            {
                Id =
                    metric.Id,

                MetricType =
                    metric.MetricType,

                Value =
                    metric.Value,

                Unit =
                    metric.Unit,

                Status =
                    metric.Status,

                Note =
                    metric.Note,

                RecordedAt =
                    metric.RecordedAt
            };
        }

        private IQueryable<MedicationCollection>
            GetCollectionQuery()
        {
            return _context
                .MedicationCollections
                .Include(c => c.Patient)
                    .ThenInclude(p => p.User)
                .Include(c => c.Clinic)
                .Include(c => c.Proxy)
                    .ThenInclude(p => p!.User)
                .Include(c => c.ProcessedByNurse)
                    .ThenInclude(n => n!.User)
                .Include(c => c.Items)
                    .ThenInclude(i => i.Medication)
                .Include(c => c.Items)
                    .ThenInclude(i => i.ClinicStock);
        }

        private static MedicationCollectionResponseDto
            ToCollectionDto(
                MedicationCollection collection
            )
        {
            var status =
                collection.Status;

            if (
                status != "Collected" &&
                status != "Cancelled"
            )
            {
                status =
                    collection
                        .ScheduledCollectionDate
                        .Date <
                    DateTime.UtcNow.Date
                        ? "Overdue"
                        : "Pending";
            }

            return new MedicationCollectionResponseDto
            {
                Id =
                    collection.Id,

                PatientId =
                    collection.PatientId,

                PatientName =
                    collection.Patient
                        .User.FullName,

                ClinicId =
                    collection.ClinicId,

                ClinicName =
                    collection.Clinic.Name,

                ProxyId =
                    collection.ProxyId,

                ProxyName =
                    collection.Proxy?
                        .User.FullName,

                ProcessedByNurseId =
                    collection.ProcessedByNurseId,

                ProcessedByNurseName =
                    collection
                        .ProcessedByNurse?
                        .User.FullName,

                ScheduledCollectionDate =
                    collection
                        .ScheduledCollectionDate,

                CollectedAt =
                    collection.CollectedAt,

                Status =
                    status,

                MedicationName =
                    string.Join(
                        ", ",
                        collection.Items
                            .Select(
                                i =>
                                    i.Medication.Name
                            )
                            .Distinct()
                    ),

                Date =
                    collection
                        .ScheduledCollectionDate
                        .ToString(
                            "yyyy-MM-dd"
                        ),

                Notes =
                    collection.Notes,

                Items =
                    collection.Items
                        .Select(i =>
                            new MedicationCollectionItemResponseDto
                            {
                                Id =
                                    i.Id,

                                MedicationId =
                                    i.MedicationId,

                                ClinicStockId =
                                    i.ClinicStockId,

                                MedicationName =
                                    i.Medication.Name,

                                Dosage =
                                    i.Medication.Dosage,

                                Form =
                                    i.Medication.Form,

                                Quantity =
                                    i.Quantity,

                                Notes =
                                    i.Notes
                            }
                        )
                        .ToList()
            };
        }

        private static PatientPreferenceDto
            ToPreferenceDto(
                PatientPreference preference
            )
        {
            return new PatientPreferenceDto
            {
                MedicationReminders =
                    preference
                        .MedicationReminders,

                AppointmentReminders =
                    preference
                        .AppointmentReminders,

                ClinicNotifications =
                    preference
                        .ClinicNotifications,

                HealthUpdates =
                    preference.HealthUpdates,

                ShareHealthData =
                    preference.ShareHealthData,

                AllowChatbotProfileAccess =
                    preference
                        .AllowChatbotProfileAccess
            };
        }
    }
}