using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalProject.Models.Constants;
using PersonalProject.Models.DTOs;
using PersonalProject.Services.Interfaces;
using System.Security.Claims;

namespace PersonalProject.Controllers
{
    [ApiController]
    [Route("api/collections")]
    [Authorize(Policy = "ClinicStaff")]
    public class CollectionsController : ControllerBase
    {
        private readonly IMedicationCollectionService _collectionService;

        public CollectionsController(IMedicationCollectionService collectionService)
        {
            _collectionService = collectionService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                return Ok(await _collectionService.GetClinicCollectionsAsync(GetCurrentUserId()));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            try
            {
                return Ok(await _collectionService.GetSummaryAsync(GetCurrentUserId()));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateMedicationCollectionDto dto)
        {
            try
            {
                return Ok(await _collectionService.CreateAsync(dto, GetCurrentUserId()));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(
                    new { message = ex.Message }
                );
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(
                    new { message = ex.Message }
                );
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpPatch("{id:guid}/proxy")]
        public async Task<IActionResult> AssignProxy(Guid id, AssignCollectionProxyDto dto)
        {
            try
            {
                return Ok(await _collectionService.AssignProxyAsync(id, dto.ProxyId, GetCurrentUserId()));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(
                    new { message = ex.Message }
                );
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        /*
         * The current Nurse frontend already calls:
         *
         * PATCH /api/collections/{id}/collect
         */
        [HttpPatch("{id:guid}/collect")]
        [Authorize(Roles = RoleNames.Nurse)]
        public async Task<IActionResult> Collect(Guid id, CompleteMedicationCollectionDto dto)
        {
            try
            {
                return Ok(await _collectionService.CompleteAsync(id, dto, GetCurrentUserId()));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(
                    new { message = ex.Message }
                );
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(
                    new { message = ex.Message }
                );
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        private Guid GetCurrentUserId()
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (
                string.IsNullOrWhiteSpace(value) ||
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