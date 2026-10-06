using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using System.Threading.RateLimiting;

namespace PersonalProject.Security
{
    public static class
        RateLimitingConfiguration
    {
        public static IServiceCollection
            AddPhilaLinkRateLimiting(
                this IServiceCollection
                    services
            )
        {
            services.AddRateLimiter(
                options =>
                {
                    options
                        .RejectionStatusCode =
                        StatusCodes
                            .Status429TooManyRequests;

                    options.GlobalLimiter =
                        PartitionedRateLimiter
                            .Create<
                                HttpContext,
                                string
                            >(
                                httpContext =>
                                {
                                    var clientIp =
                                        GetClientIp(
                                            httpContext
                                        );

                                    var identity =
                                        GetIdentity(
                                            httpContext,
                                            clientIp
                                        );

                                    var path =
                                        httpContext
                                            .Request
                                            .Path
                                            .Value
                                            ?.ToLowerInvariant()
                                        ??
                                        string.Empty;

                                    // -------------------------
                                    // HEALTH
                                    // -------------------------

                                    if (
                                        path ==
                                        "/api/health"
                                    )
                                    {
                                        return Fixed(
                                            $"health:{clientIp}",
                                            120,
                                            TimeSpan
                                                .FromMinutes(
                                                    1
                                                )
                                        );
                                    }

                                    // -------------------------
                                    // LOGIN
                                    // -------------------------

                                    if (
                                        path ==
                                        "/api/auth/login"
                                    )
                                    {
                                        return Fixed(
                                            $"login:{clientIp}",
                                            10,
                                            TimeSpan
                                                .FromMinutes(
                                                    1
                                                )
                                        );
                                    }

                                    // -------------------------
                                    // REGISTRATION
                                    // -------------------------

                                    if (
                                        path ==
                                        "/api/auth/register"
                                    )
                                    {
                                        return Fixed(
                                            $"register:{clientIp}",
                                            5,
                                            TimeSpan
                                                .FromMinutes(
                                                    10
                                                )
                                        );
                                    }

                                    // -------------------------
                                    // OTP GENERATION
                                    // -------------------------

                                    if (
                                        path ==
                                        "/api/auth/otp/generate"
                                    )
                                    {
                                        return Fixed(
                                            $"otp-generate:{clientIp}",
                                            5,
                                            TimeSpan
                                                .FromMinutes(
                                                    10
                                                )
                                        );
                                    }

                                    // -------------------------
                                    // OTP VERIFICATION
                                    // -------------------------

                                    if (
                                        path ==
                                        "/api/auth/otp/verify"
                                    )
                                    {
                                        return Fixed(
                                            $"otp-verify:{clientIp}",
                                            15,
                                            TimeSpan
                                                .FromMinutes(
                                                    5
                                                )
                                        );
                                    }

                                    // -------------------------
                                    // PASSWORD RESET REQUEST
                                    // -------------------------

                                    if (
                                        path ==
                                        "/api/auth/password-reset/request"
                                    )
                                    {
                                        return Fixed(
                                            $"password-reset-request:{clientIp}",
                                            5,
                                            TimeSpan
                                                .FromMinutes(
                                                    10
                                                )
                                        );
                                    }

                                    // -------------------------
                                    // PASSWORD RESET
                                    // -------------------------

                                    if (
                                        path ==
                                        "/api/auth/password-reset/reset"
                                    )
                                    {
                                        return Fixed(
                                            $"password-reset:{clientIp}",
                                            10,
                                            TimeSpan
                                                .FromMinutes(
                                                    10
                                                )
                                        );
                                    }

                                    // -------------------------
                                    // CHANGE PASSWORD
                                    // -------------------------

                                    if (
                                        path ==
                                        "/api/auth/change-password"
                                    )
                                    {
                                        return Fixed(
                                            $"change-password:{identity}",
                                            10,
                                            TimeSpan
                                                .FromMinutes(
                                                    10
                                                )
                                        );
                                    }

                                    // -------------------------
                                    // GOOGLE LOGIN
                                    // -------------------------

                                    if (
                                        path ==
                                        "/api/auth/google-login"
                                    )
                                    {
                                        return Fixed(
                                            $"google-login:{clientIp}",
                                            20,
                                            TimeSpan
                                                .FromMinutes(
                                                    1
                                                )
                                        );
                                    }

                                    // -------------------------
                                    // GOOGLE CALLBACK
                                    // -------------------------

                                    if (
                                        path ==
                                        "/api/auth/google-callback"
                                    )
                                    {
                                        return Fixed(
                                            $"google-callback:{clientIp}",
                                            60,
                                            TimeSpan
                                                .FromMinutes(
                                                    5
                                                )
                                        );
                                    }

                                    // -------------------------
                                    // CHATBOT
                                    // -------------------------

                                    if (
                                        path ==
                                        "/api/chatbot/message"
                                    )
                                    {
                                        return Fixed(
                                            $"chatbot:{identity}",
                                            20,
                                            TimeSpan
                                                .FromMinutes(
                                                    10
                                                )
                                        );
                                    }

                                    // -------------------------
                                    // WEATHER
                                    // -------------------------

                                    if (
                                        path.StartsWith(
                                            "/api/weather",
                                            StringComparison
                                                .OrdinalIgnoreCase
                                        )
                                    )
                                    {
                                        return Fixed(
                                            $"weather:{identity}",
                                            60,
                                            TimeSpan
                                                .FromMinutes(
                                                    10
                                                )
                                        );
                                    }

                                    // -------------------------
                                    // AUTHENTICATED API
                                    // -------------------------

                                    if (
                                        httpContext
                                            .User
                                            .Identity
                                            ?.IsAuthenticated ==
                                        true
                                    )
                                    {
                                        return Fixed(
                                            $"authenticated:{identity}",
                                            300,
                                            TimeSpan
                                                .FromMinutes(
                                                    1
                                                )
                                        );
                                    }

                                    // -------------------------
                                    // OTHER ANONYMOUS TRAFFIC
                                    // -------------------------

                                    return Fixed(
                                        $"anonymous:{clientIp}",
                                        60,
                                        TimeSpan
                                            .FromMinutes(
                                                1
                                            )
                                    );
                                }
                            );

                    options.OnRejected =
                        async (
                            context,
                            cancellationToken
                        ) =>
                        {
                            var response =
                                context
                                    .HttpContext
                                    .Response;

                            response.StatusCode =
                                StatusCodes
                                    .Status429TooManyRequests;

                            response.ContentType =
                                "application/json";

                            if (
                                context.Lease
                                    .TryGetMetadata(
                                        MetadataName
                                            .RetryAfter,
                                        out var retryAfter
                                    )
                            )
                            {
                                response.Headers[
                                    "Retry-After"
                                ] =
                                    Math.Ceiling(
                                        retryAfter
                                            .TotalSeconds
                                    )
                                    .ToString();
                            }

                            await response
                                .WriteAsJsonAsync(
                                    new
                                    {
                                        message =
                                            "Too many requests. Please wait a moment and try again."
                                    },
                                    cancellationToken
                                );
                        };
                }
            );

            return services;
        }

        private static RateLimitPartition<
            string
        > Fixed(
            string partitionKey,
            int permitLimit,
            TimeSpan window
        )
        {
            return RateLimitPartition
                .GetFixedWindowLimiter(
                    partitionKey,
                    _ =>
                        new FixedWindowRateLimiterOptions
                        {
                            PermitLimit =
                                permitLimit,

                            Window =
                                window,

                            QueueLimit =
                                0,

                            AutoReplenishment =
                                true
                        }
                );
        }

        private static string
            GetClientIp(
                HttpContext context
            )
        {
            /*
             * UseForwardedHeaders runs before rate limiting,
             * therefore RemoteIpAddress already represents the
             * external client instead of Render's ingress proxy.
             */
            return context
                       .Connection
                       .RemoteIpAddress
                       ?.ToString()
                   ??
                   "unknown";
        }

        private static string
            GetIdentity(
                HttpContext context,
                string clientIp
            )
        {
            if (
                context
                    .User
                    .Identity
                    ?.IsAuthenticated ==
                true
            )
            {
                var userId =
                    context.User
                        .FindFirstValue(
                            ClaimTypes
                                .NameIdentifier
                        );

                if (
                    !string.IsNullOrWhiteSpace(
                        userId
                    )
                )
                {
                    return
                        $"user:{userId}";
                }
            }

            return
                $"ip:{clientIp}";
        }
    }
}
