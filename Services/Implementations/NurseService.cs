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
        private readonly PhilaLinkDbContext _context;

        public NurseService(
            PhilaLinkDbContext context
        )
        {
            _context = context;
        }

        public async Task<NurseMeDto> GetMeAsync(
            Guid userId
        )
        {
            var nurse =
                await GetActiveNurseAsync(
                    userId
                );

            await _context.Entry(nurse)
                .Reference(n => n.Clinic)
                .LoadAsync();

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
                today.AddDays(1);

            var clinicPatients =
                await _context.Patients
                    .CountAsync(
                        p =>
                            p.ClinicId ==
                                clinicId &&
                            p.User.IsActive
                    );

            var appointmentsToday =
                await _context.Appointments
                    .CountAsync(
                        a =>
                            a.ClinicId ==
                                clinicId &&
                            a.ScheduledAt >=
                                today &&
                            a.ScheduledAt <
                                tomorrow &&
                            a.Status !=
                                AppointmentStatuses.Cancelled
                    );

            var collectionsDueToday =
                await _context
                    .MedicationCollections
                    .CountAsync(
                        c =>
                            c.ClinicId ==
                                clinicId &&
                            c.ScheduledCollectionDate >=
                                today &&
                            c.ScheduledCollectionDate <
                                tomorrow &&
                            c.Status !=
                                MedicationCollectionStatuses.Collected &&
                            c.Status !=
                                MedicationCollectionStatuses.Cancelled
                    );

            var overdueCollections =
                await _context
                    .MedicationCollections
                    .CountAsync(
                        c =>
                            c.ClinicId ==
                                clinicId &&
                            c.ScheduledCollectionDate <
                                today &&
                            c.Status !=
                                MedicationCollectionStatuses.Collected &&
                            c.Status !=
                                MedicationCollectionStatuses.Cancelled
                    );

            var lowStockItems =
                await _context.ClinicStocks
                    .CountAsync(
                        s =>
                            s.ClinicId ==
                                clinicId &&
                            s.IsActive &&
                            s.QuantityOnHand <=
                                s.ReorderLevel
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
                    overdueCollections,

                LowStockItems =
                    lowStockItems
            };
        }

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
                .Include(p => p.User)
                .Where(
                    p =>
                        p.ClinicId ==
                            nurse.ClinicId &&
                        p.User.IsActive
                )
                .OrderBy(
                    p => p.User.FullName
                )
                .Select(
                    p =>
                        new NursePatientDto
                        {
                            PatientId =
                                p.Id,

                            UserId =
                                p.UserId,

                            PatientNumber =
                                p.PatientNumber,

                            FullName =
                                p.User.FullName,

                            DateOfBirth =
                                p.DateOfBirth,

                            Gender =
                                p.Gender,

                            PhoneNumber =
                                p.User.PhoneNumber
                        }
                )
                .ToListAsync();
        }

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
                    .CountAsync(
                        c =>
                            c.ClinicId ==
                                clinicId &&
                            c.ScheduledCollectionDate <
                                today &&
                            c.Status !=
                                MedicationCollectionStatuses.Collected &&
                            c.Status !=
                                MedicationCollectionStatuses.Cancelled
                    );

            var lowStockItems =
                await _context.ClinicStocks
                    .CountAsync(
                        s =>
                            s.ClinicId ==
                                clinicId &&
                            s.IsActive &&
                            s.QuantityOnHand <=
                                s.ReorderLevel
                    );

            var alerts =
                new List<NurseAlertDto>();

            if (overdueCollections > 0)
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

            if (lowStockItems > 0)
            {
                alerts.Add(
                    new NurseAlertDto
                    {
                        Code =
                            "LOW_STOCK",

                        Severity =
                            "Medium",

                        Count =
                            lowStockItems,

                        Message =
                            $"{lowStockItems} clinic stock item(s) are at or below reorder level."
                    }
                );
            }

            return alerts;
        }

        private async Task<Nurse>
            GetActiveNurseAsync(
                Guid userId
            )
        {
            var nurse =
                await _context.Nurses
                    .Include(n => n.User)
                    .Include(n => n.Clinic)
                    .FirstOrDefaultAsync(
                        n =>
                            n.UserId ==
                                userId &&
                            n.User.Role ==
                                RoleNames.Nurse &&
                            n.User.IsActive
                    );

            if (nurse == null)
            {
                throw new UnauthorizedAccessException(
                    "Active Nurse account required."
                );
            }

            return nurse;
        }
    }
}