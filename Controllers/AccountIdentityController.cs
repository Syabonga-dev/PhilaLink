using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalProject.Data;
using PersonalProject.Models.Constants;
using PersonalProject.Models.DTOs;
using PersonalProject.Models.Entities;
using PersonalProject.Services.Interfaces;
using PersonalProject.Utilities;
using System.Security.Claims;

namespace PersonalProject.Controllers
{
    [ApiController]
    [Route("api/account/identity")]
    [Authorize]
    public class AccountIdentityController :
        ControllerBase
    {
        private readonly PhilaLinkDbContext
            _context;

        private readonly IAuditLogService
            _audit;

        public AccountIdentityController(
            PhilaLinkDbContext context,
            IAuditLogService audit
        )
        {
            _context =
                context;

            _audit =
                audit;
        }

        // =====================================================
        // UPDATE DATE OF BIRTH
        // =====================================================
        //
        // Only the YYMMDD portion of the current 13-digit
        // South African ID is replaced. The remaining seven
        // digits are preserved exactly.
        // =====================================================

        [HttpPut("date-of-birth")]
        public async Task<IActionResult>
            UpdateDateOfBirth(
                UpdateDateOfBirthDto dto
            )
        {
            try
            {
                var user =
                    await GetIdentityUserAsync(
                        GetCurrentUserId()
                    );

                var updatedIdNumber =
                    SouthAfricanIdNumber
                        .WithDateOfBirth(
                            user.IdNumber,
                            dto.DateOfBirth
                        );

                await EnsureIdNumberAvailableAsync(
                    updatedIdNumber,
                    user.Id
                );

                user.IdNumber =
                    updatedIdNumber;

                ApplyDateOfBirth(
                    user,
                    dto.DateOfBirth
                );

                user.UpdatedAt =
                    DateTime.UtcNow;

                await _context
                    .SaveChangesAsync();

                await _audit.LogAsync(
                    user.Role ==
                        RoleNames.Proxy
                        ? "ProxyDateOfBirthUpdated"
                        : "PatientDateOfBirthUpdated",
                    user.Id,
                    "Date of birth updated and synchronized with the first six digits of the account ID number."
                );

                return Ok(
                    CreateResponse(
                        user,
                        dto.DateOfBirth
                    )
                );
            }
            catch (
                InvalidOperationException ex
            )
            {
                return BadRequest(
                    new
                    {
                        message =
                            ex.Message
                    }
                );
            }
            catch (
                UnauthorizedAccessException
            )
            {
                return Forbid();
            }
        }

        // =====================================================
        // UPDATE FULL ID NUMBER
        // =====================================================
        //
        // The complete 13-digit ID is replaced. DateOfBirth is
        // then derived from the new ID's YYMMDD prefix so that
        // the two values cannot remain out of sync.
        // =====================================================

        [HttpPut("id-number")]
        public async Task<IActionResult>
            UpdateIdNumber(
                UpdateIdNumberDto dto
            )
        {
            try
            {
                var user =
                    await GetIdentityUserAsync(
                        GetCurrentUserId()
                    );

                var normalizedIdNumber =
                    SouthAfricanIdNumber
                        .Normalize(
                            dto.IdNumber
                        );

                var dateOfBirth =
                    SouthAfricanIdNumber
                        .GetDateOfBirth(
                            normalizedIdNumber
                        );

                await EnsureIdNumberAvailableAsync(
                    normalizedIdNumber,
                    user.Id
                );

                user.IdNumber =
                    normalizedIdNumber;

                ApplyDateOfBirth(
                    user,
                    dateOfBirth
                );

                user.UpdatedAt =
                    DateTime.UtcNow;

                await _context
                    .SaveChangesAsync();

                await _audit.LogAsync(
                    user.Role ==
                        RoleNames.Proxy
                        ? "ProxyIdNumberUpdated"
                        : "PatientIdNumberUpdated",
                    user.Id,
                    "Full account ID number updated and date of birth synchronized from its first six digits."
                );

                return Ok(
                    CreateResponse(
                        user,
                        dateOfBirth
                    )
                );
            }
            catch (
                InvalidOperationException ex
            )
            {
                return BadRequest(
                    new
                    {
                        message =
                            ex.Message
                    }
                );
            }
            catch (
                UnauthorizedAccessException
            )
            {
                return Forbid();
            }
        }

        // =====================================================
        // USER / ROLE
        // =====================================================

        private async Task<User>
            GetIdentityUserAsync(
                Guid userId
            )
        {
            var user =
                await _context.Users
                    .Include(
                        item =>
                            item.Patient
                    )
                    .Include(
                        item =>
                            item.Proxy
                    )
                    .FirstOrDefaultAsync(
                        item =>
                            item.Id ==
                                userId &&
                            item.IsActive
                    );

            if (
                user ==
                null
            )
            {
                throw new UnauthorizedAccessException();
            }

            if (
                user.Role ==
                    RoleNames.Patient &&
                user.Patient !=
                    null
            )
            {
                return user;
            }

            if (
                user.Role ==
                    RoleNames.Proxy &&
                user.Proxy !=
                    null
            )
            {
                return user;
            }

            throw new UnauthorizedAccessException();
        }

        // =====================================================
        // UNIQUE ID
        // =====================================================

        private async Task
            EnsureIdNumberAvailableAsync(
                string idNumber,
                Guid currentUserId
            )
        {
            var exists =
                await _context.Users
                    .AsNoTracking()
                    .AnyAsync(
                        user =>
                            user.Id !=
                                currentUserId &&
                            user.IdNumber ==
                                idNumber
                    );

            if (exists)
            {
                throw new InvalidOperationException(
                    "An account with that ID number already exists."
                );
            }
        }

        // =====================================================
        // PROFILE DOB
        // =====================================================

        private static void
            ApplyDateOfBirth(
                User user,
                DateOnly dateOfBirth
            )
        {
            var now =
                DateTime.UtcNow;

            if (
                user.Role ==
                    RoleNames.Patient &&
                user.Patient !=
                    null
            )
            {
                user.Patient.DateOfBirth =
                    dateOfBirth;

                user.Patient.UpdatedAt =
                    now;

                return;
            }

            if (
                user.Role ==
                    RoleNames.Proxy &&
                user.Proxy !=
                    null
            )
            {
                user.Proxy.DateOfBirth =
                    dateOfBirth;

                user.Proxy.UpdatedAt =
                    now;

                return;
            }

            throw new UnauthorizedAccessException();
        }

        // =====================================================
        // RESPONSE
        // =====================================================

        private static IdentityResponseDto
            CreateResponse(
                User user,
                DateOnly dateOfBirth
            )
        {
            return new IdentityResponseDto
            {
                IdNumber =
                    user.IdNumber,

                DateOfBirth =
                    dateOfBirth
            };
        }

        // =====================================================
        // CURRENT USER
        // =====================================================

        private Guid GetCurrentUserId()
        {
            var value =
                User.FindFirstValue(
                    ClaimTypes
                        .NameIdentifier
                );

            if (
                string.IsNullOrWhiteSpace(
                    value
                ) ||
                !Guid.TryParse(
                    value,
                    out var userId
                )
            )
            {
                throw new UnauthorizedAccessException();
            }

            return userId;
        }
    }
}
