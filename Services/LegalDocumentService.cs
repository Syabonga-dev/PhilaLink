using Microsoft.EntityFrameworkCore;
using PersonalProject.Data;
using PersonalProject.Models.DTOs.Legal;
using PersonalProject.Models.Entities;
using PersonalProject.Services.Interfaces;

namespace PersonalProject.Services
{
    public class LegalDocumentService : ILegalDocumentService
    {
        private readonly PhilaLinkDbContext _context;

        public LegalDocumentService(
            PhilaLinkDbContext context
        )
        {
            _context = context;
        }

        // =====================================================
        // GET CURRENT LEGAL STATUS
        // =====================================================

        public async Task<LegalStatusDto> GetLegalStatusAsync(
            Guid userId
        )
        {
            var userExists = await _context.Users
                .AsNoTracking()
                .AnyAsync(u =>
                    u.Id == userId &&
                    u.IsActive
                );

            if (!userExists)
            {
                throw new KeyNotFoundException(
                    "User was not found or is inactive."
                );
            }

            var now = DateTime.UtcNow;

            /*
             * Only documents that:
             *
             * 1. Are marked as current
             * 2. Have already reached their effective date
             *
             * are applicable to the user.
             */
            var currentDocuments = await _context
                .LegalDocuments
                .AsNoTracking()
                .Where(ld =>
                    ld.IsCurrent &&
                    ld.EffectiveDate <= now
                )
                .OrderBy(ld => ld.Type)
                .ThenBy(ld => ld.EffectiveDate)
                .ToListAsync();

            if (currentDocuments.Count == 0)
            {
                return new LegalStatusDto
                {
                    RequiresAction = false,
                    Documents = new List<LegalDocumentDto>()
                };
            }

            var currentDocumentIds = currentDocuments
                .Select(ld => ld.Id)
                .ToList();

            /*
             * Get only acceptance records related to the
             * currently applicable document versions.
             */
            var acceptances = await _context
                .UserLegalAcceptances
                .AsNoTracking()
                .Where(ula =>
                    ula.UserId == userId &&
                    currentDocumentIds.Contains(
                        ula.LegalDocumentId
                    )
                )
                .ToListAsync();

            var acceptanceLookup = acceptances
                .ToDictionary(
                    a => a.LegalDocumentId,
                    a => a
                );

            var documents = currentDocuments
                .Select(document =>
                {
                    acceptanceLookup.TryGetValue(
                        document.Id,
                        out var acceptance
                    );

                    return new LegalDocumentDto
                    {
                        Id = document.Id,
                        Type = document.Type,
                        Title = document.Title,
                        Version = document.Version,
                        EffectiveDate = document.EffectiveDate,
                        IsCurrent = document.IsCurrent,
                        HasAccepted = acceptance != null,
                        Action = acceptance?.Action,
                        AcceptedAt = acceptance?.AcceptedAt
                    };
                })
                .ToList();

            return new LegalStatusDto
            {
                RequiresAction = documents.Any(
                    d => !d.HasAccepted
                ),

                Documents = documents
            };
        }

        // =====================================================
        // ACCEPT / ACKNOWLEDGE LEGAL DOCUMENT
        // =====================================================

