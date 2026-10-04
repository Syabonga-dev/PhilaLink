using Microsoft.AspNetCore.Authorization;
using PersonalProject.Controllers;
using PersonalProject.Models.Constants;
using System.Reflection;
using Xunit;

namespace PersonalProject.Tests.Authorization
{
    public class RoleBoundaryTests
    {
        [Fact]
        public void
            PatientControllerRequiresPatientPolicy()
        {
            var attribute =
                GetControllerAuthorize<
                    PatientController
                >();

            Assert.Equal(
                "PatientOnly",
                attribute.Policy
            );
        }

        [Fact]
        public void
            NursesControllerRequiresNurseRole()
        {
            var attribute =
                GetControllerAuthorize<
                    NursesController
                >();

            Assert.Equal(
                RoleNames.Nurse,
                attribute.Roles
            );
        }

        [Fact]
        public void
            ProxyCareControllerRequiresProxyPolicy()
        {
            var attribute =
                GetControllerAuthorize<
                    ProxyCareController
                >();

            Assert.Equal(
                "ProxyOnly",
                attribute.Policy
            );
        }

        [Fact]
        public void
            ClinicAdminControllerRequiresClinicAdminRole()
        {
            var attribute =
                GetControllerAuthorize<
                    ClinicAdminController
                >();

            Assert.Equal(
                RoleNames.ClinicAdmin,
                attribute.Roles
            );
        }

        [Fact]
        public void
            SuperAdminControllerRequiresSuperAdminPolicy()
        {
            var attribute =
                GetControllerAuthorize<
                    SuperAdminController
                >();

            Assert.Equal(
                "SuperAdminOnly",
                attribute.Policy
            );
        }

        [Fact]
        public void
            NurseRegistrationRequiresClinicAdminRole()
        {
            var attribute =
                GetMethodAuthorize<
                    AdminController
                >(
                    "RegisterNurse"
                );

            Assert.Equal(
                RoleNames.ClinicAdmin,
                attribute.Roles
            );
        }

        [Fact]
        public void
            ProxyRegistrationRequiresClinicAdminRole()
        {
            var attribute =
                GetMethodAuthorize<
                    AdminController
                >(
                    "RegisterProxy"
                );

            Assert.Equal(
                RoleNames.ClinicAdmin,
                attribute.Roles
            );
        }

        [Fact]
        public void
            ClinicAdminRegistrationRequiresSuperAdminPolicy()
        {
            var attribute =
                GetMethodAuthorize<
                    AdminController
                >(
                    "RegisterClinicAdmin"
                );

            Assert.Equal(
                "SuperAdminOnly",
                attribute.Policy
            );
        }

        private static AuthorizeAttribute
            GetControllerAuthorize<
                TController
            >()
        {
            var attribute =
                typeof(
                    TController
                )
                .GetCustomAttributes<
                    AuthorizeAttribute
                >(
                    inherit:
                        true
                )
                .FirstOrDefault();

            Assert.NotNull(
                attribute
            );

            return attribute!;
        }

        private static AuthorizeAttribute
            GetMethodAuthorize<
                TController
            >(
                string methodName
            )
        {
            var method =
                typeof(
                    TController
                )
                .GetMethod(
                    methodName,
                    BindingFlags
                        .Instance |
                    BindingFlags
                        .Public
                );

            Assert.NotNull(
                method
            );

            var attribute =
                method!
                    .GetCustomAttributes<
                        AuthorizeAttribute
                    >(
                        inherit:
                            true
                    )
                    .FirstOrDefault();

            Assert.NotNull(
                attribute
            );

            return attribute!;
        }
    }
}
