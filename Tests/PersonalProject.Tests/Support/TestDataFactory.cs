using PersonalProject.Models;
using PersonalProject.Models.Entities;

namespace PersonalProject.Tests.Support
{
    internal static class
        TestDataFactory
    {
        public static Clinic Clinic(
            string name
        )
        {
            return new Clinic
            {
                Id =
                    Guid.NewGuid(),

                Name =
                    name,

                Type =
                    "Clinic",

                Address =
                    $"{name} Test Address",

                ContactNumber =
                    "0410000000",

                Latitude =
                    -33.9608,

                Longitude =
                    25.6022,

                Services =
                    "Primary healthcare",

                IsActive =
                    true,

                CreatedAt =
                    DateTime.UtcNow
            };
        }

        public static User User(
            string role,
            string name,
            string unique
        )
        {
            return new User
            {
                Id =
                    Guid.NewGuid(),

                FullName =
                    name,

                IdNumber =
                    $"900101500{unique.PadLeft(4, '0')}"
                        .Substring(
                            0,
                            13
                        ),

                PhoneNumber =
                    $"071{unique.PadLeft(7, '0')}"
                        .Substring(
                            0,
                            10
                        ),

                Email =
                    $"{unique}@philalink.test",

                PasswordHash =
                    BCrypt.Net.BCrypt
                        .HashPassword(
                            "TempPass!12345"
                        ),

                Role =
                    role,

                IsActive =
                    true,

                IsVerified =
                    true,

                VerifiedAt =
                    DateTime.UtcNow,

                MustChangePassword =
                    false,

                CreatedAt =
                    DateTime.UtcNow
            };
        }

        public static Patient Patient(
            User user,
            Clinic clinic,
            string patientNumber
        )
        {
            return new Patient
            {
                Id =
                    Guid.NewGuid(),

                UserId =
                    user.Id,

                User =
                    user,

                PatientNumber =
                    patientNumber,

                ClinicId =
                    clinic.Id,

                Clinic =
                    clinic,

                DateOfBirth =
                    new DateOnly(
                        1990,
                        1,
                        1
                    ),

                Gender =
                    "Female",

                Email =
                    user.Email,

                AddressLine1 =
                    "1 Test Street",

                Suburb =
                    "Test Suburb",

                City =
                    "Gqeberha",

                Province =
                    "Eastern Cape",

                PostalCode =
                    "6001",

                EmergencyContactName =
                    "Emergency Contact",

                EmergencyContactPhone =
                    "0711111111",

                EmergencyContactRelationship =
                    "Family",

                IsProfileComplete =
                    true,

                CreatedAt =
                    DateTime.UtcNow
            };
        }

        public static Nurse Nurse(
            User user,
            Clinic clinic,
            string unique
        )
        {
            return new Nurse
            {
                Id =
                    Guid.NewGuid(),

                UserId =
                    user.Id,

                User =
                    user,

                ClinicId =
                    clinic.Id,

                Clinic =
                    clinic,

                EmployeeNumber =
                    $"EMP-{unique}",

                RegistrationNumber =
                    $"SANC-{unique}",

                Qualification =
                    "Registered Nurse",

                Email =
                    user.Email,

                AddressLine1 =
                    "2 Test Street",

                Suburb =
                    "Test Suburb",

                City =
                    "Gqeberha",

                Province =
                    "Eastern Cape",

                PostalCode =
                    "6001",

                DateOfBirth =
                    new DateOnly(
                        1990,
                        1,
                        1
                    ),

                Gender =
                    "Female",

                EmploymentDate =
                    DateTime.UtcNow
                        .AddYears(
                            -1
                        ),

                EmergencyContactName =
                    "Emergency Contact",

                EmergencyContactPhone =
                    "0711111111",

                EmergencyContactRelationship =
                    "Family",

                CreatedAt =
                    DateTime.UtcNow
            };
        }

        public static Proxy Proxy(
            User user,
            Clinic clinic
        )
        {
            return new Proxy
            {
                Id =
                    Guid.NewGuid(),

                UserId =
                    user.Id,

                User =
                    user,

                ClinicId =
                    clinic.Id,

                Clinic =
                    clinic,

                Email =
                    user.Email,

                AddressLine1 =
                    "3 Test Street",

                Suburb =
                    "Test Suburb",

                City =
                    "Gqeberha",

                Province =
                    "Eastern Cape",

                PostalCode =
                    "6001",

                DateOfBirth =
                    new DateOnly(
                        1990,
                        1,
                        1
                    ),

                Gender =
                    "Male",

                EmergencyContactName =
                    "Emergency Contact",

                EmergencyContactPhone =
                    "0711111111",

                EmergencyContactRelationship =
                    "Family",

                CreatedAt =
                    DateTime.UtcNow
            };
        }

        public static Admin ClinicAdmin(
            User user,
            Clinic? clinic
        )
        {
            return new Admin
            {
                UserId =
                    user.Id,

                User =
                    user,

                FullName =
                    user.FullName,

                Email =
                    user.Email,

                ClinicId =
                    clinic?.Id,

                Clinic =
                    clinic,

                CreatedAt =
                    DateTime.UtcNow
            };
        }

        public static Admin SuperAdmin(
            User user
        )
        {
            return new Admin
            {
                UserId =
                    user.Id,

                User =
                    user,

                FullName =
                    user.FullName,

                Email =
                    user.Email,

                ClinicId =
                    null,

                Clinic =
                    null,

                CreatedAt =
                    DateTime.UtcNow
            };
        }
    }
}
