using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalProject.Data;
using PersonalProject.Models.Constants;

namespace PersonalProject.Controllers
{
    [ApiController]
    [Route("api/registration")]
    public class RegistrationClinicController : ControllerBase
    {
        private readonly PhilaLinkDbContext _context;

        public RegistrationClinicController(
            PhilaLinkDbContext context
        )
        {
            _context = context;
        }

        // =====================================================
        // PUBLIC CLINIC SEARCH FOR PATIENT REGISTRATION
        // =====================================================

        /*
         * This endpoint is deliberately public because the
         * patient has not logged in yet during registration.
         *
         * Only the small amount of clinic information required
         * by the registration dropdown is returned.
         *
         * Only active clinics can appear.
         */
        [HttpGet("clinics")]
        [AllowAnonymous]
        public async Task<IActionResult>
            SearchClinics(
                [FromQuery] string? search = null,
                [FromQuery] int limit = 30
            )
        {
            limit =
                Math.Clamp(
                    limit,
                    1,
                    50
                );

            var normalizedSearch =
                search?.Trim();

            if (
                normalizedSearch?.Length >
                100
            )
            {
                normalizedSearch =
                    normalizedSearch[..100];
            }

            var query =
                _context.Clinics
                    .AsNoTracking()
                    .Where(
                        clinic =>
                            clinic.IsActive
                    );

            if (
                !string.IsNullOrWhiteSpace(
                    normalizedSearch
                )
            )
            {
                var pattern =
                    $"%{normalizedSearch}%";

                query =
                    query.Where(
                        clinic =>
                            EF.Functions.ILike(
                                clinic.Name,
                                pattern
                            )
                            ||
                            EF.Functions.ILike(
                                clinic.Address,
                                pattern
                            )
                            ||
                            EF.Functions.ILike(
                                clinic.Type,
                                pattern
                            )
                    );
            }

            var clinics =
                await query
                    .OrderBy(
                        clinic =>
                            clinic.Name
                    )
                    .ThenBy(
                        clinic =>
                            clinic.Address
                    )
                    .Take(limit)
                    .Select(
                        clinic =>
                            new RegistrationClinicResultDto
                            {
                                Id =
                                    clinic.Id,

                                Name =
                                    clinic.Name,

                                Type =
                                    clinic.Type,

                                Address =
                                    clinic.Address
                            }
                    )
                    .ToListAsync();

            return Ok(
                clinics
            );
        }

        // =====================================================
        // PATIENT CLINIC SELECTION
        // =====================================================

        /*
         * Clinic selection occurs after email verification
         * but before the patient logs in.
         *
         * For safety:
         *
         * - the account must exist
         * - it must be a Patient account
         * - the account must already be verified
         * - the clinic must exist and be active
         * - clinic selection can only be completed once here
         *
         * A patient who later needs to change clinic should do
         * so through the authenticated patient profile/settings
         * workflow instead of this public registration endpoint.
         */
        [HttpPost("clinic-selection")]
        [AllowAnonymous]
        public async Task<IActionResult>
            SelectClinic(
                RegistrationClinicSelectionRequestDto request
            )
        {
            if (
                request.UserId ==
                    Guid.Empty
            )
            {
                return BadRequest(
                    new
                    {
                        message =
                            "A valid user ID is required."
                    }
                );
            }

            if (
                request.ClinicId ==
                    Guid.Empty
            )
            {
                return BadRequest(
                    new
                    {
                        message =
                            "Please select a clinic."
                    }
                );
            }

            var patient =
                await _context.Patients
                    .Include(
                        patient =>
                            patient.User
                    )
                    .FirstOrDefaultAsync(
                        patient =>
                            patient.UserId ==
                                request.UserId
                    );

            if (
                patient == null
            )
            {
                return NotFound(
                    new
                    {
                        message =
                            "Patient registration could not be found."
                    }
                );
            }

            if (
                patient.User.Role !=
                    RoleNames.Patient
            )
            {
                return BadRequest(
                    new
                    {
                        message =
                            "Clinic registration is only available for patient accounts."
                    }
                );
            }

            if (
                !patient.User.IsActive
            )
            {
                return BadRequest(
                    new
                    {
                        message =
                            "This account is not active."
                    }
                );
            }

            if (
                !patient.User.IsVerified
            )
            {
                return BadRequest(
                    new
                    {
                        message =
                            "Verify your email before selecting a clinic."
                    }
                );
            }

            /*
             * Registration clinic selection is one-time.
             *
             * Returning OK for the same clinic makes the request
             * idempotent if the browser retries after a network
             * interruption.
             */
            if (
                patient.ClinicId
                    .HasValue
            )
            {
                if (
                    patient.ClinicId.Value ==
                        request.ClinicId
                )
                {
                    var existingClinic =
                        await _context.Clinics
                            .AsNoTracking()
                            .Where(
                                clinic =>
                                    clinic.Id ==
                                        request.ClinicId
                            )
                            .Select(
                                clinic =>
                                    new RegistrationClinicResultDto
                                    {
                                        Id =
                                            clinic.Id,

                                        Name =
                                            clinic.Name,

                                        Type =
                                            clinic.Type,

                                        Address =
                                            clinic.Address
                                    }
                            )
                            .FirstOrDefaultAsync();

                    return Ok(
                        new
                        {
                            message =
                                "Clinic already selected.",

                            clinic =
                                existingClinic
                        }
                    );
                }

                return BadRequest(
                    new
                    {
                        message =
                            "A clinic has already been selected for this registration."
                    }
                );
            }

            var selectedClinic =
                await _context.Clinics
                    .FirstOrDefaultAsync(
                        clinic =>
                            clinic.Id ==
                                request.ClinicId
                            &&
                            clinic.IsActive
                    );

            if (
                selectedClinic == null
            )
            {
                return BadRequest(
                    new
                    {
                        message =
                            "The selected clinic is not available."
                    }
                );
            }

            patient.ClinicId =
                selectedClinic.Id;

            patient.UpdatedAt =
                DateTime.UtcNow;

            await _context
                .SaveChangesAsync();

            return Ok(
                new
                {
                    message =
                        "Clinic selected successfully.",

                    clinic =
                        new RegistrationClinicResultDto
                        {
                            Id =
                                selectedClinic.Id,

                            Name =
                                selectedClinic.Name,

                            Type =
                                selectedClinic.Type,

                            Address =
                                selectedClinic.Address
                        }
                }
            );
        }
    }

    // =========================================================
    // REGISTRATION CLINIC RESULT
    // =========================================================

    public sealed class
        RegistrationClinicResultDto
    {
        public Guid Id
        {
            get;
            set;
        }

        public string Name
        {
            get;
            set;
        } =
            string.Empty;

        public string Type
        {
            get;
            set;
        } =
            string.Empty;

        public string Address
        {
            get;
            set;
        } =
            string.Empty;
    }

    // =========================================================
    // REGISTRATION CLINIC SELECTION REQUEST
    // =========================================================

    public sealed class
        RegistrationClinicSelectionRequestDto
    {
        public Guid UserId
        {
            get;
            set;
        }

        public Guid ClinicId
        {
            get;
            set;
        }
    }
}
