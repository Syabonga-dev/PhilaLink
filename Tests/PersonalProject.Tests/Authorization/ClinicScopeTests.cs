using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using PersonalProject.Models.Constants;
using PersonalProject.Models.DTOs;
using PersonalProject.Models.Entities;
using PersonalProject.Services.Implementations;
using PersonalProject.Tests.Support;
using Xunit;

namespace PersonalProject.Tests.Authorization
{
    public class ClinicScopeTests
    {
        [Fact]
        public async Task
            NurseCannotAccessPatientFromAnotherClinic()
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
                    "Nurse A",
                    "101"
                );

            var nurse =
                TestDataFactory.Nurse(
                    nurseUser,
                    clinicA,
                    "101"
                );

            var patientUser =
                TestDataFactory.User(
                    RoleNames.Patient,
                    "Patient B",
                    "102"
                );

            var patient =
                TestDataFactory.Patient(
                    patientUser,
                    clinicB,
                    "PHL-TEST-102"
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
                new NurseService(
                    db,
                    NoOpAuditLogService
                        .Instance
                );

            await Assert.ThrowsAsync<
                UnauthorizedAccessException
            >(
                () =>
                    service
                        .GetPatientCareAsync(
                            nurseUser.Id,
                            patient.Id
                        )
            );
        }

