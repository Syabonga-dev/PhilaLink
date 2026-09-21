using BCrypt.Net;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Npgsql;
using PersonalProject.Data;
using PersonalProject.Models;
using PersonalProject.Models.Constants;
using PersonalProject.Models.Entities;
using PersonalProject.Services.AI;
using PersonalProject.Services.Implementations;
using PersonalProject.Services.Interfaces;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using PersonalProject.Services;

var builder = WebApplication.CreateBuilder(args);

// =====================================================
// DATABASE
// =====================================================

var configuredConnectionString =
    builder.Configuration.GetConnectionString(
        "DefaultConnection"
    );

if (
    string.IsNullOrWhiteSpace(
        configuredConnectionString
    )
)
{
    throw new InvalidOperationException(
        "ConnectionStrings:DefaultConnection is missing."
    );
}

var connectionStringBuilder =
    new NpgsqlConnectionStringBuilder(
        configuredConnectionString
    )
    {
        KeepAlive =
            30,

        ConnectionIdleLifetime =
            300,

        ConnectionPruningInterval =
            10
    };

builder.Services.AddDbContext<PhilaLinkDbContext>(
    options =>
        options.UseNpgsql(
            connectionStringBuilder
                .ConnectionString,
            npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay:
                        TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null
                );
            }
        )
);

// =====================================================
// DEPENDENCY INJECTION
// =====================================================

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IProxyService, ProxyService>();
builder.Services.AddScoped<IMedicationService, MedicationService>();
builder.Services.AddScoped<IClinicService, ClinicService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<ISymptomAssessmentService, SymptomAssessmentService>();
builder.Services.AddScoped<IOtpVerificationService, OtpVerificationService>();
builder.Services.AddScoped<IPatientService, PatientService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IAppointmentService, AppointmentService>();
builder.Services.AddScoped<IMedicationCollectionService, MedicationCollectionService>();
builder.Services.AddScoped<IClinicStockService, ClinicStockService>();
builder.Services.AddScoped<INurseService, NurseService>();
builder.Services.AddScoped<IChatbotService, ChatbotService>();
builder.Services.AddScoped<ILegalDocumentService, LegalDocumentService>();

builder.Services.AddHttpClient<IChatbotProvider, GeminiChatbotProvider>(
    client =>
    {
        client.Timeout =
            TimeSpan.FromSeconds(45);
    }
);

builder.Services.AddHttpClient<IWeatherService, WeatherService>(
    client =>
    {
        client.BaseAddress =
            new Uri(
                "https://api.openweathermap.org"
            );

        client.Timeout =
            TimeSpan.FromSeconds(15);
    }
);

// =====================================================
// CONTROLLERS
// =====================================================

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// =====================================================
// SWAGGER
// =====================================================

builder.Services.AddSwaggerGen(
    options =>
    {
        options.SwaggerDoc(
            "v1",
            new OpenApiInfo
            {
                Title = "PhilaLink API",
                Version = "v1"
            }
        );

        options.AddSecurityDefinition(
            "Bearer",
            new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description =
                    "Enter JWT token like: Bearer {token}"
            }
        );

        options.AddSecurityRequirement(
            document =>
                new OpenApiSecurityRequirement
                {
                    [
                        new OpenApiSecuritySchemeReference(
                            "Bearer",
                            document
                        )
                    ] = new List<string>()
                }
        );
    }
);

// =====================================================
// CORS
// =====================================================

var allowedOrigins =
    new List<string>
    {
        "https://philalinkmed.vercel.app"
    };

var configuredFrontendBaseUrl =
    builder.Configuration[
        "Frontend:BaseUrl"
    ];

