using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Text;

namespace PersonalProject.Security
{
    public sealed class RuntimeSecuritySettings
    {
        public required string ConnectionString
        {
            get;
            init;
        }

        public required string JwtKey
        {
            get;
            init;
        }

        public required string JwtIssuer
        {
            get;
            init;
        }

        public required string JwtAudience
        {
            get;
            init;
        }

        public required IReadOnlyList<string>
            AllowedOrigins
        {
            get;
            init;
        }
    }

    public static class
        ProductionSecurityConfiguration
    {
        private const int
            MinimumJwtKeyBytes = 32;

        public static RuntimeSecuritySettings
            Build(
                IConfiguration configuration,
                string environmentName
            )
        {
            var isDevelopment =
                string.Equals(
                    environmentName,
                    Environments.Development,
                    StringComparison
                        .OrdinalIgnoreCase
                );

            var connectionString =
                Require(
                    configuration
                        .GetConnectionString(
                            "DefaultConnection"
                        ),
                    "ConnectionStrings:DefaultConnection"
                );

            var jwtKey =
                Require(
                    configuration[
                        "Jwt:Key"
                    ],
                    "Jwt:Key"
                );

            if (
                Encoding.UTF8
                    .GetByteCount(
                        jwtKey
                    ) <
                MinimumJwtKeyBytes
            )
            {
                throw new
                    InvalidOperationException(
                        "Jwt:Key must contain at least 32 bytes."
                    );
            }

            var jwtIssuer =
                Require(
                    configuration[
                        "Jwt:Issuer"
                    ],
                    "Jwt:Issuer"
                );

            var jwtAudience =
                Require(
                    configuration[
                        "Jwt:Audience"
                    ],
                    "Jwt:Audience"
                );

            var allowedOrigins =
                BuildAllowedOrigins(
                    configuration,
                    isDevelopment
                );

            if (!isDevelopment)
            {
                ValidateHostedConfiguration(
                    configuration
                );
            }

            return new
                RuntimeSecuritySettings
                {
                    ConnectionString =
                        connectionString,

                    JwtKey =
                        jwtKey,

                    JwtIssuer =
                        jwtIssuer,

                    JwtAudience =
                        jwtAudience,

                    AllowedOrigins =
                        allowedOrigins
                };
        }

        private static IReadOnlyList<
            string
        > BuildAllowedOrigins(
            IConfiguration configuration,
            bool isDevelopment
        )
        {
            var allowedOrigins =
                new HashSet<string>(
                    StringComparer
                        .OrdinalIgnoreCase
                );

            var configuredFrontend =
                configuration[
                    "Frontend:BaseUrl"
                ];

            if (
                string.IsNullOrWhiteSpace(
                    configuredFrontend
                )
            )
            {
                if (!isDevelopment)
                {
                    throw new
                        InvalidOperationException(
                            "Frontend:BaseUrl is required outside Development."
                        );
                }
            }
            else
            {
                var frontendUri =
                    ParseHttpUri(
                        configuredFrontend,
                        "Frontend:BaseUrl"
                    );

                if (
                    !isDevelopment &&
                    frontendUri.Scheme !=
                    Uri.UriSchemeHttps
                )
                {
                    throw new
                        InvalidOperationException(
                            "Frontend:BaseUrl must use HTTPS outside Development."
                        );
                }

                allowedOrigins.Add(
                    frontendUri
                        .GetLeftPart(
                            UriPartial.Authority
                        )
                );
            }

            if (isDevelopment)
            {
                allowedOrigins.Add(
                    "http://localhost:5173"
                );

                allowedOrigins.Add(
                    "https://localhost:5173"
                );
            }

            return allowedOrigins
                .OrderBy(
                    origin => origin,
                    StringComparer
                        .OrdinalIgnoreCase
                )
                .ToArray();
        }

        private static void
            ValidateHostedConfiguration(
                IConfiguration configuration
            )
        {
            var googleRedirectUri =
                configuration[
                    "GoogleOAuth:RedirectUri"
                ];

            if (
                !string.IsNullOrWhiteSpace(
                    googleRedirectUri
                )
            )
            {
                var redirectUri =
                    ParseHttpUri(
                        googleRedirectUri,
                        "GoogleOAuth:RedirectUri"
                    );

                if (
                    redirectUri.Scheme !=
                    Uri.UriSchemeHttps
                )
                {
                    throw new
                        InvalidOperationException(
                            "GoogleOAuth:RedirectUri must use HTTPS outside Development."
                        );
                }
            }

            var configuredSmtpSsl =
                configuration[
                    "Smtp:EnableSsl"
                ];

            if (
                bool.TryParse(
                    configuredSmtpSsl,
                    out var enableSsl
                ) &&
                !enableSsl
            )
            {
                throw new
                    InvalidOperationException(
                        "Smtp:EnableSsl cannot be false outside Development."
                    );
            }
        }

        private static Uri
            ParseHttpUri(
                string value,
                string settingName
            )
        {
            var normalized =
                value
                    .Trim()
                    .TrimEnd('/');

            if (
                !Uri.TryCreate(
                    normalized,
                    UriKind.Absolute,
                    out var uri
                ) ||
                (
                    uri.Scheme !=
                    Uri.UriSchemeHttp &&
                    uri.Scheme !=
                    Uri.UriSchemeHttps
                ) ||
                !string.IsNullOrEmpty(
                    uri.UserInfo
                )
            )
            {
                throw new
                    InvalidOperationException(
                        $"{settingName} must be a valid absolute HTTP or HTTPS URL."
                    );
            }

            return uri;
        }

        private static string Require(
            string? value,
            string settingName
        )
        {
            if (
                string.IsNullOrWhiteSpace(
                    value
                )
            )
            {
                throw new
                    InvalidOperationException(
                        $"{settingName} is missing from configuration."
                    );
            }

            return value.Trim();
        }
    }
}
