using Microsoft.EntityFrameworkCore;
using PersonalProject.Data;
using PersonalProject.Models.Constants;
using PersonalProject.Models.DTOs;

namespace PersonalProject.Services.Queries
{
    public static class PatientRecordQueries
    {
        private const int
            DefaultPageSize = 20;

        private const int
            MaximumPageSize = 100;

        private const int
            MaximumPageNumber = 1_000_000;

        private const int
            MaximumSearchLength = 100;

        public static async Task<
            PagedResultDto<
                HealthRecordResponseDto
            >
        >
            GetPageAsync(
                PhilaLinkDbContext context,
                Guid userId,
                string? category,
                string? search,
                int page,
                int pageSize,
                CancellationToken
                    cancellationToken =
                        default
            )
        {
            var patientId =
                await context.Patients
                    .AsNoTracking()
                    .Where(
                        patient =>
                            patient.UserId ==
                                userId &&
                            patient.User.Role ==
                                RoleNames.Patient &&
                            patient.User.IsActive
                    )
                    .Select(
                        patient =>
                            (Guid?)patient.Id
                    )
                    .FirstOrDefaultAsync(
                        cancellationToken
                    );

            if (
                patientId ==
                    null
            )
            {
                throw new UnauthorizedAccessException(
                    "Active Patient profile not found."
                );
            }

            var normalizedPage =
                Math.Clamp(
                    page,
                    1,
                    MaximumPageNumber
                );

            var normalizedPageSize =
                pageSize <=
                    0
                    ? DefaultPageSize
                    : Math.Min(
                        pageSize,
                        MaximumPageSize
                    );

            var normalizedCategory =
                NormalizeCategory(
                    category
                );

            var normalizedSearch =
                NormalizeSearch(
                    search
                );

            var query =
                context.HealthRecords
                    .AsNoTracking()
                    .Where(
                        record =>
                            record.PatientId ==
                                patientId.Value
                    );

            query =
                ApplyCategoryFilter(
                    query,
                    normalizedCategory
                );

            if (
                normalizedSearch !=
                    null
            )
            {
                var pattern =
                    $"%{normalizedSearch}%";

                query =
                    query.Where(
                        record =>
                            EF.Functions.ILike(
                                record.Title,
                                pattern
                            ) ||
                            EF.Functions.ILike(
                                record.Type,
                                pattern
                            ) ||
                            EF.Functions.ILike(
                                record.Category,
                                pattern
                            ) ||
                            (
                                record.ProviderName !=
                                    null &&
                                EF.Functions.ILike(
                                    record.ProviderName,
                                    pattern
                                )
                            ) ||
                            EF.Functions.ILike(
                                record.Summary,
                                pattern
                            ) ||
                            EF.Functions.ILike(
                                record.Status,
                                pattern
                            ) ||
                            (
                                record.Clinic !=
                                    null &&
                                EF.Functions.ILike(
                                    record.Clinic.Name,
                                    pattern
                                )
                            )
                    );
            }

            var totalItems =
                await query.CountAsync(
                    cancellationToken
                );

            var skip =
                (
                    normalizedPage -
                    1
                ) *
                normalizedPageSize;

            var items =
                await query
                    .OrderByDescending(
                        record =>
                            record.RecordDate
                    )
                    .ThenByDescending(
                        record =>
                            record.Id
                    )
                    .Skip(
                        skip
                    )
                    .Take(
                        normalizedPageSize
                    )
                    .Select(
                        record =>
                            new HealthRecordResponseDto
                            {
                                Id =
                                    record.Id,

                                Title =
                                    record.Title,

                                Type =
                                    record.Type,

                                Category =
                                    record.Category,

                                ProviderName =
                                    record.ProviderName,

                                Facility =
                                    record.Clinic ==
                                    null
                                        ? null
                                        : record
                                            .Clinic
                                            .Name,

                                Summary =
                                    record.Summary,

                                Status =
                                    record.Status,

                                RecordDate =
                                    record.RecordDate
                            }
                    )
                    .ToListAsync(
                        cancellationToken
                    );

            return new
                PagedResultDto<
                    HealthRecordResponseDto
                >
            {
                Items =
                        items,

                Page =
                        normalizedPage,

                PageSize =
                        normalizedPageSize,

                TotalItems =
                        totalItems
            };
        }