        [Fact]
        public async Task
            NurseCanAccessPatientFromOwnClinic()
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
                    "103"
                );

            var nurse =
                TestDataFactory.Nurse(
                    nurseUser,
                    clinic,
                    "103"
                );

            var patientUser =
                TestDataFactory.User(
                    RoleNames.Patient,
                    "Patient A",
                    "104"
                );

            var patient =
                TestDataFactory.Patient(
                    patientUser,
                    clinic,
                    "PHL-TEST-104"
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
                new NurseService(
                    db,
                    NoOpAuditLogService
                        .Instance
                );

            var result =
                await service
                    .GetPatientCareAsync(
                        nurseUser.Id,
                        patient.Id
                    );

            Assert.Equal(
                patient.Id,
                result.PatientId
            );
        }

        [Fact]
        public async Task
            ProxyOnlySeesActiveLinksFromOwnClinic()
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

            var proxyUser =
                TestDataFactory.User(
                    RoleNames.Proxy,
                    "Proxy A",
                    "105"
                );

            var proxy =
                TestDataFactory.Proxy(
                    proxyUser,
                    clinicA
                );

            var activePatientUser =
                TestDataFactory.User(
                    RoleNames.Patient,
                    "Active Patient",
                    "106"
                );

            var activePatient =
                TestDataFactory.Patient(
                    activePatientUser,
                    clinicA,
                    "PHL-TEST-106"
                );

            var inactivePatientUser =
                TestDataFactory.User(
                    RoleNames.Patient,
                    "Inactive Link Patient",
                    "107"
                );

            var inactivePatient =
                TestDataFactory.Patient(
                    inactivePatientUser,
                    clinicA,
                    "PHL-TEST-107"
                );

            var otherClinicUser =
                TestDataFactory.User(
                    RoleNames.Patient,
                    "Cross Clinic Patient",
                    "108"
                );

            var otherClinicPatient =
                TestDataFactory.Patient(
                    otherClinicUser,
                    clinicB,
                    "PHL-TEST-108"
                );

            var activeLink =
                new ProxyLink
                {
                    Id =
                        Guid.NewGuid(),

                    PatientId =
                        activePatient.Id,

                    Patient =
                        activePatient,

                    ProxyId =
                        proxy.Id,

                    Proxy =
                        proxy,

                    AssignedAt =
                        DateTime.UtcNow,

                    IsActive =
                        true
                };

            var inactiveLink =
                new ProxyLink
                {
                    Id =
                        Guid.NewGuid(),

                    PatientId =
                        inactivePatient.Id,

                    Patient =
                        inactivePatient,

                    ProxyId =
                        proxy.Id,

                    Proxy =
                        proxy,

                    AssignedAt =
                        DateTime.UtcNow,

                    IsActive =
                        false,

                    EndedAt =
                        DateTime.UtcNow
                };

            /*
             * Deliberately seed an invalid historical
             * cross-clinic relationship.
             *
             * The service must still refuse to expose it.
             */
            var crossClinicLink =
                new ProxyLink
                {
                    Id =
                        Guid.NewGuid(),

                    PatientId =
                        otherClinicPatient.Id,

                    Patient =
                        otherClinicPatient,

                    ProxyId =
                        proxy.Id,

                    Proxy =
                        proxy,

                    AssignedAt =
                        DateTime.UtcNow,

                    IsActive =
                        true
                };

            db.AddRange(
                clinicA,
                clinicB,
                proxyUser,
                proxy,
                activePatientUser,
                activePatient,
                inactivePatientUser,
                inactivePatient,
                otherClinicUser,
                otherClinicPatient,
                activeLink,
                inactiveLink,
                crossClinicLink
            );

            await db.SaveChangesAsync();

            var service =
                new ProxyService(
                    db,
                    NoOpAuditLogService
                        .Instance
                );

            var result =
                await service
                    .GetMyPatientsAsync(
                        proxyUser.Id
                    );

            Assert.Single(
                result
            );

            Assert.Equal(
                activePatient.Id,
                result[0]
                    .PatientId
            );
        }

        [Fact]
        public async Task
            EndingProxyLinkImmediatelyRemovesAccess()
        {
            await using var db =
                TestDb.Create();

            var clinic =
                TestDataFactory.Clinic(
                    "Clinic A"
                );

            var proxyUser =
                TestDataFactory.User(
                    RoleNames.Proxy,
                    "Proxy A",
                    "109"
                );

            var proxy =
                TestDataFactory.Proxy(
                    proxyUser,
                    clinic
                );

            var patientUser =
                TestDataFactory.User(
                    RoleNames.Patient,
                    "Patient A",
                    "110"
                );

            var patient =
                TestDataFactory.Patient(
                    patientUser,
                    clinic,
                    "PHL-TEST-110"
                );

            var link =
                new ProxyLink
                {
                    Id =
                        Guid.NewGuid(),

                    PatientId =
                        patient.Id,

                    Patient =
                        patient,

                    ProxyId =
                        proxy.Id,

                    Proxy =
                        proxy,

                    AssignedAt =
                        DateTime.UtcNow,

                    IsActive =
                        true
                };

            db.AddRange(
                clinic,
                proxyUser,
                proxy,
                patientUser,
                patient,
                link
            );

            await db.SaveChangesAsync();

            var service =
                new ProxyService(
                    db,
                    NoOpAuditLogService
                        .Instance
                );

            var before =
                await service
                    .GetMyPatientsAsync(
                        proxyUser.Id
                    );

            Assert.Single(
                before
            );

            link.IsActive =
                false;

            link.EndedAt =
                DateTime.UtcNow;

            await db.SaveChangesAsync();

            var after =
                await service
                    .GetMyPatientsAsync(
                        proxyUser.Id
                    );

            Assert.Empty(
                after
            );
        }

        [Fact]
        public async Task
            ClinicAdminOnlyListsNursesFromOwnClinic()
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

            var adminUser =
                TestDataFactory.User(
                    RoleNames.ClinicAdmin,
                    "Clinic Admin A",
                    "111"
                );

            var admin =
                TestDataFactory.ClinicAdmin(
                    adminUser,
                    clinicA
                );

            var ownNurseUser =
                TestDataFactory.User(
                    RoleNames.Nurse,
                    "Own Clinic Nurse",
                    "112"
                );

            var ownNurse =
                TestDataFactory.Nurse(
                    ownNurseUser,
                    clinicA,
                    "112"
                );

            var otherNurseUser =
                TestDataFactory.User(
                    RoleNames.Nurse,
                    "Other Clinic Nurse",
                    "113"
                );

            var otherNurse =
                TestDataFactory.Nurse(
                    otherNurseUser,
                    clinicB,
                    "113"
                );

            db.AddRange(
                clinicA,
                clinicB,
                adminUser,
                admin,
                ownNurseUser,
                ownNurse,
                otherNurseUser,
                otherNurse
            );

            await db.SaveChangesAsync();

            var service =
                new AdminService(
                    db,
                    new ConfigurationBuilder()
                        .Build(),
                    NullLogger<
                        AdminService
                    >.Instance
                );

            var result =
                await service
                    .ListAccountsAsync(
                        RoleNames.Nurse,
                        adminUser.Id
                    );

            Assert.Single(
                result
            );

            Assert.Equal(
                ownNurseUser.Id,
                result[0]
                    .UserId
            );
        }

        [Fact]
        public async Task
            UnassignedClinicAdminCannotUseClinicScope()
        {
            await using var db =
                TestDb.Create();

            var adminUser =
                TestDataFactory.User(
                    RoleNames.ClinicAdmin,
                    "Unassigned Admin",
                    "114"
                );

            var admin =
                TestDataFactory.ClinicAdmin(
                    adminUser,
                    null
                );

            db.AddRange(
                adminUser,
                admin
            );

            await db.SaveChangesAsync();

            var service =
                new AdminService(
                    db,
                    new ConfigurationBuilder()
                        .Build(),
                    NullLogger<
                        AdminService
                    >.Instance
                );

            await Assert.ThrowsAsync<
                InvalidOperationException
            >(
                () =>
                    service
                        .GetClinicOverviewAsync(
                            adminUser.Id
                        )
            );
        }

        [Fact]
        public async Task
            SuperAdminCannotRegisterNurse()
        {
            await using var db =
                TestDb.Create();

            var superAdminUser =
                TestDataFactory.User(
                    RoleNames.SuperAdmin,
                    "System Admin",
                    "115"
                );

            var superAdmin =
                TestDataFactory.SuperAdmin(
                    superAdminUser
                );

            db.AddRange(
                superAdminUser,
                superAdmin
            );

            await db.SaveChangesAsync();

            var service =
                new AdminService(
                    db,
                    new ConfigurationBuilder()
                        .Build(),
                    NullLogger<
                        AdminService
                    >.Instance
                );

            var dto =
                new RegisterNurseDto
                {
                    ClinicId =
                        Guid.NewGuid()
                };

            await Assert.ThrowsAsync<
                UnauthorizedAccessException
            >(
                () =>
                    service
                        .RegisterNurseAsync(
                            dto,
                            superAdminUser.Id
                        )
            );
        }

        [Fact]
        public async Task
            ClinicAdminCannotRegisterClinicAdmin()
        {
            await using var db =
                TestDb.Create();

            var clinic =
                TestDataFactory.Clinic(
                    "Clinic A"
                );

            var adminUser =
                TestDataFactory.User(
                    RoleNames.ClinicAdmin,
                    "Clinic Admin",
                    "116"
                );

            var admin =
                TestDataFactory.ClinicAdmin(
                    adminUser,
                    clinic
                );

            db.AddRange(
                clinic,
                adminUser,
                admin
            );

            await db.SaveChangesAsync();

            var service =
                new AdminService(
                    db,
                    new ConfigurationBuilder()
                        .Build(),
                    NullLogger<
                        AdminService
                    >.Instance
                );

            var dto =
                new RegisterClinicAdminDto
                {
                    ClinicId =
                        clinic.Id
                };

            await Assert.ThrowsAsync<
                UnauthorizedAccessException
            >(
                () =>
                    service
                        .RegisterClinicAdminAsync(
                            dto,
                            adminUser.Id
                        )
            );
        }
    }
}