        public async Task<LegalDocumentDto>
            AcceptLegalDocumentAsync(
                Guid userId,
                AcceptLegalDocumentRequest request
            )
        {
            if (request == null)
            {
                throw new ArgumentNullException(
                    nameof(request)
                );
            }

            var userExists = await _context.Users
                .AsNoTracking()
                .AnyAsync(u =>
                    u.Id == userId &&
                    u.IsActive
                );

            if (!userExists)
            {
                throw new KeyNotFoundException(
                    "User was not found or is inactive."
                );
            }

            var document = await _context
                .LegalDocuments
                .FirstOrDefaultAsync(ld =>
                    ld.Id == request.LegalDocumentId
                );

            if (document == null)
            {
                throw new KeyNotFoundException(
                    "Legal document was not found."
                );
            }

            var now = DateTime.UtcNow;

            /*
             * Users may only accept the currently applicable
             * version of a legal document.
             *
             * This prevents an old Terms or Privacy version
             * from being submitted manually through the API.
             */
            if (!document.IsCurrent)
            {
                throw new InvalidOperationException(
                    "This legal document is no longer current."
                );
            }

            if (document.EffectiveDate > now)
            {
                throw new InvalidOperationException(
                    "This legal document is not yet effective."
                );
            }

            var validatedAction = ValidateAction(
                document.Type,
                request.Action
            );

            /*
             * Check whether this user has already completed
             * the legal action for this exact document version.
             */
            var existingAcceptance = await _context
                .UserLegalAcceptances
                .AsNoTracking()
                .FirstOrDefaultAsync(ula =>
                    ula.UserId == userId &&
                    ula.LegalDocumentId == document.Id
                );

            /*
             * Treat a repeated request as idempotent.
             *
             * This is useful if the frontend submits twice
             * because of a slow connection, double click,
             * retry, or refresh.
             *
             * We do NOT create a second audit record.
             */
            if (existingAcceptance != null)
            {
                return new LegalDocumentDto
                {
                    Id = document.Id,
                    Type = document.Type,
                    Title = document.Title,
                    Version = document.Version,
                    EffectiveDate = document.EffectiveDate,
                    IsCurrent = document.IsCurrent,
                    HasAccepted = true,
                    Action = existingAcceptance.Action,
                    AcceptedAt =
                        existingAcceptance.AcceptedAt
                };
            }

            var acceptance =
                new UserLegalAcceptance
                {
                    Id = Guid.NewGuid(),

                    UserId = userId,

                    LegalDocumentId =
                        document.Id,

                    Action =
                        validatedAction,

                    AcceptedAt =
                        DateTime.UtcNow
                };

            _context.UserLegalAcceptances.Add(
                acceptance
            );

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                /*
                 * The database also has a unique constraint
                 * on:
                 *
                 * UserId + LegalDocumentId
                 *
                 * If two requests arrive simultaneously,
                 * only one is allowed to create the row.
                 *
                 * We re-query so the operation behaves
                 * safely and idempotently.
                 */

                var savedAcceptance = await _context
                    .UserLegalAcceptances
                    .AsNoTracking()
                    .FirstOrDefaultAsync(ula =>
                        ula.UserId == userId &&
                        ula.LegalDocumentId ==
                            document.Id
                    );

                if (savedAcceptance == null)
                {
                    throw;
                }

                return new LegalDocumentDto
                {
                    Id = document.Id,
                    Type = document.Type,
                    Title = document.Title,
                    Version = document.Version,
                    EffectiveDate =
                        document.EffectiveDate,
                    IsCurrent =
                        document.IsCurrent,
                    HasAccepted = true,
                    Action =
                        savedAcceptance.Action,
                    AcceptedAt =
                        savedAcceptance.AcceptedAt
                };
            }

            return new LegalDocumentDto
            {
                Id = document.Id,
                Type = document.Type,
                Title = document.Title,
                Version = document.Version,
                EffectiveDate =
                    document.EffectiveDate,
                IsCurrent =
                    document.IsCurrent,
                HasAccepted = true,
                Action = acceptance.Action,
                AcceptedAt =
                    acceptance.AcceptedAt
            };
        }

        // =====================================================
        // VALIDATE ACTION
        // =====================================================

        private static string ValidateAction(
            string documentType,
            string action
        )
        {
            if (string.IsNullOrWhiteSpace(
                documentType
            ))
            {
                throw new InvalidOperationException(
                    "Legal document type is invalid."
                );
            }

            if (string.IsNullOrWhiteSpace(action))
            {
                throw new ArgumentException(
                    "A legal action is required.",
                    nameof(action)
                );
            }

            var normalizedType =
                documentType.Trim();

            var normalizedAction =
                action.Trim();

            // -------------------------------------------------
            // TERMS OF USE
            // -------------------------------------------------

            if (normalizedType.Equals(
                "TermsOfUse",
                StringComparison.OrdinalIgnoreCase
            ))
            {
                if (!normalizedAction.Equals(
                    "Accepted",
                    StringComparison.OrdinalIgnoreCase
                ))
                {
                    throw new ArgumentException(
                        "Terms of Use must be accepted.",
                        nameof(action)
                    );
                }

                return "Accepted";
            }

            // -------------------------------------------------
            // PRIVACY POLICY
            // -------------------------------------------------

            if (normalizedType.Equals(
                "PrivacyPolicy",
                StringComparison.OrdinalIgnoreCase
            ))
            {
                if (!normalizedAction.Equals(
                    "Acknowledged",
                    StringComparison.OrdinalIgnoreCase
                ))
                {
                    throw new ArgumentException(
                        "Privacy Policy must be acknowledged.",
                        nameof(action)
                    );
                }

                return "Acknowledged";
            }

            /*
             * Do not silently accept an unknown legal document
             * type. If we introduce another legal document in
             * future, its required action should be explicitly
             * defined here.
             */
            throw new InvalidOperationException(
                $"Unsupported legal document type: {documentType}."
            );
        }
    }
}