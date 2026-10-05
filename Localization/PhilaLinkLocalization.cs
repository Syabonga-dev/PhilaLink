using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;

namespace PersonalProject.Localization
{
    public static class PhilaLinkLocalization
    {
        public const string DefaultLanguage =
            "en";

        private static readonly string[]
            SupportedLanguageCodes =
            {
                "en",
                "zu",
                "xh",
                "af",
                "nso",
                "tn",
                "st",
                "ts",
                "ss",
                "ve",
                "nr"
            };

        public static IReadOnlyCollection<string>
            SupportedLanguages =>
                SupportedLanguageCodes;

        public static bool IsSupported(
            string? language
        )
        {
            if (
                string.IsNullOrWhiteSpace(
                    language
                )
            )
            {
                return false;
            }

            return SupportedLanguageCodes
                .Contains(
                    language
                        .Trim()
                        .ToLowerInvariant(),
                    StringComparer
                        .OrdinalIgnoreCase
                );
        }

        public static string Normalize(
            string? language
        )
        {
            if (
                string.IsNullOrWhiteSpace(
                    language
                )
            )
            {
                return DefaultLanguage;
            }

            var normalized =
                language
                    .Trim()
                    .Replace(
                        '_',
                        '-'
                    )
                    .Split(
                        '-',
                        StringSplitOptions
                            .RemoveEmptyEntries
                    )
                    .FirstOrDefault()
                    ?.ToLowerInvariant();

            return IsSupported(
                normalized
            )
                ? normalized!
                : DefaultLanguage;
        }

        public static void AddPhilaLinkLocalization(
            IServiceCollection services
        )
        {
            services.AddLocalization(
                options =>
                {
                    options.ResourcesPath =
                        "Resources";
                }
            );

            var cultures =
                SupportedLanguageCodes
                    .Select(
                        language =>
                            new CultureInfo(
                                language
                            )
                    )
                    .ToArray();

            services.Configure<
                RequestLocalizationOptions
            >(
                options =>
                {
                    options.DefaultRequestCulture =
                        new RequestCulture(
                            DefaultLanguage
                        );

                    options.SupportedCultures =
                        cultures;

                    options.SupportedUICultures =
                        cultures;

                    /*
                     * ASP.NET Core includes the
                     * Accept-Language provider by
                     * default.
                     *
                     * Restrict processing to the
                     * cultures above.
                     */
                    options.ApplyCurrentCultureToResponseHeaders =
                        true;
                }
            );
        }

        public static void UsePhilaLinkLocalization(
            WebApplication app
        )
        {
            app.UseRequestLocalization();
        }
    }
}