if (
    !string.IsNullOrWhiteSpace(
        configuredFrontendBaseUrl
    )
)
{
    var normalizedFrontendUrl =
        configuredFrontendBaseUrl
            .Trim()
            .TrimEnd('/');

    if (
        Uri.TryCreate(
            normalizedFrontendUrl,
            UriKind.Absolute,
            out var frontendUri
        ) &&
        (
            frontendUri.Scheme ==
                Uri.UriSchemeHttps ||
            frontendUri.Scheme ==
                Uri.UriSchemeHttp
        )
    )
    {
        allowedOrigins.Add(
            frontendUri.GetLeftPart(
                UriPartial.Authority
            )
        );
    }
}

if (
    builder.Environment
        .IsDevelopment()
)
{
    allowedOrigins.Add(
        "http://localhost:5173"
    );

    allowedOrigins.Add(
        "https://localhost:5173"
    );
}

allowedOrigins =
    allowedOrigins
        .Distinct(
            StringComparer
                .OrdinalIgnoreCase
        )
        .ToList();

builder.Services.AddCors(
    options =>
    {
        options.AddPolicy(
            "FrontendOnly",
            policy =>
            {
                policy
                    .WithOrigins(
                        allowedOrigins
                            .ToArray()
                    )
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            }
        );
    }
);

// =====================================================
// RATE LIMIT HELPERS
// =====================================================

static string GetClientIp(
    HttpContext httpContext
)
{
    /*
     * Render provides the original client address through
     * X-Forwarded-For.
     *
     * The first valid address is used. If it is unavailable,
     * fall back to the connection's remote address.
     */
    var forwardedFor =
        httpContext
            .Request
            .Headers[
                "X-Forwarded-For"
            ]
            .FirstOrDefault();

    if (
        !string.IsNullOrWhiteSpace(
            forwardedFor
        )
    )
    {
        var candidates =
            forwardedFor.Split(
                ',',
                StringSplitOptions
                    .RemoveEmptyEntries |
                StringSplitOptions
                    .TrimEntries
            );

        foreach (
            var candidate in candidates
        )
        {
            if (
                IPAddress.TryParse(
                    candidate,
                    out var address
                )
            )
            {
                return address
                    .ToString();
            }
        }
    }

    return httpContext
               .Connection
               .RemoteIpAddress
               ?.ToString()
           ??
           "unknown";
}

