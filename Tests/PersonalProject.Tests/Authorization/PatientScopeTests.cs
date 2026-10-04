using PersonalProject.Models.Constants;
using PersonalProject.Services.Implementations;
using PersonalProject.Tests.Support;
using Xunit;

namespace PersonalProject.Tests.Authorization
{
    public class PatientScopeTests
    {
        [Fact]
        public async Task
            PatientProfileIsResolvedFromAuthenticatedUserId()
        {
            await using var db =
                TestDb.Create();

            var clinic =
                TestDataFactory.Clinic(
                    "Clinic A"
                );

            var patientAUser =
                TestDataFactory.User(
                    RoleNames.Patient,
                    "Patient A",
                    "201"
                );

            var patientA =
                TestDataFactory.Patient(
                    patientAUser,
                    clinic,
                    "PHL-TEST-201"
                );

            var patientBUser =
                TestDataFactory.User(
                    RoleNames.Patient,
                    "Patient B",
                    "202"
                );

            var patientB =
                TestDataFactory.Patient(
                    patientBUser,
                    clinic,
                    "PHL-TEST-202"
                );

            db.AddRange(
                clinic,
                patientAUser,
                patientA,
                patientBUser,
                patientB
            );

            await db.SaveChangesAsync();

            /*
             * GetMeAsync does not accept an arbitrary
             * PatientId.
             *
             * It receives the authenticated UserId and
             * resolves that user's Patient profile.
             */
            var service =
                new PatientService(
                    db,
                    null!,
                    null!
                );

            var result =
                await service
                    .GetMeAsync(
                        patientAUser.Id
                    );

            Assert.Equal(
                patientA.Id,
                result.PatientId
            );

            Assert.Equal(
                patientAUser.Id,
                result.UserId
            );

            Assert.NotEqual(
                patientB.Id,
                result.PatientId
            );
        }

        [Fact]
        public async Task
            NonPatientUserCannotResolvePatientProfile()
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
                    "Nurse A",
                    "203"
                );

            db.AddRange(
                clinic,
                nurseUser
            );

            await db.SaveChangesAsync();

            var service =
                new PatientService(
                    db,
                    null!,
                    null!
                );

            await Assert.ThrowsAsync<
                UnauthorizedAccessException
            >(
                () =>
                    service
                        .GetMeAsync(
                            nurseUser.Id
                        )
            );
        }
    }
}
