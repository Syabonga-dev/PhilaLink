using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalProject.Models.DTOs;
using PersonalProject.Services.Interfaces;
using System.Security.Claims;

namespace PersonalProject.Controllers
{
    [ApiController]
    [Route("api/proxies/me/profile")]
    [Authorize(Policy = "ProxyOnly")]
    public class ProxyProfileController :
        ControllerBase
    {
        private readonly IProxyService
            _proxyService;

        public ProxyProfileController(
            IProxyService proxyService
        )
        {
            _proxyService =
                proxyService;
        }

        // =====================================================
        // GET MY PROFILE
        // =====================================================

        [HttpGet]
        public async Task<IActionResult>
            GetMe()
        {
            try
            {
                var profile =
                    await _proxyService
                        .GetMeAsync(
                            GetCurrentUserId()
                        );

                return Ok(
                    profile
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
        // UPDATE MY PROFILE
        // =====================================================

        [HttpPut]
        public async Task<IActionResult>
            UpdateMe(
                UpdateProxyProfileDto dto
            )
        {
            try
            {
                var profile =
                    await _proxyService
                        .UpdateMeAsync(
                            GetCurrentUserId(),
                            dto
                        );

                return Ok(
                    profile
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