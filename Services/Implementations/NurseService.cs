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

        public NurseService(
            PhilaLinkDbContext context
        )
        {
            _context =
                context;
        }

        // =====================================================
        // CURRENT NURSE
        // =====================================================

        public async Task<NurseMeDto>
            GetMeAsync(
                Guid userId
            )
        {
            var nurse =
                await GetActiveNurseAsync(
                    userId
                );

            return new NurseMeDto
            {
                NurseId =
                    nurse.Id,

                UserId =
                    nurse.UserId,

                FullName =
                    nurse.User.FullName,

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

                Email =
                    nurse.Email
            };
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
                    userId
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

            var collectionsDueToday =
                await _context
                    .MedicationCollections
                    .AsNoTracking()
                    .CountAsync(
                        collection =>
                            collection.ClinicId ==
                                clinicId &&
                            collection.ScheduledCollectionDate >=
                                today &&
                            collection.ScheduledCollectionDate <
                                tomorrow &&
                            collection.Status !=
                                MedicationCollectionStatuses.Collected &&
                            collection.Status !=
                                MedicationCollectionStatuses.Cancelled
                    );

            var overdueCollections =
                await _context
                    .MedicationCollections
                    .AsNoTracking()
                    .CountAsync(
                        collection =>
                            collection.ClinicId ==
                                clinicId &&
                            collection.ScheduledCollectionDate <
                                today &&
                            collection.Status !=
                                MedicationCollectionStatuses.Collected &&
                            collection.Status !=
                                MedicationCollectionStatuses.Cancelled
                    );

            return new NurseDashboardDto
            {
                ClinicPatients =
                    clinicPatients,

                AppointmentsToday =
                    appointmentsToday,

                CollectionsDueToday =
                    collectionsDueToday,

                OverdueCollections =
                    overdueCollections
            };
        }

        // =====================================================
        // CLINIC PATIENTS
        // =====================================================

        public async Task<List<NursePatientDto>>
            GetClinicPatientsAsync(
                Guid userId
            )
        {
            var nurse =
                await GetActiveNurseAsync(
                    userId
                );

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
                                patient.User.PhoneNumber
                        }
                )
                .ToListAsync();
        }

        // =====================================================
        // URGENT NURSE ALERTS
        // =====================================================
        //
        // Stock alerts deliberately do not live here.
        //
        // Clinic inventory and reorder monitoring are the
        // responsibility of the ClinicAdmin.
        //
        // Nurse alerts should concern direct patient-care work.
        // =====================================================

        public async Task<List<NurseAlertDto>>
            GetUrgentAlertsAsync(
                Guid userId
            )
        {
            var nurse =
                await GetActiveNurseAsync(
                    userId
                );

            var clinicId =
                nurse.ClinicId;

            var today =
                DateTime.UtcNow.Date;

            var overdueCollections =
                await _context
                    .MedicationCollections
                    .AsNoTracking()
                    .CountAsync(
                        collection =>
                            collection.ClinicId ==
                                clinicId &&
                            collection.ScheduledCollectionDate <
                                today &&
                            collection.Status !=
                                MedicationCollectionStatuses.Collected &&
                            collection.Status !=
                                MedicationCollectionStatuses.Cancelled
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

            return alerts;
        }

        // =====================================================
        // ACTIVE NURSE
        // =====================================================

        private async Task<Nurse>
            GetActiveNurseAsync(
                Guid userId
            )
        {
            var nurse =
                await _context.Nurses
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
    }
}