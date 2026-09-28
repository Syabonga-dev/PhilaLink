using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalProject.Models.Constants;
using PersonalProject.Models.DTOs;
using PersonalProject.Services.Interfaces;
using System.Security.Claims;

namespace PersonalProject.Controllers
{
    [ApiController]
    [Route("api/clinic-stock")]
    [Authorize(Roles = RoleNames.ClinicAdmin)]
    public class ClinicStockController :
        ControllerBase
    {
        private readonly IClinicStockService
            _stockService;

        public ClinicStockController(
            IClinicStockService stockService
        )
        {
            _stockService =
                stockService;
        }

        // =====================================================
        // GET CLINIC STOCK
        // =====================================================

        [HttpGet]
        public async Task<IActionResult>
            GetAll()
        {
            try
            {
                return Ok(
                    await _stockService
                        .GetAllAsync(
                            GetCurrentUserId()
                        )
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
        // CREATE STOCK ITEM
        // =====================================================

        [HttpPost]
        public async Task<IActionResult>
            Create(
                CreateClinicStockDto dto
            )
        {
            try
            {
                var result =
                    await _stockService
                        .CreateAsync(
                            dto,
                            GetCurrentUserId()
                        );

                return Ok(
                    result
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
        // UPDATE STOCK ITEM
        // =====================================================

        [HttpPut("{id:guid}")]
        public async Task<IActionResult>
            Update(
                Guid id,
                UpdateClinicStockDto dto
            )
        {
            try
            {
                return Ok(
                    await _stockService
                        .UpdateAsync(
                            id,
                            dto,
                            GetCurrentUserId()
                        )
                );
            }
            catch (
                KeyNotFoundException ex
            )
            {
                return NotFound(
                    new
                    {
                        message =
                            ex.Message
                    }
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
        // ADJUST QUANTITY
        // =====================================================

        [HttpPatch("{id:guid}/adjust")]
        public async Task<IActionResult>
            Adjust(
                Guid id,
                AdjustClinicStockDto dto
            )
        {
            try
            {
                return Ok(
                    await _stockService
                        .AdjustAsync(
                            id,
                            dto,
                            GetCurrentUserId()
                        )
                );
            }
            catch (
                KeyNotFoundException ex
            )
            {
                return NotFound(
                    new
                    {
                        message =
                            ex.Message
                    }
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

        private Guid
            GetCurrentUserId()
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