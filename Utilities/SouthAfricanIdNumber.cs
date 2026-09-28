namespace PersonalProject.Utilities
{
    public static class SouthAfricanIdNumber
    {
        public static string Normalize(
            string? idNumber
        )
        {
            var normalized =
                idNumber?.Trim() ??
                string.Empty;

            if (
                normalized.Length !=
                    13 ||
                normalized.Any(
                    character =>
                        character < '0' ||
                        character > '9'
                )
            )
            {
                throw new InvalidOperationException(
                    "A valid 13-digit South African ID number is required."
                );
            }

            return normalized;
        }

        public static DateOnly GetDateOfBirth(
            string idNumber
        )
        {
            var normalized =
                Normalize(
                    idNumber
                );

            var yearPart =
                int.Parse(
                    normalized[..2]
                );

            var month =
                int.Parse(
                    normalized.Substring(
                        2,
                        2
                    )
                );

            var day =
                int.Parse(
                    normalized.Substring(
                        4,
                        2
                    )
                );

            var today =
                DateOnly.FromDateTime(
                    DateTime.UtcNow
                );

            var currentCentury =
                (
                    today.Year /
                    100
                ) *
                100;

            DateOnly? selected =
                null;

            foreach (
                var year in
                new[]
                {
                    currentCentury +
                        yearPart,
                    currentCentury -
                        100 +
                        yearPart
                }
            )
            {
                DateOnly candidate;

                try
                {
                    candidate =
                        new DateOnly(
                            year,
                            month,
                            day
                        );
                }
                catch (
                    ArgumentOutOfRangeException
                )
                {
                    continue;
                }

                if (
                    candidate >
                    today
                )
                {
                    continue;
                }

                if (
                    selected ==
                        null ||
                    candidate >
                        selected.Value
                )
                {
                    selected =
                        candidate;
                }
            }

            if (
                selected ==
                null
            )
            {
                throw new InvalidOperationException(
                    "The first six digits of the ID number do not contain a valid date of birth."
                );
            }

            return selected.Value;
        }

        public static string WithDateOfBirth(
            string currentIdNumber,
            DateOnly dateOfBirth
        )
        {
            var normalized =
                Normalize(
                    currentIdNumber
                );

            var today =
                DateOnly.FromDateTime(
                    DateTime.UtcNow
                );

            if (
                dateOfBirth >
                today
            )
            {
                throw new InvalidOperationException(
                    "Date of birth cannot be in the future."
                );
            }

            return
                dateOfBirth.ToString(
                    "yyMMdd"
                ) +
                normalized[6..];
        }
    }
}
