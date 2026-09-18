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

        public PatientService(
            PhilaLinkDbContext context,
            IAuditLogService audit,
            IMedicationService medicationService
        )
        {
            _context = context;
            _audit = audit;
            _medicationService = medicationService;
        }

        public async Task LogMedicationAsync(
            Guid userId,
            Guid medicationId,
            bool taken,
            string? notes
        )
        {
            await _medicationService.LogPatientMedicationAsync(
                userId,
                medicationId,
                taken,
                notes
            );
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
                    userId,
                    asTracking: false
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
                    userId,
                    asTracking: true
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
                    user =>
                        user.Id != userId &&
                        user.PhoneNumber ==
                            dto.PhoneNumber.Trim()
                );

            if (duplicatePhone)
            {
                throw new InvalidOperationException(
                    "That phone number is already in use."
                );
            }

            patient.User.FullName =
                dto.FullName.Trim();

            patient.User.PhoneNumber =
                dto.PhoneNumber.Trim();

            patient.User.Email =
                dto.Email.Trim();

            patient.User.UpdatedAt =
                DateTime.UtcNow;

            patient.DateOfBirth =
                dto.DateOfBirth;

            patient.Gender =
                dto.Gender.Trim();

            patient.Email =
                dto.Email.Trim();

            patient.AddressLine1 =
                dto.AddressLine1.Trim();

            patient.AddressLine2 =
                string.IsNullOrWhiteSpace(
                    dto.AddressLine2
                )
                    ? null
                    : dto.AddressLine2.Trim();

            patient.Suburb =
                dto.Suburb.Trim();

            patient.City =
                dto.City.Trim();

            patient.Province =
                dto.Province.Trim();

            patient.PostalCode =
                dto.PostalCode.Trim();

            patient.EmergencyContactName =
                dto.EmergencyContactName.Trim();

            patient.EmergencyContactPhone =
                dto.EmergencyContactPhone.Trim();

            patient.EmergencyContactRelationship =
                dto.EmergencyContactRelationship.Trim();

            patient.IsProfileComplete =
                IsProfileComplete(patient);

            if (
                patient.IsProfileComplete &&
                patient.ProfileCompletedAt == null
            )
            {
                patient.ProfileCompletedAt =
                    DateTime.UtcNow;
            }

            patient.UpdatedAt =
                DateTime.UtcNow;

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
            var now =
                DateTime.UtcNow;

            var patient =
                await _context.Patients
                    .AsNoTracking()
                    .Where(
                        patient =>
                            patient.UserId == userId &&
                            patient.User.Role ==
                                RoleNames.Patient &&
                            patient.User.IsActive
                    )
                    .Select(
                        patient =>
                            new
                            {
                                patient.Id,
                                patient.PatientNumber,
                                patient.IsProfileComplete,

                                FullName =
                                    patient.User.FullName,

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
                                                patient.Clinic.ContactNumber,

                                            OpeningTime =
                                                patient.Clinic.OpeningTime,

                                            ClosingTime =
                                                patient.Clinic.ClosingTime
                                        }
                            }
                    )
                    .FirstOrDefaultAsync();

            if (patient == null)
            {
                throw new UnauthorizedAccessException(
                    "Active Patient profile not found."
                );
            }

            var medicationEntities =
                await _context.Medications
                    .AsNoTracking()
                    .Include(
                        medication =>
                            medication.Schedules
                                .Where(
                                    schedule =>
                                        schedule.IsActive
                                )
                    )
                    .Where(
                        medication =>
                            medication.PatientId ==
                                patient.Id &&
                            medication.IsActive
                    )
                    .OrderBy(
                        medication =>
                            medication.Name
                    )
                    .Take(3)
                    .ToListAsync();

            var medications =
                medicationEntities
                    .Select(
                        medication =>
                            ToPatientMedicationDto(
                                medication,
                                now
                            )
                    )
                    .ToList();

            var appointments =
                await _context.Appointments
                    .AsNoTracking()
                    .Where(
                        appointment =>
                            appointment.PatientId ==
                                patient.Id &&
                            appointment.ScheduledAt >=
                                now &&
                            appointment.Status !=
                                "Cancelled" &&
                            appointment.Status !=
                                "Completed"
                    )
                    .OrderBy(
                        appointment =>
                            appointment.ScheduledAt
                    )
                    .Take(3)
                    .Select(
                        appointment =>
                            new AppointmentResponseDto
                            {
                                Id =
                                    appointment.Id,

                                PatientId =
                                    appointment.PatientId,

                                PatientName =
                                    appointment
                                        .Patient
                                        .User
                                        .FullName,

                                ClinicId =
                                    appointment.ClinicId,

                                ClinicName =
                                    appointment
                                        .Clinic
                                        .Name,

                                NurseId =
                                    appointment.NurseId,

                                NurseName =
                                    appointment.Nurse == null
                                        ? null
                                        : appointment
                                            .Nurse
                                            .User
                                            .FullName,

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
                            }
                    )
                    .ToListAsync();

            var metricRows =
                await _context.HealthMetrics
                    .AsNoTracking()
                    .Where(
                        metric =>
                            metric.PatientId ==
                                patient.Id
                    )
                    .OrderByDescending(
                        metric =>
                            metric.RecordedAt
                    )
                    .Select(
                        metric =>
                            new HealthMetricResponseDto
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
                            }
                    )
                    .ToListAsync();

            var metrics =
                metricRows
                    .GroupBy(
                        metric =>
                            metric.MetricType
                    )
                    .Select(
                        group =>
                            group.First()
                    )
                    .ToList();

            var unread =
                await _context.Notifications
                    .CountAsync(
                        notification =>
                            notification.UserId ==
                                userId &&
                            !notification.IsRead
                    );

            var nextCollection =
                await ProjectCollectionQuery(
                    _context
                        .MedicationCollections
                        .AsNoTracking()
                        .Where(
                            collection =>
                                collection.PatientId ==
                                    patient.Id &&
                                collection.Status !=
                                    MedicationCollectionStatuses
                                        .Collected &&
                                collection.Status !=
                                    MedicationCollectionStatuses
                                        .Cancelled
                        )
                )
                .OrderBy(
                    collection =>
                        collection
                            .ScheduledCollectionDate
                )
                .FirstOrDefaultAsync();

            if (nextCollection != null)
            {
                FinalizeCollectionDto(
                    nextCollection
                );
            }

            return new PatientDashboardDto
            {
                FullName =
                    patient.FullName,

                PatientNumber =
                    patient.PatientNumber,

                IsProfileComplete =
                    patient.IsProfileComplete,

                Clinic =
                    patient.Clinic,

                Medications =
                    medications,

                UpcomingAppointments =
                    appointments,

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
                await GetActivePatientAccessAsync(
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
            var now =
                DateTime.UtcNow;

            var medications =
                await _context.Medications
                    .AsNoTracking()
                    .Include(
                        medication =>
                            medication.Schedules
                                .Where(
                                    schedule =>
                                        schedule.IsActive
                                )
                    )
                    .Where(
                        medication =>
                            medication.PatientId ==
                                patientId &&
                            medication.IsActive
                    )
                    .OrderBy(
                        medication =>
                            medication.Name
                    )
                    .ToListAsync();

            return medications
                .Select(
                    medication =>
                        ToPatientMedicationDto(
                            medication,
                            now
                        )
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
                await GetActivePatientAccessAsync(
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
                await GetActivePatientAccessAsync(
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
                    Id =
                        Guid.NewGuid(),

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
                await GetActivePatientAccessAsync(
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
                        item =>
                            item.Id ==
                                appointmentId &&
                            item.PatientId ==
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

            appointment.ScheduledAt =
                dto.ScheduledAt;

            appointment.Status =
                "Rescheduled";

            appointment.UpdatedAt =
                DateTime.UtcNow;

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
                await GetActivePatientAccessAsync(
                    userId
                );

            var appointment =
                await _context.Appointments
                    .FirstOrDefaultAsync(
                        item =>
                            item.Id ==
                                appointmentId &&
                            item.PatientId ==
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
            return await _context.Appointments
                .AsNoTracking()
                .Where(
                    appointment =>
                        appointment.PatientId ==
                            patientId
                )
                .OrderByDescending(
                    appointment =>
                        appointment.ScheduledAt
                )
                .Select(
                    appointment =>
                        new AppointmentResponseDto
                        {
                            Id =
                                appointment.Id,

                            PatientId =
                                appointment.PatientId,

                            PatientName =
                                appointment
                                    .Patient
                                    .User
                                    .FullName,

                            ClinicId =
                                appointment.ClinicId,

                            ClinicName =
                                appointment
                                    .Clinic
                                    .Name,

                            NurseId =
                                appointment.NurseId,

                            NurseName =
                                appointment.Nurse == null
                                    ? null
                                    : appointment
                                        .Nurse
                                        .User
                                        .FullName,

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
                        }
                )
                .ToListAsync();
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
                await GetActivePatientAccessAsync(
                    userId
                );

            return await _context.HealthRecords
                .AsNoTracking()
                .Where(
                    record =>
                        record.PatientId ==
                            patient.Id
                )
                .OrderByDescending(
                    record =>
                        record.RecordDate
                )
                .Select(
                    record =>
                        new HealthRecordResponseDto
                        {
                            Id =
                                record.Id,

                            Title =
                                record.Title,

                            Type =
                                record.Type,

                            Category =
                                record.Category,

                            ProviderName =
                                record.ProviderName,

                            Facility =
                                record.Clinic == null
                                    ? null
                                    : record
                                        .Clinic
                                        .Name,

                            Summary =
                                record.Summary,

                            Status =
                                record.Status,

                            RecordDate =
                                record.RecordDate
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
                await GetActivePatientAccessAsync(
                    userId
                );

            var collections =
                await ProjectCollectionQuery(
                    _context
                        .MedicationCollections
                        .AsNoTracking()
                        .Where(
                            collection =>
                                collection.PatientId ==
                                    patient.Id
                        )
                )
                .OrderByDescending(
                    collection =>
                        collection
                            .ScheduledCollectionDate
                )
                .ToListAsync();

            foreach (var collection in collections)
            {
                FinalizeCollectionDto(
                    collection
                );
            }

            return collections;
        }

        public async Task<
            MedicationCollectionResponseDto?>
            GetNextCollectionAsync(
                Guid userId
            )
        {
            var patient =
                await GetActivePatientAccessAsync(
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
                await ProjectCollectionQuery(
                    _context
                        .MedicationCollections
                        .AsNoTracking()
                        .Where(
                            item =>
                                item.PatientId ==
                                    patientId &&
                                item.Status !=
                                    "Collected" &&
                                item.Status !=
                                    "Cancelled"
                        )
                )
                .OrderBy(
                    item =>
                        item
                            .ScheduledCollectionDate
                )
                .FirstOrDefaultAsync();

            if (collection == null)
            {
                return null;
            }

            FinalizeCollectionDto(
                collection
            );

            return collection;
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
            _ =
                await GetActivePatientAccessAsync(
                    userId
                );

            return await _context.Notifications
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
        }

        public async Task MarkNotificationReadAsync(
            Guid userId,
            Guid notificationId
        )
        {
            _ =
                await GetActivePatientAccessAsync(
                    userId
                );

            var notification =
                await _context.Notifications
                    .FirstOrDefaultAsync(
                        item =>
                            item.Id ==
                                notificationId &&
                            item.UserId ==
                                userId
                    );

            if (notification == null)
            {
                throw new KeyNotFoundException(
                    "Notification not found."
                );
            }

            notification.IsRead =
                true;

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
                await GetActivePatientAccessAsync(
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
                await GetActivePatientAccessAsync(
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

        private async Task<PatientAccess>
            GetActivePatientAccessAsync(
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
                            new PatientAccess
                            {
                                Id =
                                    item.Id,

                                ClinicId =
                                    item.ClinicId
                            }
                    )
                    .FirstOrDefaultAsync();

            if (patient == null)
            {
                throw new UnauthorizedAccessException(
                    "Active Patient profile not found."
                );
            }

            return patient;
        }

        private async Task<Patient>
            GetPatientByUserIdAsync(
                Guid userId,
                bool asTracking
            )
        {
            IQueryable<Patient> query =
                _context.Patients;

            if (!asTracking)
            {
                query =
                    query.AsNoTracking();
            }

            var patient =
                await query
                    .Include(
                        item =>
                            item.User
                    )
                    .Include(
                        item =>
                            item.Clinic
                    )
                    .Include(
                        item =>
                            item.Allergies
                    )
                    .Include(
                        item =>
                            item.MedicalConditions
                    )
                    .FirstOrDefaultAsync(
                        item =>
                            item.UserId ==
                                userId &&
                            item.User.Role ==
                                RoleNames.Patient &&
                            item.User.IsActive
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
                        item =>
                            item.PatientId ==
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
                        .Select(
                            allergy =>
                                new PatientAllergyDto
                                {
                                    Id =
                                        allergy.Id,

                                    Name =
                                        allergy.AllergyName,

                                    Reaction =
                                        allergy.Reaction,

                                    Severity =
                                        allergy.Severity,

                                    Notes =
                                        allergy.Notes
                                }
                        )
                        .ToList(),

                Conditions =
                    patient.MedicalConditions
                        .Where(
                            condition =>
                                condition.IsActive
                        )
                        .Select(
                            condition =>
                                new PatientConditionDto
                                {
                                    Id =
                                        condition.Id,

                                    Name =
                                        condition.ConditionName,

                                    DiagnosisDate =
                                        condition.DiagnosisDate,

                                    IsChronic =
                                        condition.IsChronic,

                                    Notes =
                                        condition.Notes
                                }
                        )
                        .ToList()
            };
        }

        private static PatientMedicationDto
            ToPatientMedicationDto(
                Medication medication,
                DateTime now
            )
        {
            return new PatientMedicationDto
            {
                Id =
                    medication.Id,

                Name =
                    medication.Name,

                Dosage =
                    medication.Dosage,

                Form =
                    medication.Form,

                Instructions =
                    medication.Instructions,

                PrescribedBy =
                    medication.PrescribedBy,

                ConditionName =
                    medication.ConditionName,

                StartDate =
                    medication.StartDate,

                EndDate =
                    medication.EndDate,

                IsActive =
                    medication.IsActive,

                ScheduleTimes =
                    medication.Schedules
                        .Where(
                            schedule =>
                                schedule.IsActive
                        )
                        .OrderBy(
                            schedule =>
                                schedule.TimeOfDay
                        )
                        .Select(
                            schedule =>
                                schedule.TimeOfDay
                                    .ToString(
                                        @"hh\:mm"
                                    )
                        )
                        .ToList(),

                NextDoseAt =
                    CalculateNextDose(
                        medication.Schedules,
                        now
                    ),

                DaysRemaining =
                    medication.EndDate == null
                        ? null
                        : Math.Max(
                            0,
                            (
                                medication
                                    .EndDate
                                    .Value
                                    .Date -
                                now.Date
                            ).Days
                        )
            };
        }

        private static bool IsProfileComplete(
            Patient patient
        )
        {
            return
                patient.DateOfBirth !=
                    default &&
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
            IEnumerable<MedicationSchedule> schedules,
            DateTime? nowOverride = null
        )
        {
            var active =
                schedules
                    .Where(
                        schedule =>
                            schedule.IsActive
                    )
                    .OrderBy(
                        schedule =>
                            schedule.TimeOfDay
                    )
                    .ToList();

            if (active.Count == 0)
            {
                return null;
            }

            var now =
                nowOverride ??
                DateTime.UtcNow;

            foreach (
                var schedule in active
            )
            {
                var candidate =
                    now.Date +
                    schedule.TimeOfDay;

                if (candidate >= now)
                {
                    return candidate;
                }
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

        private async Task<AppointmentResponseDto>
            GetAppointmentDtoAsync(
                Guid appointmentId,
                Guid patientId
            )
        {
            return await _context.Appointments
                .AsNoTracking()
                .Where(
                    appointment =>
                        appointment.Id ==
                            appointmentId &&
                        appointment.PatientId ==
                            patientId
                )
                .Select(
                    appointment =>
                        new AppointmentResponseDto
                        {
                            Id =
                                appointment.Id,

                            PatientId =
                                appointment.PatientId,

                            PatientName =
                                appointment
                                    .Patient
                                    .User
                                    .FullName,

                            ClinicId =
                                appointment.ClinicId,

                            ClinicName =
                                appointment
                                    .Clinic
                                    .Name,

                            NurseId =
                                appointment.NurseId,

                            NurseName =
                                appointment.Nurse == null
                                    ? null
                                    : appointment
                                        .Nurse
                                        .User
                                        .FullName,

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
                        }
                )
                .FirstAsync();
        }

        private static IQueryable<
            MedicationCollectionResponseDto>
            ProjectCollectionQuery(
                IQueryable<MedicationCollection> query
            )
        {
            return query.Select(
                collection =>
                    new MedicationCollectionResponseDto
                    {
                        Id =
                            collection.Id,

                        PatientId =
                            collection.PatientId,

                        PatientName =
                            collection
                                .Patient
                                .User
                                .FullName,

                        ClinicId =
                            collection.ClinicId,

                        ClinicName =
                            collection
                                .Clinic
                                .Name,

                        ProxyId =
                            collection.ProxyId,

                        ProxyName =
                            collection.Proxy == null
                                ? null
                                : collection
                                    .Proxy
                                    .User
                                    .FullName,

                        ProcessedByNurseId =
                            collection
                                .ProcessedByNurseId,

                        ProcessedByNurseName =
                            collection.ProcessedByNurse ==
                                null
                                ? null
                                : collection
                                    .ProcessedByNurse
                                    .User
                                    .FullName,

                        ScheduledCollectionDate =
                            collection
                                .ScheduledCollectionDate,

                        CollectedAt =
                            collection.CollectedAt,

                        /*
                         * Raw status is finalized after the
                         * database projection so Pending/Overdue
                         * behavior remains exactly as before.
                         */
                        Status =
                            collection.Status,

                        MedicationName =
                            string.Empty,

                        Date =
                            string.Empty,

                        Notes =
                            collection.Notes,

                        Items =
                            collection.Items
                                .Select(
                                    item =>
                                        new MedicationCollectionItemResponseDto
                                        {
                                            Id =
                                                item.Id,

                                            MedicationId =
                                                item.MedicationId,

                                            ClinicStockId =
                                                item.ClinicStockId,

                                            MedicationName =
                                                item
                                                    .Medication
                                                    .Name,

                                            Dosage =
                                                item
                                                    .Medication
                                                    .Dosage,

                                            Form =
                                                item
                                                    .Medication
                                                    .Form,

                                            Quantity =
                                                item.Quantity,

                                            Notes =
                                                item.Notes
                                        }
                                )
                                .ToList()
                    }
            );
        }

        private static void FinalizeCollectionDto(
            MedicationCollectionResponseDto collection
        )
        {
            if (
                collection.Status !=
                    "Collected" &&
                collection.Status !=
                    "Cancelled"
            )
            {
                collection.Status =
                    collection
                        .ScheduledCollectionDate
                        .Date <
                    DateTime.UtcNow.Date
                        ? "Overdue"
                        : "Pending";
            }

            collection.MedicationName =
                string.Join(
                    ", ",
                    collection.Items
                        .Select(
                            item =>
                                item.MedicationName
                        )
                        .Distinct()
                );

            collection.Date =
                collection
                    .ScheduledCollectionDate
                    .ToString(
                        "yyyy-MM-dd"
                    );
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

        private sealed class PatientAccess
        {
            public Guid Id
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
