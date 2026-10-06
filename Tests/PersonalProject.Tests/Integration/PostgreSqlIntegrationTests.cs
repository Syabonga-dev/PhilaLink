using Microsoft.EntityFrameworkCore;
using PersonalProject.Models.Constants;
using PersonalProject.Tests.Support;
using Xunit;

namespace PersonalProject.Tests.Integration
{
    public class PostgreSqlIntegrationTests
    {
        [PostgreSqlFact]
        public async Task
            FreshPostgreSqlDatabaseCanApplyAllMigrations()
        {
            await using var database =
                await PostgreSqlTestDatabase
                    .CreateAsync();

            var db =
                database.Context;

            Assert.Equal(
                "Npgsql.EntityFrameworkCore.PostgreSQL",
                db.Database.ProviderName
            );

            var pending =
                await db.Database
                    .GetPendingMigrationsAsync();

            Assert.Empty(
                pending
            );

            var applied =
                await db.Database
                    .GetAppliedMigrationsAsync();

            Assert.NotEmpty(
                applied
            );

            /*
             * Proves that the fully migrated database can
             * actually execute normal application queries.
             */
            var userCount =
                await db.Users
                    .CountAsync();

            Assert.Equal(
                0,
                userCount
            );
        }

        [PostgreSqlFact]
        public async Task
            PostgreSqlEnforcesUniqueUserEmailConstraint()
        {
            await using var database =
                await PostgreSqlTestDatabase
                    .CreateAsync();

            var db =
                database.Context;

            var firstUser =
                TestDataFactory.User(
                    RoleNames.Patient,
                    "First Patient",
                    "1701"
                );

            firstUser.Email =
                "duplicate@philalink.test";

            db.Users.Add(
                firstUser
            );

            await db.SaveChangesAsync();

            var duplicateUser =
                TestDataFactory.User(
                    RoleNames.Patient,
                    "Second Patient",
                    "1702"
                );

            /*
             * ID number and phone remain different.
             * Only the email duplicates the first user.
             */
            duplicateUser.Email =
                firstUser.Email;

            db.Users.Add(
                duplicateUser
            );

            await Assert.ThrowsAsync<
                DbUpdateException
            >(
                () =>
                    db.SaveChangesAsync()
            );
        }

        [PostgreSqlFact]
        public async Task
            PostgreSqlEnforcesUniquePatientNumberConstraint()
        {
            await using var database =
                await PostgreSqlTestDatabase
                    .CreateAsync();

            var db =
                database.Context;

            var clinic =
                TestDataFactory.Clinic(
                    "PostgreSQL Clinic"
                );

            var firstUser =
                TestDataFactory.User(
                    RoleNames.Patient,
                    "Patient One",
                    "1711"
                );

            var firstPatient =
                TestDataFactory.Patient(
                    firstUser,
                    clinic,
                    "PHL-POSTGRES-001"
                );

            db.AddRange(
                clinic,
                firstUser,
                firstPatient
            );

            await db.SaveChangesAsync();

            var secondUser =
                TestDataFactory.User(
                    RoleNames.Patient,
                    "Patient Two",
                    "1712"
                );

            var secondPatient =
                TestDataFactory.Patient(
                    secondUser,
                    clinic,
                    "PHL-POSTGRES-001"
                );

            db.AddRange(
                secondUser,
                secondPatient
            );

            await Assert.ThrowsAsync<
                DbUpdateException
            >(
                () =>
                    db.SaveChangesAsync()
            );
        }

        [PostgreSqlFact]
        public async Task
            PostgreSqlRejectsDeletingClinicReferencedByPatient()
        {
            await using var database =
                await PostgreSqlTestDatabase
                    .CreateAsync();

            var db =
                database.Context;

            var clinic =
                TestDataFactory.Clinic(
                    "Referenced Clinic"
                );

            var patientUser =
                TestDataFactory.User(
                    RoleNames.Patient,
                    "Patient One",
                    "1721"
                );

            var patient =
                TestDataFactory.Patient(
                    patientUser,
                    clinic,
                    "PHL-POSTGRES-1721"
                );

            db.AddRange(
                clinic,
                patientUser,
                patient
            );

            await db.SaveChangesAsync();

            /*
             * Clear tracked relationship objects so EF cannot
             * satisfy or reject the relationship entirely in
             * memory.
             *
             * The DELETE is therefore sent to PostgreSQL and
             * PostgreSQL's actual FK restriction is responsible
             * for rejecting it.
             */
            db.ChangeTracker
                .Clear();

            var persistedClinic =
                await db.Clinics
                    .SingleAsync(
                        item =>
                            item.Id ==
                            clinic.Id
                    );

            db.Clinics.Remove(
                persistedClinic
            );

            await Assert.ThrowsAsync<
                DbUpdateException
            >(
                () =>
                    db.SaveChangesAsync()
            );
        }
    }
}
