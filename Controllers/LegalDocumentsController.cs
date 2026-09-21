using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalProject.Models.DTOs.Legal;
using PersonalProject.Services.Interfaces;

namespace PersonalProject.Controllers
{
    [ApiController]
    [Route("api/legal-documents")]
    [Authorize]
    public class LegalDocumentsController : ControllerBase
    {
        private readonly ILegalDocumentService _legalDocumentService;

        public LegalDocumentsController(
            ILegalDocumentService legalDocumentService
        )
        {
            _legalDocumentService = legalDocumentService;
        }

        // =====================================================
        // GET LEGAL STATUS
        // =====================================================

        /*
         * GET:
         * /api/legal-documents/status
         *
         * Returns the current Terms of Use and Privacy Policy,
         * together with whether the authenticated user has
         * already accepted/acknowledged each document.
         */
        [HttpGet("status")]
        public async Task<ActionResult<LegalStatusDto>>
            GetLegalStatus()
        {
            var userId = GetAuthenticatedUserId();

            if (userId == null)
            {
                return Unauthorized(new
                {
                    message =
                        "The authenticated user could not be identified."
                });
            }

            try
            {
                var status =
                    await _legalDocumentService
                        .GetLegalStatusAsync(
                            userId.Value
                        );

                return Ok(status);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new
                {
                    message = ex.Message
                });
            }
            catch (Exception)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        message =
                            "An unexpected error occurred while retrieving legal document status."
                    }
                );
            }
        }

        // =====================================================
        // ACCEPT / ACKNOWLEDGE DOCUMENT
        // =====================================================

        /*
         * POST:
         * /api/legal-documents/accept
         *
         * Example:
         *
         * {
         *     "legalDocumentId":
         *         "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
         *
         *     "action":
         *         "Accepted"
         * }
         *
         * TermsOfUse:
         *      Accepted
         *
         * PrivacyPolicy:
         *      Acknowledged
         */
        [HttpPost("accept")]
        public async Task<ActionResult<LegalDocumentDto>>
            AcceptLegalDocument(
                [FromBody]
                AcceptLegalDocumentRequest request
            )
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(
                    ModelState
                );
            }

            var userId = GetAuthenticatedUserId();

            if (userId == null)
            {
                return Unauthorized(new
                {
                    message =
                        "The authenticated user could not be identified."
                });
            }

            try
            {
                var result =
                    await _legalDocumentService
                        .AcceptLegalDocumentAsync(
                            userId.Value,
                            request
                        );

                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new
                {
                    message = ex.Message
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
            catch (Exception)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        message =
                            "An unexpected error occurred while recording the legal acceptance."
                    }
                );
            }
        }

        // =====================================================
        // GET AUTHENTICATED USER ID
        // =====================================================

        /*
         * Never accept UserId from the frontend for legal
         * acceptance.
         *
         * The user is identified directly from the JWT.
         *
         * This prevents a malicious request such as:
         *
         * "Accept Terms for another user's ID."
         */
        private Guid? GetAuthenticatedUserId()
        {
            var claimValue =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier
                )
                ??
                User.FindFirstValue("sub")
                ??
                User.FindFirstValue("userId")
                ??
                User.FindFirstValue("id");

            if (string.IsNullOrWhiteSpace(
                claimValue
            ))
            {
                return null;
            }

            if (!Guid.TryParse(
                claimValue,
                out var userId
            ))
            {
                return null;
            }

            return userId;
        }
    }
}