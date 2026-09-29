using Microsoft.EntityFrameworkCore;
using PersonalProject.Data;
using PersonalProject.Models.Constants;
using PersonalProject.Models.DTOs;
using PersonalProject.Models.Entities;
using PersonalProject.Services.Interfaces;

namespace PersonalProject.Services.Implementations
{
    public class NurseService :
        INurseService
    {
        private readonly PhilaLinkDbContext
            _context;

        private readonly IAuditLogService
            _audit;

        public NurseService(
            PhilaLinkDbContext context,
            IAuditLogService audit
        )
        {
            _context =
                context;

            _audit =
                audit;
        }

        // =====================================================
        // PROFILE
        // =====================================================

        public async Task<NurseMeDto>
            GetMeAsync(
                Guid userId
            )
        {
            var nurse =
                await GetActiveNurseAsync(
                    userId,
                    false
                );

            return ToMeDto(
                nurse
            );
        }

        public async Task<NurseMeDto>
            UpdateMeAsync(
                Guid userId,
                UpdateNurseProfileDto dto
            )
        {
            var nurse =
                await GetActiveNurseAsync(
                    userId,
                    true
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

            if (
                string.IsNullOrWhiteSpace(
                    dto.Email
                )
            )
            {
                throw new InvalidOperationException(
                    "Email address is required."
                );
            }

            var normalizedPhone =
                dto.PhoneNumber
                    .Trim();

            var normalizedEmail =
                dto.Email
                    .Trim()
                    .ToLowerInvariant();

            var duplicatePhone =
                await _context.Users
                    .AsNoTracking()
                    .AnyAsync(
                        user =>
                            user.Id !=
                                userId &&
                            user.PhoneNumber ==
                                normalizedPhone
                    );

            if (
                duplicatePhone
            )
            {
                throw new InvalidOperationException(
                    "That phone number is already in use."
                );
            }

            var duplicateEmail =
                await _context.Users
                    .AsNoTracking()
                    .AnyAsync(
                        user =>
                            user.Id !=
                                userId &&
                            user.Email
                                .ToLower() ==
                                normalizedEmail
                    );

            if (
                duplicateEmail
            )
            {
                throw new InvalidOperationException(
                    "That email address is already in use."
                );
            }

            var now =
                DateTime.UtcNow;

            nurse.User.FullName =
                dto.FullName
                    .Trim();

            nurse.User.PhoneNumber =
                normalizedPhone;

            nurse.User.Email =
                normalizedEmail;

            nurse.User.UpdatedAt =
                now;

            nurse.Email =
                normalizedEmail;

            nurse.Gender =
                dto.Gender
                    .Trim();

            nurse.AddressLine1 =
                dto.AddressLine1
                    .Trim();

            nurse.AddressLine2 =
                string.IsNullOrWhiteSpace(
                    dto.AddressLine2
                )
                    ? null
                    : dto.AddressLine2
                        .Trim();

            nurse.Suburb =
                dto.Suburb
                    .Trim();

            nurse.City =
                dto.City
                    .Trim();

            nurse.Province =
                dto.Province
                    .Trim();

            nurse.PostalCode =
                dto.PostalCode
                    .Trim();

            nurse.EmergencyContactName =
                dto.EmergencyContactName
                    .Trim();

            nurse.EmergencyContactPhone =
                dto.EmergencyContactPhone
                    .Trim();

            nurse.EmergencyContactRelationship =
                dto
                    .EmergencyContactRelationship
                    .Trim();

            nurse.UpdatedAt =
                now;

            await _context
                .SaveChangesAsync();

            await _audit.LogAsync(
                "NurseProfileUpdated",
                userId,
                $"Nurse {nurse.Id} updated their profile.",
                nurse.ClinicId
            );

            return ToMeDto(
                nurse
            );
        }

        // =====================================================
        // DASHBOARD
        // =====================================================

        public async Task<NurseDashboardDto>
            GetDashboardAsync(
                Guid userId
            )
        {
            var nurse =
                await GetActiveNurseAsync(
                    userId,
                    false
                );

            var clinicId =
                nurse.ClinicId;

            var today =
                DateTime.UtcNow.Date;

            var tomorrow =
                today.AddDays(
                    1
                );

            var clinicPatients =
                await _context.Patients
                    .AsNoTracking()
                    .CountAsync(
                        patient =>
                            patient.ClinicId ==
                                clinicId &&
                            patient.User.IsActive
                    );

            var appointmentsToday =
                await _context.Appointments
                    .AsNoTracking()
                    .CountAsync(
                        appointment =>
                            appointment.ClinicId ==
                                clinicId &&
                            appointment.ScheduledAt >=
                                today &&
                            appointment.ScheduledAt <
                                tomorrow &&
                            appointment.Status !=
                                AppointmentStatuses.Cancelled
                    );

            var pendingAppointments =
                await _context.Appointments
                    .AsNoTracking()
                    .CountAsync(
                        appointment =>
                            appointment.ClinicId ==
                                clinicId &&
                            appointment.Status ==
                                AppointmentStatuses.Pending
                    );

            var collectionsDueToday =
                await _context
                    .MedicationCollections
                    .AsNoTracking()
                    .CountAsync(
                        collection =>
                            collection.ClinicId ==
                                clinicId &&
                            collection
                                .ScheduledCollectionDate >=
                                today &&
                            collection
                                .ScheduledCollectionDate <
                                tomorrow &&
                            collection.Status !=
                                MedicationCollectionStatuses
                                    .Collected &&
                            collection.Status !=
                                MedicationCollectionStatuses
                                    .Cancelled
                    );

            var overdueCollections =
                await _context
                    .MedicationCollections
                    .AsNoTracking()
                    .CountAsync(
                        collection =>
                            collection.ClinicId ==
                                clinicId &&
                            collection
                                .ScheduledCollectionDate <
                                today &&
                            collection.Status !=
                                MedicationCollectionStatuses
                                    .Collected &&
                            collection.Status !=
                                MedicationCollectionStatuses
                                    .Cancelled
                    );

            return new NurseDashboardDto
            {
                ClinicPatients =
                    clinicPatients,

                AppointmentsToday =
                    appointmentsToday,

                PendingAppointments =
                    pendingAppointments,

                CollectionsDueToday =
                    collectionsDueToday,

                OverdueCollections =
                    overdueCollections
            };
        }

        // =====================================================
        // PATIENT LIST
        // =====================================================

        public async Task<List<NursePatientDto>>
            GetClinicPatientsAsync(
                Guid userId
            )
        {
            var nurse =
                await GetActiveNurseAsync(
                    userId,
                    false
                );

            var today =
                DateTime.UtcNow.Date;

            var now =
                DateTime.UtcNow;

            return await _context.Patients
                .AsNoTracking()
                .Where(
                    patient =>
                        patient.ClinicId ==
                            nurse.ClinicId &&
                        patient.User.IsActive
                )
                .OrderBy(
                    patient =>
                        patient.User.FullName
                )
                .Select(
                    patient =>
                        new NursePatientDto
                        {
                            PatientId =
                                patient.Id,

                            UserId =
                                patient.UserId,

                            PatientNumber =
                                patient.PatientNumber,

                            FullName =
                                patient.User.FullName,

                            DateOfBirth =
                                patient.DateOfBirth,

                            Gender =
                                patient.Gender,

                            PhoneNumber =
                                patient
                                    .User
                                    .PhoneNumber,

                            AllergyCount =
                                patient
                                    .Allergies
                                    .Count(),

                            ActiveMedicationCount =
                                _context
                                    .Medications
                                    .Count(
                                        medication =>
                                            medication
                                                .PatientId ==
                                                patient.Id &&
                                            medication
                                                .IsActive
                                    ),

                            OverdueCollectionCount =
                                _context
                                    .MedicationCollections
                                    .Count(
                                        collection =>
                                            collection
                                                .PatientId ==
                                                patient.Id &&
                                            collection
                                                .ScheduledCollectionDate <
                                                today &&
                                            collection.Status !=
                                                MedicationCollectionStatuses
                                                    .Collected &&
                                            collection.Status !=
                                                MedicationCollectionStatuses
                                                    .Cancelled
                                    ),

                            NextCollectionDate =
                                _context
                                    .MedicationCollections
                                    .Where(
                                        collection =>
                                            collection
                                                .PatientId ==
                                                patient.Id &&
                                            collection
                                                .ScheduledCollectionDate >=
                                                today &&
                                            collection.Status !=
                                                MedicationCollectionStatuses
                                                    .Collected &&
                                            collection.Status !=
                                                MedicationCollectionStatuses
                                                    .Cancelled
                                    )
                                    .OrderBy(
                                        collection =>
                                            collection
                                                .ScheduledCollectionDate
                                    )
                                    .Select(
                                        collection =>
                                            (DateTime?)
                                                collection
                                                    .ScheduledCollectionDate
                                    )
                                    .FirstOrDefault(),

                            NextAppointmentAt =
                                _context
                                    .Appointments
                                    .Where(
                                        appointment =>
                                            appointment
                                                .PatientId ==
                                                patient.Id &&
                                            appointment
                                                .ScheduledAt >=
                                                now &&
                                            appointment
                                                .Status !=
                                                AppointmentStatuses
                                                    .Cancelled
                                    )
                                    .OrderBy(
                                        appointment =>
                                            appointment
                                                .ScheduledAt
                                    )
                                    .Select(
                                        appointment =>
                                            (DateTime?)
                                                appointment
                                                    .ScheduledAt
                                    )
                                    .FirstOrDefault()
                        }
                )
                .ToListAsync();
        }

        // =====================================================
        // ALERTS
        // =====================================================

        public async Task<List<NurseAlertDto>>
            GetUrgentAlertsAsync(
                Guid userId
            )
        {
            var nurse =
                await GetActiveNurseAsync(
                    userId,
                    false
                );

            var today =
                DateTime.UtcNow.Date;

            var tomorrow =
                today.AddDays(
                    1
                );

            var overdueCollections =
                await _context
                    .MedicationCollections
                    .AsNoTracking()
                    .CountAsync(
                        collection =>
                            collection.ClinicId ==
                                nurse.ClinicId &&
                            collection
                                .ScheduledCollectionDate <
                                today &&
                            collection.Status !=
                                MedicationCollectionStatuses
                                    .Collected &&
                            collection.Status !=
                                MedicationCollectionStatuses
                                    .Cancelled
                    );

            var collectionsDueToday =
                await _context
                    .MedicationCollections
                    .AsNoTracking()
                    .CountAsync(
                        collection =>
                            collection.ClinicId ==
                                nurse.ClinicId &&
                            collection
                                .ScheduledCollectionDate >=
                                today &&
                            collection
                                .ScheduledCollectionDate <
                                tomorrow &&
                            collection.Status !=
                                MedicationCollectionStatuses
                                    .Collected &&
                            collection.Status !=
                                MedicationCollectionStatuses
                                    .Cancelled
                    );

            var pendingAppointments =
                await _context.Appointments
                    .AsNoTracking()
                    .CountAsync(
                        appointment =>
                            appointment.ClinicId ==
                                nurse.ClinicId &&
                            appointment.Status ==
                                AppointmentStatuses.Pending
                    );

            var alerts =
                new List<NurseAlertDto>();

            if (
                overdueCollections >
                0
            )
            {
                alerts.Add(
                    new NurseAlertDto
                    {
                        Code =
                            "OVERDUE_COLLECTIONS",

                        Severity =
                            "High",

                        Count =
                            overdueCollections,

                        Message =
                            $"{overdueCollections} medication collection(s) are overdue."
                    }
                );
            }

            if (
                collectionsDueToday >
                0
            )
            {
                alerts.Add(
                    new NurseAlertDto
                    {
                        Code =
                            "COLLECTIONS_DUE_TODAY",

                        Severity =
                            "Medium",

                        Count =
                            collectionsDueToday,

                        Message =
                            $"{collectionsDueToday} medication collection(s) are due today."
                    }
                );
            }

            if (
                pendingAppointments >
                0
            )
            {
                alerts.Add(
                    new NurseAlertDto
                    {
                        Code =
                            "PENDING_APPOINTMENTS",

                        Severity =
                            "Medium",

                        Count =
                            pendingAppointments,

                        Message =
                            $"{pendingAppointments} appointment request(s) are waiting for review."
                    }
                );
            }

            return alerts;
        }

        // =====================================================
        // PATIENT CARE
        // =====================================================

        public async Task<NursePatientCareDto>
            GetPatientCareAsync(
                Guid userId,
                Guid patientId
            )
        {
            var (
                nurse,
                patient
            ) =
                await GetClinicPatientAsync(
                    userId,
                    patientId
                );

            var allergies =
                await _context.Allergies
                    .AsNoTracking()
                    .Where(
                        allergy =>
                            allergy.PatientId ==
                                patient.Id
                    )
                    .OrderBy(
                        allergy =>
                            allergy.AllergyName
                    )
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
                    .ToListAsync();

            var conditions =
                await _context
                    .MedicalConditions
                    .AsNoTracking()
                    .Where(
                        condition =>
                            condition.PatientId ==
                                patient.Id
                    )
                    .OrderByDescending(
                        condition =>
                            condition.IsActive
                    )
                    .ThenBy(
                        condition =>
                            condition
                                .ConditionName
                    )
                    .Select(
                        condition =>
                            new NurseConditionDto
                            {
                                Id =
                                    condition.Id,

                                Name =
                                    condition
                                        .ConditionName,

                                DiagnosisDate =
                                    condition
                                        .DiagnosisDate,

                                IsChronic =
                                    condition
                                        .IsChronic,

                                Notes =
                                    condition.Notes,

                                IsActive =
                                    condition
                                        .IsActive
                            }
                    )
                    .ToListAsync();

                                var healthMetrics =
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
                    .Take(
                        20
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

            var medications =
                await _context.Medications
                    .AsNoTracking()
                    .Include(
                        medication =>
                            medication.Schedules
                    )
                    .Where(
                        medication =>
                            medication.PatientId ==
                                patient.Id
                    )
                    .OrderByDescending(
                        medication =>
                            medication.IsActive
                    )
                    .ThenBy(
                        medication =>
                            medication.Name
                    )
                    .ToListAsync();

            var collections =
                await _context
                    .MedicationCollections
                    .AsNoTrackingWithIdentityResolution()
                    .AsSplitQuery()
                    .Include(
                        collection =>
                            collection.Items
                    )
                    .ThenInclude(
                        item =>
                            item.Medication
                    )
                    .Include(
                        collection =>
                            collection.Proxy
                    )
                    .ThenInclude(
                        proxy =>
                            proxy!.User
                    )
                    .Include(
                        collection =>
                            collection
                                .ProcessedByNurse
                    )
                    .ThenInclude(
                        processingNurse =>
                            processingNurse!.User
                    )
                    .Where(
                        collection =>
                            collection.PatientId ==
                                patient.Id &&
                            collection.ClinicId ==
                                nurse.ClinicId
                    )
                    .OrderByDescending(
                        collection =>
                            collection
                                .ScheduledCollectionDate
                    )
                    .Take(
                        20
                    )
                    .ToListAsync();

            var appointments =
                await _context.Appointments
                    .AsNoTracking()
                    .Include(
                        appointment =>
                            appointment.Nurse
                    )
                    .ThenInclude(
                        appointmentNurse =>
                            appointmentNurse!.User
                    )
                    .Where(
                        appointment =>
                            appointment.PatientId ==
                                patient.Id &&
                            appointment.ClinicId ==
                                nurse.ClinicId
                    )
                    .OrderByDescending(
                        appointment =>
                            appointment.ScheduledAt
                    )
                    .Take(
                        20
                    )
                    .ToListAsync();

            var proxies =
                await _context.ProxyLinks
                    .AsNoTracking()
                    .Where(
                        link =>
                            link.PatientId ==
                                patient.Id &&
                            link.IsActive &&
                            link.Proxy.ClinicId ==
                                nurse.ClinicId &&
                            link.Proxy
                                .User
                                .IsActive
                    )
                    .OrderByDescending(
                        link =>
                            link.AssignedAt
                    )
                    .Select(
                        link =>
                            new NursePatientProxyDto
                            {
                                ProxyLinkId =
                                    link.Id,

                                ProxyId =
                                    link.ProxyId,

                                FullName =
                                    link.Proxy
                                        .User
                                        .FullName,

                                PhoneNumber =
                                    link.Proxy
                                        .User
                                        .PhoneNumber,

                                AssignedAt =
                                    link.AssignedAt
                            }
                    )
                    .ToListAsync();

            return new NursePatientCareDto
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
                    patient
                        .User
                        .PhoneNumber,

                Email =
                    patient.User.Email,

                DateOfBirth =
                    patient.DateOfBirth,

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

                ClinicId =
                    nurse.ClinicId,

                ClinicName =
                    nurse.Clinic.Name,

                Allergies =
                    allergies,

                Conditions =
                    conditions,
                    HealthMetrics = healthMetrics,

                Medications =
                    medications
                        .Select(
                            medication =>
                                new NurseMedicationDto
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
                                        medication
                                            .Instructions,

                                    UnitsPerDose =
                                        medication
                                            .UnitsPerDose,

                                    PrescribedBy =
                                        medication
                                            .PrescribedBy,

                                    ConditionName =
                                        medication
                                            .ConditionName,

                                    StartDate =
                                        medication
                                            .StartDate,

                                    EndDate =
                                        medication.EndDate,

                                    IsActive =
                                        medication.IsActive,

                                    Schedules =
                                        medication
                                            .Schedules
                                            .OrderBy(
                                                schedule =>
                                                    schedule
                                                        .TimeOfDay
                                            )
                                            .Select(
                                                schedule =>
                                                    new NurseMedicationScheduleDto
                                                    {
                                                        Id =
                                                            schedule.Id,

                                                        TimeOfDay =
                                                            schedule
                                                                .TimeOfDay
                                                                .ToString(
                                                                    @"hh\:mm"
                                                                ),

                                                        IsActive =
                                                            schedule
                                                                .IsActive
                                                    }
                                            )
                                            .ToList()
                                }
                        )
                        .ToList(),

                Collections =
                    collections
                        .Select(
                            ToCollectionDto
                        )
                        .ToList(),

                Appointments =
                    appointments
                        .Select(
                            appointment =>
                                new NurseAppointmentDto
                                {
                                    Id =
                                        appointment.Id,

                                    ScheduledAt =
                                        appointment
                                            .ScheduledAt,

                                    DurationMinutes =
                                        appointment
                                            .DurationMinutes,

                                    Type =
                                        appointment.Type,

                                    Reason =
                                        appointment.Reason,

                                    ProviderName =
                                        appointment
                                            .ProviderName,

                                    Mode =
                                        appointment.Mode,

                                    Status =
                                        appointment.Status,

                                    Notes =
                                        appointment.Notes,

                                    NurseId =
                                        appointment.NurseId,

                                    NurseName =
                                        appointment
                                            .Nurse?
                                            .User
                                            .FullName
                                }
                        )
                        .ToList(),

                Proxies =
                    proxies
            };
        }

                // =====================================================
        // CLINIC INVENTORY FOR PRESCRIBING
        // =====================================================

        public async Task<List<ClinicStockResponseDto>>
            GetClinicStockAsync(
                Guid userId
            )
        {
            var nurse =
                await GetActiveNurseAsync(
                    userId,
                    false
                );

            return await _context.ClinicStocks
                .AsNoTracking()
                .Where(
                    stock =>
                        stock.ClinicId ==
                            nurse.ClinicId &&
                        stock.IsActive &&
                        stock.QuantityOnHand >
                            0
                )
                .OrderBy(
                    stock =>
                        stock.MedicationName
                )
                .ThenBy(
                    stock =>
                        stock.Strength
                )
                .ThenBy(
                    stock =>
                        stock.Form
                )
                .Select(
                    stock =>
                        new ClinicStockResponseDto
                        {
                            Id =
                                stock.Id,

                            ClinicId =
                                stock.ClinicId,

                            ClinicName =
                                stock.Clinic.Name,

                            MedicationName =
                                stock.MedicationName,

                            Strength =
                                stock.Strength,

                            Form =
                                stock.Form,

                            Unit =
                                stock.Unit,

                            QuantityOnHand =
                                stock.QuantityOnHand,

                            ReorderLevel =
                                stock.ReorderLevel,

                            IsActive =
                                stock.IsActive
                        }
                )
                .ToListAsync();
        }

        // =====================================================
        // CLINIC PROXIES
        // =====================================================

        public async Task<List<NurseClinicProxyDto>>
            GetClinicProxiesAsync(
                Guid userId
            )
        {
            var nurse =
                await GetActiveNurseAsync(
                    userId,
                    false
                );

            return await _context.Proxies
                .AsNoTracking()
                .Where(
                    proxy =>
                        proxy.ClinicId ==
                            nurse.ClinicId &&
                        proxy.User.Role ==
                            RoleNames.Proxy &&
                        proxy.User.IsActive
                )
                .OrderBy(
                    proxy =>
                        proxy.User.FullName
                )
                .Select(
                    proxy =>
                        new NurseClinicProxyDto
                        {
                            ProxyId =
                                proxy.Id,

                            FullName =
                                proxy.User
                                    .FullName,

                            PhoneNumber =
                                proxy.User
                                    .PhoneNumber,

                            Email =
                                proxy.User.Email
                        }
                )
                .ToListAsync();
        }

                // =====================================================
        // HEALTH METRICS
        // =====================================================

        public async Task<HealthMetricResponseDto>
            CreateHealthMetricAsync(
                Guid userId,
                Guid patientId,
                NurseHealthMetricWriteDto dto
            )
        {
            var (
                nurse,
                patient
            ) =
                await GetClinicPatientAsync(
                    userId,
                    patientId
                );

            var metricType =
                dto.MetricType
                    .Trim();

            var value =
                dto.Value
                    .Trim();

            if (
                string.IsNullOrWhiteSpace(
                    metricType
                ) ||
                string.IsNullOrWhiteSpace(
                    value
                )
            )
            {
                throw new InvalidOperationException(
                    "Metric type and value are required."
                );
            }

            var metric =
                new HealthMetric
                {
                    Id =
                        Guid.NewGuid(),

                    PatientId =
                        patient.Id,

                    MetricType =
                        metricType,

                    Value =
                        value,

                    Unit =
                        dto.Unit?.Trim() ??
                        string.Empty,

                    Status =
                        CleanOptional(
                            dto.Status
                        ),

                    Note =
                        CleanOptional(
                            dto.Note
                        ),

                    RecordedAt =
                        DateTime.UtcNow
                };

            _context.HealthMetrics.Add(
                metric
            );

            await _context
                .SaveChangesAsync();

            await _audit.LogAsync(
                "PatientHealthMetricRecorded",
                userId,
                $"Health metric {metric.Id} ({metric.MetricType}) recorded for patient {patient.Id}.",
                nurse.ClinicId
            );

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

        // =====================================================
        // ALLERGIES
        // =====================================================

        public async Task<PatientAllergyDto>
            CreateAllergyAsync(
                Guid userId,
                Guid patientId,
                NurseAllergyWriteDto dto
            )
        {
            var (
                nurse,
                patient
            ) =
                await GetClinicPatientAsync(
                    userId,
                    patientId
                );

            var name =
                dto.Name
                    .Trim();

            if (
                string.IsNullOrWhiteSpace(
                    name
                )
            )
            {
                throw new InvalidOperationException(
                    "Allergy name is required."
                );
            }

            var duplicate =
                await _context.Allergies
                    .AsNoTracking()
                    .AnyAsync(
                        allergy =>
                            allergy.PatientId ==
                                patient.Id &&
                            allergy.AllergyName
                                .ToLower() ==
                                name.ToLower()
                    );

            if (
                duplicate
            )
            {
                throw new InvalidOperationException(
                    "That allergy is already recorded for this patient."
                );
            }

            var allergy =
                new Allergy
                {
                    Id =
                        Guid.NewGuid(),

                    PatientId =
                        patient.Id,

                    AllergyName =
                        name,

                    Reaction =
                        CleanOptional(
                            dto.Reaction
                        ),

                    Severity =
                        CleanOptional(
                            dto.Severity
                        ),

                    Notes =
                        CleanOptional(
                            dto.Notes
                        ),

                    RecordedAt =
                        DateTime.UtcNow
                };

            _context.Allergies.Add(
                allergy
            );

            await _context
                .SaveChangesAsync();

            await _audit.LogAsync(
                "PatientAllergyCreated",
                userId,
                $"Allergy {allergy.Id} added to patient {patient.Id}.",
                nurse.ClinicId
            );

            return ToAllergyDto(
                allergy
            );
        }

        public async Task<PatientAllergyDto>
            UpdateAllergyAsync(
                Guid userId,
                Guid patientId,
                Guid allergyId,
                NurseAllergyWriteDto dto
            )
        {
            var (
                nurse,
                patient
            ) =
                await GetClinicPatientAsync(
                    userId,
                    patientId
                );

            var allergy =
                await _context.Allergies
                    .FirstOrDefaultAsync(
                        item =>
                            item.Id ==
                                allergyId &&
                            item.PatientId ==
                                patient.Id
                    );

            if (
                allergy ==
                null
            )
            {
                throw new KeyNotFoundException(
                    "Allergy record not found."
                );
            }

            var name =
                dto.Name
                    .Trim();

            if (
                string.IsNullOrWhiteSpace(
                    name
                )
            )
            {
                throw new InvalidOperationException(
                    "Allergy name is required."
                );
            }

            var duplicate =
                await _context.Allergies
                    .AsNoTracking()
                    .AnyAsync(
                        item =>
                            item.Id !=
                                allergyId &&
                            item.PatientId ==
                                patient.Id &&
                            item.AllergyName
                                .ToLower() ==
                                name.ToLower()
                    );

            if (
                duplicate
            )
            {
                throw new InvalidOperationException(
                    "That allergy is already recorded for this patient."
                );
            }

            allergy.AllergyName =
                name;

            allergy.Reaction =
                CleanOptional(
                    dto.Reaction
                );

            allergy.Severity =
                CleanOptional(
                    dto.Severity
                );

            allergy.Notes =
                CleanOptional(
                    dto.Notes
                );

            await _context
                .SaveChangesAsync();

            await _audit.LogAsync(
                "PatientAllergyUpdated",
                userId,
                $"Allergy {allergy.Id} updated for patient {patient.Id}.",
                nurse.ClinicId
            );

            return ToAllergyDto(
                allergy
            );
        }

        public async Task
            DeleteAllergyAsync(
                Guid userId,
                Guid patientId,
                Guid allergyId
            )
        {
            var (
                nurse,
                patient
            ) =
                await GetClinicPatientAsync(
                    userId,
                    patientId
                );

            var allergy =
                await _context.Allergies
                    .FirstOrDefaultAsync(
                        item =>
                            item.Id ==
                                allergyId &&
                            item.PatientId ==
                                patient.Id
                    );

            if (
                allergy ==
                null
            )
            {
                throw new KeyNotFoundException(
                    "Allergy record not found."
                );
            }

            _context.Allergies.Remove(
                allergy
            );

            await _context
                .SaveChangesAsync();

            await _audit.LogAsync(
                "PatientAllergyRemoved",
                userId,
                $"Allergy {allergyId} removed from patient {patient.Id}.",
                nurse.ClinicId
            );
        }

        // =====================================================
        // CONDITIONS
        // =====================================================

        public async Task<NurseConditionDto>
            CreateConditionAsync(
                Guid userId,
                Guid patientId,
                NurseConditionWriteDto dto
            )
        {
            var (
                nurse,
                patient
            ) =
                await GetClinicPatientAsync(
                    userId,
                    patientId
                );

            var name =
                dto.Name
                    .Trim();

            if (
                string.IsNullOrWhiteSpace(
                    name
                )
            )
            {
                throw new InvalidOperationException(
                    "Condition name is required."
                );
            }

            var condition =
                new MedicalCondition
                {
                    Id =
                        Guid.NewGuid(),

                    PatientId =
                        patient.Id,

                    ConditionName =
                        name,

                    DiagnosisDate =
                        dto.DiagnosisDate,

                    IsChronic =
                        dto.IsChronic,

                    Notes =
                        CleanOptional(
                            dto.Notes
                        ),

                    IsActive =
                        true
                };

            _context
                .MedicalConditions
                .Add(
                    condition
                );

            await _context
                .SaveChangesAsync();

            await _audit.LogAsync(
                "PatientConditionCreated",
                userId,
                $"Condition {condition.Id} added to patient {patient.Id}.",
                nurse.ClinicId
            );

            return ToConditionDto(
                condition
            );
        }

        public async Task<NurseConditionDto>
            UpdateConditionAsync(
                Guid userId,
                Guid patientId,
                Guid conditionId,
                NurseConditionWriteDto dto
            )
        {
            var (
                nurse,
                patient
            ) =
                await GetClinicPatientAsync(
                    userId,
                    patientId
                );

            var condition =
                await _context
                    .MedicalConditions
                    .FirstOrDefaultAsync(
                        item =>
                            item.Id ==
                                conditionId &&
                            item.PatientId ==
                                patient.Id
                    );

            if (
                condition ==
                null
            )
            {
                throw new KeyNotFoundException(
                    "Condition record not found."
                );
            }

            var name =
                dto.Name
                    .Trim();

            if (
                string.IsNullOrWhiteSpace(
                    name
                )
            )
            {
                throw new InvalidOperationException(
                    "Condition name is required."
                );
            }

            condition.ConditionName =
                name;

            condition.DiagnosisDate =
                dto.DiagnosisDate;

            condition.IsChronic =
                dto.IsChronic;

            condition.Notes =
                CleanOptional(
                    dto.Notes
                );

            condition.IsActive =
                dto.IsActive;

            await _context
                .SaveChangesAsync();

            await _audit.LogAsync(
                "PatientConditionUpdated",
                userId,
                $"Condition {condition.Id} updated for patient {patient.Id}.",
                nurse.ClinicId
            );

            return ToConditionDto(
                condition
            );
        }

        public async Task
            ArchiveConditionAsync(
                Guid userId,
                Guid patientId,
                Guid conditionId
            )
        {
            var (
                nurse,
                patient
            ) =
                await GetClinicPatientAsync(
                    userId,
                    patientId
                );

            var condition =
                await _context
                    .MedicalConditions
                    .FirstOrDefaultAsync(
                        item =>
                            item.Id ==
                                conditionId &&
                            item.PatientId ==
                                patient.Id
                    );

            if (
                condition ==
                null
            )
            {
                throw new KeyNotFoundException(
                    "Condition record not found."
                );
            }

            condition.IsActive =
                false;

            await _context
                .SaveChangesAsync();

            await _audit.LogAsync(
                "PatientConditionArchived",
                userId,
                $"Condition {condition.Id} archived for patient {patient.Id}.",
                nurse.ClinicId
            );
        }

        // =====================================================
        // SCHEDULE COLLECTION
        // =====================================================

        public async Task<NurseCollectionDto>
            ScheduleCollectionAsync(
                Guid userId,
                Guid patientId,
                NurseScheduleCollectionDto dto
            )
        {
            var (
                nurse,
                patient
            ) =
                await GetClinicPatientAsync(
                    userId,
                    patientId
                );

            if (
                dto.Quantity <=
                0
            )
            {
                throw new InvalidOperationException(
                    "Collection quantity must be greater than zero."
                );
            }

            if (
                dto.ScheduledCollectionDate
                    .Date <
                DateTime.UtcNow.Date
            )
            {
                throw new InvalidOperationException(
                    "Collection date cannot be in the past."
                );
            }

            var medication =
                await _context
                    .Medications
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        item =>
                            item.Id ==
                                dto.MedicationId &&
                            item.PatientId ==
                                patient.Id &&
                            item.IsActive
                    );

            if (
                medication ==
                null
            )
            {
                throw new KeyNotFoundException(
                    "Active patient medication not found."
                );
            }

            var duplicate =
                await _context
                    .MedicationCollections
                    .AsNoTracking()
                    .AnyAsync(
                        collection =>
                            collection.PatientId ==
                                patient.Id &&
                            collection.ClinicId ==
                                nurse.ClinicId &&
                            collection
                                .ScheduledCollectionDate
                                .Date ==
                                dto
                                    .ScheduledCollectionDate
                                    .Date &&
                            collection.Status !=
                                MedicationCollectionStatuses
                                    .Collected &&
                            collection.Status !=
                                MedicationCollectionStatuses
                                    .Cancelled &&
                            collection.Items
                                .Any(
                                    item =>
                                        item.MedicationId ==
                                            medication.Id
                                )
                    );

            if (
                duplicate
            )
            {
                throw new InvalidOperationException(
                    "This medication already has an active collection scheduled for that date."
                );
            }

            /*
             * The Nurse does not manage stock.
             *
             * This lookup only connects the collection item to
             * the ClinicStock row already configured by the
             * Clinic Admin.
             */
            var stockCandidates =
                await _context.ClinicStocks
                    .AsNoTracking()
                    .Where(
                        stock =>
                            stock.ClinicId ==
                                nurse.ClinicId &&
                            stock.IsActive &&
                            stock.MedicationName
                                .ToLower() ==
                                medication.Name
                                    .Trim()
                                    .ToLower()
                    )
                    .ToListAsync();

            var stock =
                stockCandidates
                    .FirstOrDefault(
                        candidate =>
                            string.Equals(
                                candidate
                                    .Strength
                                    .Trim(),
                                medication
                                    .Dosage
                                    .Trim(),
                                StringComparison
                                    .OrdinalIgnoreCase
                            ) &&
                            string.Equals(
                                candidate
                                    .Form
                                    .Trim(),
                                medication
                                    .Form
                                    .Trim(),
                                StringComparison
                                    .OrdinalIgnoreCase
                            )
                    )
                ??
                stockCandidates
                    .FirstOrDefault(
                        candidate =>
                            string.Equals(
                                candidate
                                    .Form
                                    .Trim(),
                                medication
                                    .Form
                                    .Trim(),
                                StringComparison
                                    .OrdinalIgnoreCase
                            )
                    )
                ??
                (
                    stockCandidates.Count ==
                    1
                        ? stockCandidates[0]
                        : null
                );

            if (
                stock ==
                null
            )
            {
                throw new InvalidOperationException(
                    "No matching clinic stock item is configured for this medication. The Clinic Admin must configure the medication stock before a collection can be scheduled."
                );
            }

            if (
                dto.ProxyId !=
                null
            )
            {
                var validProxy =
                    await _context
                        .ProxyLinks
                        .AsNoTracking()
                        .AnyAsync(
                            link =>
                                link.PatientId ==
                                    patient.Id &&
                                link.ProxyId ==
                                    dto.ProxyId.Value &&
                                link.IsActive &&
                                link.Proxy
                                    .ClinicId ==
                                    nurse.ClinicId &&
                                link.Proxy
                                    .User
                                    .IsActive
                        );

                if (
                    !validProxy
                )
                {
                    throw new InvalidOperationException(
                        "The selected Proxy is not actively assigned to this patient."
                    );
                }
            }

            var collection =
                new MedicationCollection
                {
                    Id =
                        Guid.NewGuid(),

                    PatientId =
                        patient.Id,

                    ClinicId =
                        nurse.ClinicId,

                    ProxyId =
                        dto.ProxyId,

                    ScheduledCollectionDate =
                        dto
                            .ScheduledCollectionDate,

                    Status =
                        MedicationCollectionStatuses
                            .Scheduled,

                    Notes =
                        CleanOptional(
                            dto.Notes
                        ),

                    CreatedAt =
                        DateTime.UtcNow
                };

            collection.Items.Add(
                new MedicationCollectionItem
                {
                    Id =
                        Guid.NewGuid(),

                    MedicationId =
                        medication.Id,

                    ClinicStockId =
                        stock.Id,

                    Quantity =
                        dto.Quantity,

                    Notes =
                        null,

                    CreatedAt =
                        DateTime.UtcNow
                }
            );

            _context
                .MedicationCollections
                .Add(
                    collection
                );

            await _context
                .SaveChangesAsync();

            await _audit.LogAsync(
                "MedicationCollectionCreated",
                userId,
                $"Collection {collection.Id} scheduled for patient {patient.Id}.",
                nurse.ClinicId
            );

            var saved =
                await _context
                    .MedicationCollections
                    .AsNoTrackingWithIdentityResolution()
                    .Include(
                        item =>
                            item.Items
                    )
                    .ThenInclude(
                        item =>
                            item.Medication
                    )
                    .Include(
                        item =>
                            item.Proxy
                    )
                    .ThenInclude(
                        proxy =>
                            proxy!.User
                    )
                    .Include(
                        item =>
                            item.ProcessedByNurse
                    )
                    .ThenInclude(
                        processingNurse =>
                            processingNurse!.User
                    )
                    .FirstAsync(
                        item =>
                            item.Id ==
                                collection.Id
                    );

            return ToCollectionDto(
                saved
            );
        }

        // =====================================================
        // ACTIVE NURSE
        // =====================================================

        private async Task<Nurse>
            GetActiveNurseAsync(
                Guid userId,
                bool tracking
            )
        {
            IQueryable<Nurse> query =
                _context.Nurses
                    .Include(
                        nurse =>
                            nurse.User
                    )
                    .Include(
                        nurse =>
                            nurse.Clinic
                    );

            if (
                !tracking
            )
            {
                query =
                    query.AsNoTracking();
            }

            var nurse =
                await query
                    .FirstOrDefaultAsync(
                        item =>
                            item.UserId ==
                                userId &&
                            item.User.Role ==
                                RoleNames.Nurse &&
                            item.User.IsActive
                    );

            if (
                nurse ==
                null
            )
            {
                throw new UnauthorizedAccessException(
                    "An active Nurse profile with an assigned clinic is required."
                );
            }

            return nurse;
        }

        // =====================================================
        // CLINIC PATIENT ACCESS
        // =====================================================

        private async Task<
            (
                Nurse Nurse,
                Patient Patient
            )
        >
            GetClinicPatientAsync(
                Guid userId,
                Guid patientId
            )
        {
            var nurse =
                await GetActiveNurseAsync(
                    userId,
                    false
                );

            var patient =
                await _context.Patients
                    .AsNoTracking()
                    .Include(
                        item =>
                            item.User
                    )
                    .Include(
                        item =>
                            item.Clinic
                    )
                    .FirstOrDefaultAsync(
                        item =>
                            item.Id ==
                                patientId &&
                            item.User.Role ==
                                RoleNames.Patient &&
                            item.User.IsActive
                    );

            if (
                patient ==
                null
            )
            {
                throw new KeyNotFoundException(
                    "Active patient not found."
                );
            }

            if (
                patient.ClinicId !=
                nurse.ClinicId
            )
            {
                throw new UnauthorizedAccessException(
                    "Patient does not belong to your clinic."
                );
            }

            return (
                nurse,
                patient
            );
        }

        // =====================================================
        // MAPPING
        // =====================================================

        private static NurseMeDto
            ToMeDto(
                Nurse nurse
            )
        {
            return new NurseMeDto
            {
                NurseId =
                    nurse.Id,

                UserId =
                    nurse.UserId,

                FullName =
                    nurse.User.FullName,

                IdNumber =
                    nurse.User.IdNumber,

                PhoneNumber =
                    nurse
                        .User
                        .PhoneNumber,

                Email =
                    nurse.User.Email,

                EmployeeNumber =
                    nurse.EmployeeNumber,

                RegistrationNumber =
                    nurse.RegistrationNumber,

                Qualification =
                    nurse.Qualification,

                ClinicId =
                    nurse.ClinicId,

                ClinicName =
                    nurse.Clinic.Name,

                AddressLine1 =
                    nurse.AddressLine1,

                AddressLine2 =
                    nurse.AddressLine2,

                Suburb =
                    nurse.Suburb,

                City =
                    nurse.City,

                Province =
                    nurse.Province,

                PostalCode =
                    nurse.PostalCode,

                DateOfBirth =
                    nurse.DateOfBirth,

                Gender =
                    nurse.Gender,

                EmploymentDate =
                    nurse.EmploymentDate,

                EmergencyContactName =
                    nurse
                        .EmergencyContactName,

                EmergencyContactPhone =
                    nurse
                        .EmergencyContactPhone,

                EmergencyContactRelationship =
                    nurse
                        .EmergencyContactRelationship,

                IsActive =
                    nurse.User.IsActive,

                IsVerified =
                    nurse.User.IsVerified,

                MustChangePassword =
                    nurse
                        .User
                        .MustChangePassword,

                CreatedAt =
                    nurse.CreatedAt,

                UpdatedAt =
                    nurse.UpdatedAt
            };
        }

        private static PatientAllergyDto
            ToAllergyDto(
                Allergy allergy
            )
        {
            return new PatientAllergyDto
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
            };
        }

