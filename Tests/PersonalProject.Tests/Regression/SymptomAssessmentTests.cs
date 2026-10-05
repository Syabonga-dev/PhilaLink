using Microsoft.EntityFrameworkCore;
using PersonalProject.Models.Constants;
using PersonalProject.Models.DTOs;
using PersonalProject.Services.Implementations;
using PersonalProject.Tests.Support;
using Xunit;

namespace PersonalProject.Tests.Regression
{
    public class SymptomAssessmentTests
    {
        [Fact]
        public async Task
            EmergencyPhraseProducesEmergencyResult()
        {
            await using var db =
                TestDb.Create();

            var clinic =
                TestDataFactory.Clinic(
                    "Clinic A"
                );

            var user =
                TestDataFactory.User(
                    RoleNames.Patient,
                    "Patient One",
                    "701"
                );

            var patient =
                TestDataFactory.Patient(
                    user,
                    clinic,
                    "PHL-TEST-701"
                );

            db.AddRange(
                clinic,
                user,
                patient
            );

            await db.SaveChangesAsync();

            var service =
                new SymptomAssessmentService(
                    db
                );

            var result =
                await service
                    .CreateForPatientAsync(
                        user.Id,
                        new SymptomCreateDto
                        {
                            Symptoms =
                                "I have chest pain."
                        }
                    );

            Assert.Equal(
                "Emergency",
                result.Result
            );

            Assert.Equal(
                1,
                await db
                    .SymptomAssessments
                    .CountAsync()
            );
        }

        [Fact]
        public async Task
            MildSymptomsProduceNonEmergencyResult()
        {
            await using var db =
                TestDb.Create();

            var clinic =
                TestDataFactory.Clinic(
                    "Clinic A"
                );

            var user =
                TestDataFactory.User(
                    RoleNames.Patient,
                    "Patient One",
                    "702"
                );

            var patient =
                TestDataFactory.Patient(
                    user,
                    clinic,
                    "PHL-TEST-702"
                );

            db.AddRange(
                clinic,
                user,
                patient
            );

            await db.SaveChangesAsync();

            var service =
                new SymptomAssessmentService(
                    db
                );

            var result =
                await service
                    .CreateForPatientAsync(
                        user.Id,
                        new SymptomCreateDto
                        {
                            Symptoms =
                                "Mild runny nose and sneezing."
                        }
                    );

            Assert.Equal(
                "NonEmergency",
                result.Result
            );
        }

        [Fact]
        public async Task
            InvalidAgeIsRejected()
        {
            await using var db =
                TestDb.Create();

            var service =
                new SymptomAssessmentService(
                    db
                );

            await Assert.ThrowsAsync<
                ArgumentException
            >(
                () =>
                    service
                        .CreateForPatientAsync(
                            Guid.NewGuid(),
                            new SymptomCreateDto
                            {
                                Symptoms =
                                    "Headache",

                                Age =
                                    121
                            }
                        )
            );
        }
    }
}
