using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalProject.Data;
using PersonalProject.Models.Constants;
using PersonalProject.Models.DTOs;
using System.Security.Claims;

namespace PersonalProject.Controllers
{
    [ApiController]
    [Route("api/clinic-admin/staff-paged")]
    [Authorize(Roles = RoleNames.ClinicAdmin)]
    public class ClinicAdminStaffPagedController
        : ControllerBase
    {
        private readonly PhilaLinkDbContext _context;

        public ClinicAdminStaffPagedController(
            PhilaLinkDbContext context
        )
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetStaff(
            [FromQuery] string? role = null,
            [FromQuery] string? search = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20
        )
        {
            try
            {
                page = Math.Max(1, page);

                pageSize = Math.Clamp(
                    pageSize <= 0 ? 20 : pageSize,
                    1,
                    100
                );

                var userId = GetCurrentUserId();

                var scope = await _context.Admins
                    .AsNoTracking()
                    .Where(
                        admin =>
                            admin.UserId == userId &&
                            admin.User.Role ==
                                RoleNames.ClinicAdmin &&
                            admin.User.IsActive &&
                            admin.ClinicId != null
                    )
                    .Select(
                        admin => new
                        {
                            ClinicId = admin.ClinicId!.Value,

                            ClinicName =
                                admin.Clinic != null
                                    ? admin.Clinic.Name
                                    : ""
                        }
                    )
                    .FirstOrDefaultAsync();

                if (
                    scope == null ||
                    scope.ClinicId == Guid.Empty ||
                    string.IsNullOrWhiteSpace(scope.ClinicName)
                )
                {
                    return Forbid();
                }

                string? normalizedRole = null;

                if (
                    !string.IsNullOrWhiteSpace(role) &&
                    !role.Equals(
                        "All",
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    if (
                        role.Equals(
                            RoleNames.Nurse,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    {
                        normalizedRole = RoleNames.Nurse;
                    }
                    else if (
                        role.Equals(
                            RoleNames.Proxy,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    {
                        normalizedRole = RoleNames.Proxy;
                    }
                    else
                    {
                        return BadRequest(new
                        {
                            message =
                                "Clinic staff role must be Nurse, Proxy, or All."
                        });
                    }
                }

                var query = _context.Users
                    .AsNoTracking()
                    .Where(
                        user =>
                            (
                                user.Role == RoleNames.Nurse &&
                                user.Nurse != null &&
                                user.Nurse.ClinicId ==
                                    scope.ClinicId
                            )
                            ||
                            (
                                user.Role == RoleNames.Proxy &&
                                user.Proxy != null &&
                                user.Proxy.ClinicId ==
                                    scope.ClinicId
                            )
                    );

                if (normalizedRole != null)
                {
                    query = query.Where(
                        user => user.Role == normalizedRole
                    );
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var pattern = $"%{search.Trim()}%";

                    query = query.Where(
                        user =>
                            EF.Functions.ILike(
                                user.FullName,
                                pattern
                            )
                            ||
                            EF.Functions.ILike(
                                user.IdNumber,
                                pattern
                            )
                            ||
                            EF.Functions.ILike(
                                user.Email,
                                pattern
                            )
                            ||
                            EF.Functions.ILike(
                                user.PhoneNumber,
                                pattern
                            )
                            ||
                            EF.Functions.ILike(
                                user.Role,
                                pattern
                            )
                    );
                }

                var totalItems = await query.CountAsync();

                var staff = await query
                    .OrderBy(user => user.FullName)
                    .ThenBy(user => user.Id)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(
                        user => new ClinicAdminStaffDto
                        {
                            UserId = user.Id,

                            FullName = user.FullName,

                            IdNumber = user.IdNumber,

                            PhoneNumber = user.PhoneNumber,

                            Email = user.Email,

                            Role = user.Role,

                            IsActive = user.IsActive,

                            CreatedAt = user.CreatedAt
                        }
                    )
                    .ToListAsync();

                return Ok(
                    new PagedResultDto<ClinicAdminStaffDto>
                    {
                        Items = staff,
                        Page = page,
                        PageSize = pageSize,
                        TotalItems = totalItems
                    }
                );
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        private Guid GetCurrentUserId()
        {
            var value = User.FindFirstValue(
                ClaimTypes.NameIdentifier
            );

            if (
                string.IsNullOrWhiteSpace(value) ||
                !Guid.TryParse(value, out var userId)
            )
            {
                throw new UnauthorizedAccessException(
                    "Invalid authentication token."
                );
            }

            return userId;
        }
    }
}