        private static NurseConditionDto
            ToConditionDto(
                MedicalCondition condition
            )
        {
            return new NurseConditionDto
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
                    condition.Notes,

                IsActive =
                    condition.IsActive
            };
        }

        private static NurseCollectionDto
            ToCollectionDto(
                MedicationCollection collection
            )
        {
            var status =
                collection.Status;

            if (
                collection.Status !=
                    MedicationCollectionStatuses
                        .Collected &&
                collection.Status !=
                    MedicationCollectionStatuses
                        .Cancelled
            )
            {
                status =
                    collection
                        .ScheduledCollectionDate
                        .Date <
                    DateTime.UtcNow.Date
                        ? MedicationCollectionStatuses
                            .Overdue
                        : MedicationCollectionStatuses
                            .Pending;
            }

            return new NurseCollectionDto
            {
                Id =
                    collection.Id,

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
                        collection
                            .Items
                            .Select(
                                item =>
                                    item
                                        .Medication
                                        .Name
                            )
                            .Distinct()
                    ),

                Quantity =
                    collection
                        .Items
                        .Sum(
                            item =>
                                item.Quantity
                        ),

                ProxyId =
                    collection.ProxyId,

                ProxyName =
                    collection
                        .Proxy?
                        .User
                        .FullName,

                ProcessedByNurseId =
                    collection
                        .ProcessedByNurseId,

                ProcessedByNurseName =
                    collection
                        .ProcessedByNurse?
                        .User
                        .FullName,

                Notes =
                    collection.Notes
            };
        }

        private static string?
            CleanOptional(
                string? value
            )
        {
            return string.IsNullOrWhiteSpace(
                value
            )
                ? null
                : value.Trim();
        }
    }
}
