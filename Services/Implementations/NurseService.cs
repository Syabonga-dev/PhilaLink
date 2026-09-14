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

        public async Task<NurseMeDto> GetMeAsync(Guid userId)
        {
            var nurse =await GetActiveNurseAsync(userId);

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
                                "Cancelled"
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
                                "Collected" &&
                            c.Status !=
                                "Cancelled"
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
                                "Collected" &&
                            c.Status !=
                                "Cancelled"
                    );

            /*
             * For now "low stock" means quantity on hand
             * is at or below the configured reorder level.
             *
             * This uses the existing stock model instead
             * of inventing another supply system.
             */
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

        public async Task<
            List<NursePatientDto>>
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
                    p => new NursePatientDto
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