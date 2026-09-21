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
using System.Text;
using System.Threading.RateLimiting;

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
                        var forwardedFor =
                            httpContext
                                .Request
                                .Headers[
                                    "X-Forwarded-For"
                                ]
                                .FirstOrDefault();

                        var clientIp =
                            !string.IsNullOrWhiteSpace(
                                forwardedFor
                            )
                                ? forwardedFor
                                    .Split(
                                        ','
                                    )[0]
                                    .Trim()
                                : httpContext
                                        .Connection
                                        .RemoteIpAddress
                                        ?.ToString() ??
                                  "unknown";

                        var path =
                            httpContext
                                .Request
                                .Path
                                .Value
                                ?.ToLowerInvariant() ??
                            string.Empty;

                        return path switch
                        {
                            "/api/auth/login" =>
                                RateLimitPartition
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
                                    ),

                            "/api/auth/register" =>
                                RateLimitPartition
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
                                    ),

                            "/api/auth/otp/generate" =>
                                RateLimitPartition
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
                                    ),

                            "/api/auth/otp/verify" =>
                                RateLimitPartition
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
                                    ),

                            "/api/auth/password-reset/request" =>
                                RateLimitPartition
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
                                    ),

                            "/api/auth/password-reset/reset" =>
                                RateLimitPartition
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
                                    ),

                            "/api/auth/google-login" =>
                                RateLimitPartition
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
                                    ),

                            _ =>
                                RateLimitPartition
                                    .GetNoLimiter(
                                        $"unlimited:{clientIp}"
                                    )
                        };
                    }
                );

        options.OnRejected =
            async (
                context,
                cancellationToken
            ) =>
            {
                context
                    .HttpContext
                    .Response
                    .ContentType =
                        "application/json";

                await context
                    .HttpContext
                    .Response
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

app.UseRateLimiter();

app.UseAuthentication();

app.UseAuthorization();

// =====================================================
// HEALTH
// =====================================================

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