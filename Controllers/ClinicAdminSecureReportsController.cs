using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalProject.Data;
using PersonalProject.Models.Constants;
using PersonalProject.Models.DTOs;
using PersonalProject.Utilities;

namespace PersonalProject.Controllers
{
    [ApiController]
    [Route("api/clinic-admin/report-builder")]
    [Authorize(Roles = RoleNames.ClinicAdmin)]
    public class ClinicAdminSecureReportsController :
        ControllerBase
    {
        private readonly PhilaLinkDbContext
            _context;

        public ClinicAdminSecureReportsController(
            PhilaLinkDbContext context
        )
        {
            _context =
                context;
        }

        [HttpPost("export/pdf")]
        [RequestSizeLimit(4_000_000)]
        public async Task<IActionResult>
            ExportSecurePdf(
                [FromBody]
                ClinicAdminSecurePdfExportRequestDto
                    request
            )
        {
            if (
                request ==
                null
            )
            {
                return BadRequest(
                    new
                    {
                        message =
                            "A PDF export request is required."
                    }
                );
            }

            if (
                string.IsNullOrWhiteSpace(
                    request.Password
                ) ||
                request.Password.Length <
                8
            )
            {
                return BadRequest(
                    new
                    {
                        message =
                            "The PDF password must contain at least 8 characters."
                    }
                );
            }

            if (
                request.Password.Length >
                128
            )
            {
                return BadRequest(
                    new
                    {
                        message =
                            "The PDF password cannot exceed 128 characters."
                    }
                );
            }

            try
            {
                /*
                 * Reuse the existing report-builder preview
                 * pipeline so the secure PDF always contains
                 * the same clinic-scoped filtered data shown
                 * in the browser and Excel export.
                 */
                var reportController =
                    new ClinicAdminReportsController(
                        _context
                    )
                    {
                        ControllerContext =
                            new ControllerContext
                            {
                                HttpContext =
                                    HttpContext
                            }
                    };

                var previewResult =
                    await reportController
                        .Preview(
                            request.Filters ??
                            new ClinicAdminDynamicReportQueryDto()
                        );

                if (
                    previewResult is
                    ForbidResult
                )
                {
                    return Forbid();
                }

                if (
                    previewResult is
                    BadRequestObjectResult
                        badRequest
                )
                {
                    return BadRequest(
                        badRequest.Value
                    );
                }

                if (
                    previewResult is not
                        OkObjectResult ok ||
                    ok.Value is not
                        ClinicAdminDynamicReportPreviewDto
                            report
                )
                {
                    return StatusCode(
                        StatusCodes
                            .Status500InternalServerError,
                        new
                        {
                            message =
                                "Could not build the secure report dataset."
                        }
                    );
                }

                byte[]?
                    logoJpeg =
                        null;

                if (
                    !string.IsNullOrWhiteSpace(
                        request.LogoJpegBase64
                    )
                )
                {
                    var base64 =
                        request
                            .LogoJpegBase64
                            .Trim();

                    var comma =
                        base64.IndexOf(
                            ','
                        );

                    if (
                        comma >=
                        0
                    )
                    {
                        base64 =
                            base64[
                                (comma + 1)..
                            ];
                    }

                    try
                    {
                        logoJpeg =
                            Convert
                                .FromBase64String(
                                    base64
                                );

                        if (
                            logoJpeg.Length >
                            1_500_000
                        )
                        {
                            return BadRequest(
                                new
                                {
                                    message =
                                        "The supplied report logo is too large."
                                }
                            );
                        }
                    }
                    catch (
                        FormatException
                    )
                    {
                        return BadRequest(
                            new
                            {
                                message =
                                    "The supplied report logo is not valid base64 image data."
                            }
                        );
                    }
                }

                var bytes =
                    ClinicAdminSecurePdfBuilder
                        .Build(
                            report,
                            request.Password,
                            logoJpeg
                        );

                var safeClinic =
                    SanitizeFileName(
                        report.ClinicName
                    );

                var safeType =
                    SanitizeFileName(
                        report.ReportType
                    );

                var stamp =
                    DateTime.UtcNow
                        .ToString(
                            "yyyyMMdd-HHmmss"
                        );

                return File(
                    bytes,
                    "application/pdf",
                    $"{safeClinic}-{safeType}-{stamp}-protected.pdf"
                );
            }
            catch (
                ArgumentException ex
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
        }

        private static string
            SanitizeFileName(
                string value
            )
        {
            var invalid =
                Path.GetInvalidFileNameChars();

            var safe =
                new string(
                    (
                        value ??
                        string.Empty
                    )
                    .Select(
                        character =>
                            invalid.Contains(
                                character
                            )
                                ? '-'
                                : character
                    )
                    .ToArray()
                )
                .Trim();

            if (
                string.IsNullOrWhiteSpace(
                    safe
                )
            )
            {
                return "PhilaLink";
            }

            return string.Join(
                "-",
                safe.Split(
                    new[]
                    {
                        ' ',
                        '\t',
                        '\r',
                        '\n'
                    },
                    StringSplitOptions
                        .RemoveEmptyEntries
                )
            );
        }
    }
}