        private static IQueryable<
            PersonalProject.Models.Entities
                .HealthRecord
        >
            ApplyCategoryFilter(
                IQueryable<
                    PersonalProject.Models.Entities
                        .HealthRecord
                > query,
                string category
            )
        {
            if (
                category ==
                    "all"
            )
            {
                return query;
            }

            if (
                category ==
                    "consultation"
            )
            {
                return query.Where(
                    record =>
                        EF.Functions.ILike(
                            record.Category,
                            "%consult%"
                        ) ||
                        EF.Functions.ILike(
                            record.Type,
                            "%consult%"
                        ) ||
                        EF.Functions.ILike(
                            record.Category,
                            "%clinical%"
                        ) ||
                        EF.Functions.ILike(
                            record.Type,
                            "%clinical%"
                        )
                );
            }

            if (
                category ==
                    "laboratory"
            )
            {
                return query.Where(
                    record =>
                        EF.Functions.ILike(
                            record.Category,
                            "%lab%"
                        ) ||
                        EF.Functions.ILike(
                            record.Type,
                            "%lab%"
                        ) ||
                        EF.Functions.ILike(
                            record.Category,
                            "%test%"
                        ) ||
                        EF.Functions.ILike(
                            record.Type,
                            "%test%"
                        )
                );
            }

            if (
                category ==
                    "medication"
            )
            {
                return query.Where(
                    record =>
                        EF.Functions.ILike(
                            record.Category,
                            "%med%"
                        ) ||
                        EF.Functions.ILike(
                            record.Type,
                            "%med%"
                        )
                );
            }

            return query.Where(
                record =>
                    EF.Functions.ILike(
                        record.Category,
                        "%observation%"
                    ) ||
                    EF.Functions.ILike(
                        record.Type,
                        "%observation%"
                    ) ||
                    EF.Functions.ILike(
                        record.Category,
                        "%vital%"
                    ) ||
                    EF.Functions.ILike(
                        record.Type,
                        "%vital%"
                    )
            );
        }

        private static string
            NormalizeCategory(
                string? category
            )
        {
            if (
                string.IsNullOrWhiteSpace(
                    category
                )
            )
            {
                return "all";
            }

            var normalized =
                category
                    .Trim()
                    .ToLowerInvariant();

            if (
                normalized ==
                    "all" ||
                normalized ==
                    "consultation" ||
                normalized ==
                    "laboratory" ||
                normalized ==
                    "medication" ||
                normalized ==
                    "observation"
            )
            {
                return normalized;
            }

            throw new InvalidOperationException(
                "Health record category must be 'all', 'consultation', 'laboratory', 'medication', or 'observation'."
            );
        }

        private static string?
            NormalizeSearch(
                string? search
            )
        {
            if (
                string.IsNullOrWhiteSpace(
                    search
                )
            )
            {
                return null;
            }

            /*
             * % and _ have special meaning inside PostgreSQL
             * LIKE/ILIKE patterns. Remove them so the patient's
             * text is treated as ordinary search input rather
             * than as a wildcard expression.
             */
            var normalized =
                search
                    .Trim()
                    .Replace(
                        "%",
                        string.Empty
                    )
                    .Replace(
                        "_",
                        string.Empty
                    );

            if (
                normalized.Length >
                    MaximumSearchLength
            )
            {
                normalized =
                    normalized[
                        ..MaximumSearchLength
                    ];
            }

            return string.IsNullOrWhiteSpace(
                normalized
            )
                ? null
                : normalized;
        }
    }
}