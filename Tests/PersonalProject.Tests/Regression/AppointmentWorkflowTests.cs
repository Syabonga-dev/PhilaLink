using Microsoft.Extensions.Logging.Abstractions;
using PersonalProject.Models.Constants;
using PersonalProject.Models.DTOs;
using PersonalProject.Services.Implementations;
using PersonalProject.Tests.Support;
using Xunit;

namespace PersonalProject.Tests.Regression
{
    public class AppointmentWorkflowTests
    {
        [Fact]
        public async Task
            NurseCanCreateAppointmentForOwnClinicPatient()
        {
            await using var db =
                TestDb.Create();

            var clinic =
                TestDataFactory.Clinic(
                    "Clinic A"
                );

            var nurseUser =
                TestDataFactory.User(
                    RoleNames.Nurse,
                    "Nurse One",
                    "801"
                );

            var nurse =
                TestDataFactory.Nurse(
                    nurseUser,
                    clinic,
                    "801"
                );

            var patientUser =
                TestDataFactory.User(
                    RoleNames.Patient,
                    "Patient One",
                    "802"
                );

            var patient =
                TestDataFactory.Patient(
                    patientUser,
                    clinic,
                    "PHL-TEST-802"
                );

            db.AddRange(
                clinic,
                nurseUser,
                nurse,
                patientUser,
                patient
            );

            await db.SaveChangesAsync();

            var service =
                CreateService(
                    db
                );

            var result =
                await service.CreateAsync(
                    new CreateAppointmentDto
                    {
                        PatientId =
                            patient.Id,

                        ClinicId =
                            clinic.Id,

                        NurseId =
                            nurse.Id,

                        ScheduledAt =
                            DateTime.UtcNow
                                .AddDays(
                                    2
                                ),

                        DurationMinutes =
                            30,

                        Type =
                            "FollowUp",

                        Reason =
                            "Routine review",

                        Mode =
                            "InPerson"
                    },
                    nurseUser.Id
                );

            Assert.Equal(
                patient.Id,
                result.PatientId
            );

            Assert.Equal(
                clinic.Id,
                result.ClinicId
            );

            Assert.Equal(
                nurse.Id,
                result.NurseId
            );

            Assert.Equal(
                AppointmentStatuses
                    .Scheduled,
                result.Status
            );
        }

        [Fact]
        public async Task
            NurseCannotCreateAppointmentForOtherClinicPatient()
        {
            await using var db =
                TestDb.Create();

            var clinicA =
                TestDataFactory.Clinic(
                    "Clinic A"
                );

            var clinicB =
                TestDataFactory.Clinic(
                    "Clinic B"
                );

            var nurseUser =
                TestDataFactory.User(
                    RoleNames.Nurse,
                    "Nurse One",
                    "803"
                );

            var nurse =
                TestDataFactory.Nurse(
                    nurseUser,
                    clinicA,
                    "803"
                );

            var patientUser =
                TestDataFactory.User(
                    RoleNames.Patient,
                    "Patient Two",
                    "804"
                );

            var patient =
                TestDataFactory.Patient(
                    patientUser,
                    clinicB,
                    "PHL-TEST-804"
                );

            db.AddRange(
                clinicA,
                clinicB,
                nurseUser,
                nurse,
                patientUser,
                patient
            );

            await db.SaveChangesAsync();

            var service =
                CreateService(
                    db
                );

            await Assert.ThrowsAsync<
                UnauthorizedAccessException
            >(
                () =>
                    service.CreateAsync(
                        new CreateAppointmentDto
                        {
                            PatientId =
                                patient.Id,

                            ClinicId =
                                clinicA.Id,

                            ScheduledAt =
                                DateTime.UtcNow
                                    .AddDays(
                                        2
                                    ),

                            DurationMinutes =
                                30,

                            Type =
                                "FollowUp",

                            Reason =
                                "Routine review",

                            Mode =
                                "InPerson"
                        },
                        nurseUser.Id
                    )
            );
        }

        private static AppointmentService
            CreateService(
                PersonalProject.Data
                    .PhilaLinkDbContext db
            )
        {
            return new AppointmentService(
                db,
                NoOpAuditLogService
                    .Instance,
                NoOpNotificationService
                    .Instance,
                NullLogger<
                    AppointmentService
                >.Instance
            );
        }
    }
}