static string
    GetAuthenticatedIdentity(
        HttpContext httpContext,
        string clientIp
    )
{
    if (
        httpContext
            .User
            .Identity
            ?.IsAuthenticated ==
        true
    )
    {
        var userId =
            httpContext.User
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

// =====================================================
// RATE LIMITING
// =====================================================

builder.Services.AddRateLimiter(
    options =>
    {
        options.RejectionStatusCode =
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
                            GetAuthenticatedIdentity(
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

                        // =====================================
                        // HEALTH
                        // =====================================

                        /*
                         * Health is intentionally generous.
                         *
                         * This allows:
                         * - Render cold-start warming
                         * - frontend Google OAuth pre-warming
                         * - uptime monitoring
                         *
                         * It is still bounded so an attacker
                         * cannot hammer it without restriction.
                         */
                        if (
                            path ==
                            "/api/health"
                        )
                        {
                            return RateLimitPartition
                                .GetFixedWindowLimiter(
                                    $"health:{clientIp}",
                                    _ =>
                                        new FixedWindowRateLimiterOptions
                                        {
                                            PermitLimit =
                                                120,

                                            Window =
                                                TimeSpan
                                                    .FromMinutes(
                                                        1
                                                    ),

                                            QueueLimit =
                                                0,

                                            AutoReplenishment =
                                                true
                                        }
                                );
                        }

                        // =====================================
                        // LOGIN
                        // =====================================

                        if (
                            path ==
                            "/api/auth/login"
                        )
                        {
                            return RateLimitPartition
                                .GetFixedWindowLimiter(
                                    $"login:{clientIp}",
                                    _ =>
                                        new FixedWindowRateLimiterOptions
                                        {
                                            PermitLimit =
                                                10,

                                            Window =
                                                TimeSpan
                                                    .FromMinutes(
                                                        1
                                                    ),

                                            QueueLimit =
                                                0,

                                            AutoReplenishment =
                                                true
                                        }
                                );
                        }

                        // =====================================
                        // REGISTRATION
                        // =====================================

                        if (
                            path ==
                            "/api/auth/register"
                        )
                        {
                            return RateLimitPartition
                                .GetFixedWindowLimiter(
                                    $"register:{clientIp}",
                                    _ =>
                                        new FixedWindowRateLimiterOptions
                                        {
                                            PermitLimit =
                                                5,

                                            Window =
                                                TimeSpan
                                                    .FromMinutes(
                                                        10
                                                    ),

                                            QueueLimit =
                                                0,

                                            AutoReplenishment =
                                                true
                                        }
                                );
                        }

                        // =====================================
                        // OTP GENERATION
                        // =====================================

                        if (
                            path ==
                            "/api/auth/otp/generate"
                        )
                        {
                            return RateLimitPartition
                                .GetFixedWindowLimiter(
                                    $"otp-generate:{clientIp}",
                                    _ =>
                                        new FixedWindowRateLimiterOptions
                                        {
                                            PermitLimit =
                                                5,

                                            Window =
                                                TimeSpan
                                                    .FromMinutes(
                                                        10
                                                    ),

                                            QueueLimit =
                                                0,

                                            AutoReplenishment =
                                                true
                                        }
                                );
                        }

                        // =====================================
                        // OTP VERIFICATION
                        // =====================================

                        if (
                            path ==
                            "/api/auth/otp/verify"
                        )
                        {
                            return RateLimitPartition
                                .GetFixedWindowLimiter(
                                    $"otp-verify:{clientIp}",
                                    _ =>
                                        new FixedWindowRateLimiterOptions
                                        {
                                            PermitLimit =
                                                15,

                                            Window =
                                                TimeSpan
                                                    .FromMinutes(
                                                        5
                                                    ),

                                            QueueLimit =
                                                0,

                                            AutoReplenishment =
                                                true
                                        }
                                );
                        }

                        // =====================================
                        // PASSWORD RESET REQUEST
                        // =====================================

                        if (
                            path ==
                            "/api/auth/password-reset/request"
                        )
                        {
                            return RateLimitPartition
                                .GetFixedWindowLimiter(
                                    $"password-reset-request:{clientIp}",
                                    _ =>
                                        new FixedWindowRateLimiterOptions
                                        {
                                            PermitLimit =
                                                5,

                                            Window =
                                                TimeSpan
                                                    .FromMinutes(
                                                        10
                                                    ),

                                            QueueLimit =
                                                0,

                                            AutoReplenishment =
                                                true
                                        }
                                );
                        }

                        // =====================================
                        // PASSWORD RESET COMPLETION
                        // =====================================

                        if (
                            path ==
                            "/api/auth/password-reset/reset"
                        )
                        {
                            return RateLimitPartition
                                .GetFixedWindowLimiter(
                                    $"password-reset:{clientIp}",
                                    _ =>
                                        new FixedWindowRateLimiterOptions
                                        {
                                            PermitLimit =
                                                10,

                                            Window =
                                                TimeSpan
                                                    .FromMinutes(
                                                        10
                                                    ),

                                            QueueLimit =
                                                0,

                                            AutoReplenishment =
                                                true
                                        }
                                );
                        }

                        // =====================================
                        // CHANGE PASSWORD
                        // =====================================

                        if (
                            path ==
                            "/api/auth/change-password"
                        )
                        {
                            return RateLimitPartition
                                .GetFixedWindowLimiter(
                                    $"change-password:{identity}",
                                    _ =>
                                        new FixedWindowRateLimiterOptions
                                        {
                                            PermitLimit =
                                                10,

                                            Window =
                                                TimeSpan
                                                    .FromMinutes(
                                                        10
                                                    ),

                                            QueueLimit =
                                                0,

                                            AutoReplenishment =
                                                true
                                        }
                                );
                        }

                        // =====================================
                        // GOOGLE LOGIN
                        // =====================================

                        if (
                            path ==
                            "/api/auth/google-login"
                        )
                        {
                            return RateLimitPartition
                                .GetFixedWindowLimiter(
                                    $"google-login:{clientIp}",
                                    _ =>
                                        new FixedWindowRateLimiterOptions
                                        {
                                            PermitLimit =
                                                20,

                                            Window =
                                                TimeSpan
                                                    .FromMinutes(
                                                        1
                                                    ),

                                            QueueLimit =
                                                0,

                                            AutoReplenishment =
                                                true
                                        }
                                );
                        }

                        // =====================================
                        // GOOGLE CALLBACK
                        // =====================================

                        /*
                         * Do not make the OAuth callback overly
                         * restrictive. A failed/repeated OAuth
                         * redirect should not lock legitimate
                         * users out.
                         */
                        if (
                            path ==
                            "/api/auth/google-callback"
                        )
                        {
                            return RateLimitPartition
                                .GetFixedWindowLimiter(
                                    $"google-callback:{clientIp}",
                                    _ =>
                                        new FixedWindowRateLimiterOptions
                                        {
                                            PermitLimit =
                                                60,

                                            Window =
                                                TimeSpan
                                                    .FromMinutes(
                                                        5
                                                    ),

                                            QueueLimit =
                                                0,

                                            AutoReplenishment =
                                                true
                                        }
                                );
                        }

                        // =====================================
                        // GEMINI / CHATBOT
                        // =====================================

                        /*
                         * This endpoint can cause a paid Gemini
                         * API request.
                         *
                         * Limit by authenticated PhilaLink user,
                         * not by IP, so multiple legitimate users
                         * sharing a university/residence network
                         * do not consume one another's allowance.
                         *
                         * 20 messages / 10 minutes allows normal
                         * conversational use while making mass
                         * automated abuse far more difficult.
                         */
                        if (
                            path ==
                            "/api/chatbot/message"
                        )
                        {
                            return RateLimitPartition
                                .GetFixedWindowLimiter(
                                    $"chatbot:{identity}",
                                    _ =>
                                        new FixedWindowRateLimiterOptions
                                        {
                                            PermitLimit =
                                                20,

                                            Window =
                                                TimeSpan
                                                    .FromMinutes(
                                                        10
                                                    ),

                                            QueueLimit =
                                                0,

                                            AutoReplenishment =
                                                true
                                        }
                                );
                        }

                        // =====================================
                        // WEATHER
                        // =====================================

                        /*
                         * Weather calls another external API.
                         * The allowance is high enough for normal
                         * dashboard refreshes while preventing
                         * uncontrolled automated traffic.
                         */
                        if (
                            path.StartsWith(
                                "/api/weather",
                                StringComparison
                                    .OrdinalIgnoreCase
                            )
                        )
                        {
                            return RateLimitPartition
                                .GetFixedWindowLimiter(
                                    $"weather:{identity}",
                                    _ =>
                                        new FixedWindowRateLimiterOptions
                                        {
                                            PermitLimit =
                                                60,

                                            Window =
                                                TimeSpan
                                                    .FromMinutes(
                                                        10
                                                    ),

                                            QueueLimit =
                                                0,

                                            AutoReplenishment =
                                                true
                                        }
                                );
                        }

                        // =====================================
                        // AUTHENTICATED API
                        // =====================================

                        /*
                         * Normal authenticated PhilaLink traffic
                         * receives a broad allowance.
                         *
                         * Dashboard pages can make multiple API
                         * requests at once, so this limit is kept
                         * intentionally high enough not to disturb
                         * ordinary application use.
                         */
                        if (
                            httpContext
                                .User
                                .Identity
                                ?.IsAuthenticated ==
                            true
                        )
                        {
                            return RateLimitPartition
                                .GetFixedWindowLimiter(
                                    $"authenticated:{identity}",
                                    _ =>
                                        new FixedWindowRateLimiterOptions
                                        {
                                            PermitLimit =
                                                300,

                                            Window =
                                                TimeSpan
                                                    .FromMinutes(
                                                        1
                                                    ),

                                            QueueLimit =
                                                0,

                                            AutoReplenishment =
                                                true
                                        }
                                );
                        }

                        // =====================================
                        // OTHER ANONYMOUS TRAFFIC
                        // =====================================

                        /*
                         * Most API endpoints require authentication
                         * anyway. This provides a final outer bound
                         * for malformed, probing or unauthenticated
                         * requests.
                         */
                        return RateLimitPartition
                            .GetFixedWindowLimiter(
                                $"anonymous:{clientIp}",
                                _ =>
                                    new FixedWindowRateLimiterOptions
                                    {
                                        PermitLimit =
                                            60,

                                        Window =
                                            TimeSpan
                                                .FromMinutes(
                                                    1
                                                ),

                                        QueueLimit =
                                            0,

                                        AutoReplenishment =
                                            true
                                    }
                            );
                    }
                );

        // =============================================
        // RATE LIMIT RESPONSE
        // =============================================

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

// =====================================================
// JWT AUTHENTICATION
// =====================================================

var jwtKey =
    builder.Configuration[
        "Jwt:Key"
    ];

if (
    string.IsNullOrWhiteSpace(
        jwtKey
    )
)
{
    throw new InvalidOperationException(
        "JWT Key is missing in configuration."
    );
}

var key =
    Encoding.UTF8.GetBytes(
        jwtKey
    );

builder.Services
    .AddAuthentication(
        JwtBearerDefaults
            .AuthenticationScheme
    )
    .AddJwtBearer(
        options =>
        {
            options.TokenValidationParameters =
                new TokenValidationParameters
                {
                    ValidateIssuer =
                        true,

                    ValidateAudience =
                        false,

                    ValidateIssuerSigningKey =
                        true,

                    ValidateLifetime =
                        true,

                    ClockSkew =
                        TimeSpan.Zero,

                    ValidIssuer =
                        builder.Configuration[
                            "Jwt:Issuer"
                        ],

                    IssuerSigningKey =
                        new SymmetricSecurityKey(
                            key
                        )
                };
        }
    );

// =====================================================
// AUTHORIZATION POLICIES
// =====================================================

builder.Services.AddAuthorization(
    options =>
    {
        options.DefaultPolicy =
            new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .RequireClaim(
                    "mustChangePassword",
                    "false"
                )
                .Build();

        options.AddPolicy(
            "PasswordChangeAllowed",
            policy =>
                policy.RequireAuthenticatedUser()
        );

        options.AddPolicy(
            "SuperAdminOnly",
            policy =>
                policy
                    .RequireRole(
                        RoleNames.SuperAdmin
                    )
                    .RequireClaim(
                        "mustChangePassword",
                        "false"
                    )
        );

        options.AddPolicy(
            "AdminOnly",
            policy =>
                policy
                    .RequireRole(
                        RoleNames.SuperAdmin,
                        RoleNames.ClinicAdmin
                    )
                    .RequireClaim(
                        "mustChangePassword",
                        "false"
                    )
        );

        options.AddPolicy(
            "ClinicStaff",
            policy =>
                policy
                    .RequireRole(
                        RoleNames.ClinicAdmin,
                        RoleNames.Nurse
                    )
                    .RequireClaim(
                        "mustChangePassword",
                        "false"
                    )
        );

        options.AddPolicy(
            "PatientOnly",
            policy =>
                policy
                    .RequireRole(
                        RoleNames.Patient
                    )
                    .RequireClaim(
                        "mustChangePassword",
                        "false"
                    )
        );

        options.AddPolicy(
            "ProxyOnly",
            policy =>
                policy
                    .RequireRole(
                        RoleNames.Proxy
                    )
                    .RequireClaim(
                        "mustChangePassword",
                        "false"
                    )
        );

        options.AddPolicy(
            "PatientOrProxy",
            policy =>
                policy
                    .RequireRole(
                        RoleNames.Patient,
                        RoleNames.Proxy
                    )
                    .RequireClaim(
                        "mustChangePassword",
                        "false"
                    )
        );
    }
);

var app =
    builder.Build();

// =====================================================
// SUPER ADMIN SEED
// =====================================================

var seedFullName =
    builder.Configuration[
        "Seed:SuperAdminFullName"
    ];

var seedIdNumber =
    builder.Configuration[
        "Seed:SuperAdminIdNumber"
    ];

var seedPhone =
    builder.Configuration[
        "Seed:SuperAdminPhoneNumber"
    ];

var seedEmail =
    builder.Configuration[
        "Seed:SuperAdminEmail"
    ];

var seedPassword =
    builder.Configuration[
        "Seed:SuperAdminPassword"
    ];

var seedConfigured =
    !string.IsNullOrWhiteSpace(
        seedFullName
    ) &&
    !string.IsNullOrWhiteSpace(
        seedIdNumber
    ) &&
    !string.IsNullOrWhiteSpace(
        seedPhone
    ) &&
    !string.IsNullOrWhiteSpace(
        seedEmail
    ) &&
    !string.IsNullOrWhiteSpace(
        seedPassword
    );

if (seedConfigured)
{
    using var scope =
        app.Services.CreateScope();

    var context =
        scope.ServiceProvider
            .GetRequiredService<
                PhilaLinkDbContext
            >();

    var superAdminExists =
        await context.Users
            .AsNoTracking()
            .AnyAsync(
                user =>
                    user.Role ==
                        RoleNames.SuperAdmin
            );

    if (!superAdminExists)
    {
        var now =
            DateTime.UtcNow;

        var adminUserId =
            Guid.NewGuid();

        var adminUser =
            new User
            {
                Id =
                    adminUserId,

                FullName =
                    seedFullName!,

                IdNumber =
                    seedIdNumber!,

                PhoneNumber =
                    seedPhone!,

                Email =
                    seedEmail!,

                PasswordHash =
                    BCrypt.Net.BCrypt
                        .HashPassword(
                            seedPassword!
                        ),

                Role =
                    RoleNames.SuperAdmin,

                IsActive =
                    true,

                IsVerified =
                    true,

                VerifiedAt =
                    now,

                MustChangePassword =
                    false,

                CreatedAt =
                    now
            };

        var admin =
            new Admin
            {
                UserId =
                    adminUserId,

                FullName =
                    seedFullName!,

                Email =
                    seedEmail!,

                ClinicId =
                    null,

                CreatedAt =
                    now
            };

        context.Users.Add(
            adminUser
        );

        context.Admins.Add(
            admin
        );

        await context.SaveChangesAsync();
    }
}
else
{
    Console.WriteLine(
        "SuperAdmin seed skipped. " +
        "Seed:SuperAdmin* configuration is incomplete."
    );
}

// =====================================================
// HTTP PIPELINE
// =====================================================

if (
    app.Environment
        .IsDevelopment()
)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(
    "FrontendOnly"
);

/*
 * Authentication must run before the rate limiter.
 *
 * This allows protected endpoints to be partitioned using
 * the authenticated user's ID rather than placing everyone
 * behind the same public IP into one shared bucket.
 */
app.UseAuthentication();

app.UseRateLimiter();

app.UseAuthorization();

// =====================================================
// HEALTH
// =====================================================

/*
 * Lightweight Render/frontend health endpoint.
 *
 * It deliberately performs no database or external API call.
 * The global limiter gives it a generous 120 requests/minute
 * per client IP.
 */
app.MapGet(
        "/api/health",
        () =>
            Results.Ok(
                new
                {
                    status =
                        "healthy",

                    timestamp =
                        DateTime.UtcNow
                }
            )
    )
    .AllowAnonymous();

app.MapControllers();

app.Run();