using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Npgsql;
using PersonalProject.Data;
using PersonalProject.Middleware;
using PersonalProject.Models;
using PersonalProject.Models.Constants;
using PersonalProject.Models.Entities;
using PersonalProject.Security;
using PersonalProject.Services;
using PersonalProject.Services.AI;
using PersonalProject.Services.Implementations;
using PersonalProject.Services.Interfaces;
using System.Security.Claims;
using System.Text;

var builder =
    WebApplication.CreateBuilder(
        args
    );

// =====================================================
// SERVER HARDENING
// =====================================================

builder.WebHost.ConfigureKestrel(
    options =>
    {
        options.AddServerHeader =
            false;

        options.Limits.MaxRequestBodySize =
            2 * 1024 * 1024;
    }
);

builder.Logging.AddFilter(
    "System.Net.Http.HttpClient",
    LogLevel.Warning
);

// =====================================================
// SECURITY CONFIGURATION
// =====================================================

var securitySettings =
    ProductionSecurityConfiguration.Build(
        builder.Configuration,
        builder.Environment.EnvironmentName
    );

// =====================================================
// DATABASE
// =====================================================

var connectionStringBuilder =
    new NpgsqlConnectionStringBuilder(
        securitySettings.ConnectionString
    )
    {
        KeepAlive = 30,
        ConnectionIdleLifetime = 300,
        ConnectionPruningInterval = 10
    };

/*
 * The interceptor automatically revokes existing JWTs
 * whenever security-sensitive User fields change.
 */
builder.Services.AddScoped<
    TokenVersionSaveChangesInterceptor
>();

builder.Services.AddDbContext<
    PhilaLinkDbContext
>(
    (
        serviceProvider,
        options
    ) =>
    {
        options.UseNpgsql(
            connectionStringBuilder
                .ConnectionString,
            npgsqlOptions =>
            {
                npgsqlOptions
                    .EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay:
                            TimeSpan
                                .FromSeconds(5),
                        errorCodesToAdd:
                            null
                    );
            }
        );

        options.AddInterceptors(
            serviceProvider
                .GetRequiredService<
                    TokenVersionSaveChangesInterceptor
                >()
        );
    }
);

// =====================================================
// DEPENDENCY INJECTION
// =====================================================

/*
 * Register the original AuthService as a concrete service.
 *
 * SessionAwareAuthService implements IAuthService and wraps
 * AuthService so every returned application JWT receives
 * the current TokenVersion.
 */
builder.Services.AddScoped<
    AuthService
>();

builder.Services.AddScoped<
    IAuthService,
    SessionAwareAuthService
>();

builder.Services.AddScoped<
    ISessionService,
    SessionService
>();

builder.Services.AddScoped<
    IUserService,
    UserService
>();

builder.Services.AddScoped<
    IProxyService,
    ProxyService
>();

builder.Services.AddScoped<
    IMedicationService,
    MedicationService
>();

builder.Services.AddScoped<
    IClinicService,
    ClinicService
>();

builder.Services.AddScoped<
    INotificationService,
    NotificationService
>();

builder.Services.AddScoped<
    IAuditLogService,
    AuditLogService
>();

builder.Services.AddScoped<
    ISymptomAssessmentService,
    SymptomAssessmentService
>();

builder.Services.AddScoped<
    IOtpVerificationService,
    OtpVerificationService
>();

builder.Services.AddScoped<
    IPatientService,
    PatientService
>();

builder.Services.AddScoped<
    IAdminService,
    AdminService
>();

builder.Services.AddScoped<
    IAppointmentService,
    AppointmentService
>();

builder.Services.AddScoped<
    IMedicationCollectionService,
    MedicationCollectionService
>();

builder.Services.AddScoped<
    IClinicStockService,
    ClinicStockService
>();

builder.Services.AddScoped<
    INurseService,
    NurseService
>();

builder.Services.AddScoped<
    IChatbotService,
    ChatbotService
>();

builder.Services.AddScoped<
    ILegalDocumentService,
    LegalDocumentService
>();

// =====================================================
// EXTERNAL HTTP APIS
// =====================================================

builder.Services.AddTransient<
    SanitizedExternalApiHandler
>();

builder.Services
    .AddHttpClient<
        IChatbotProvider,
        GeminiChatbotProvider
    >(
        client =>
        {
            client.Timeout =
                TimeSpan.FromSeconds(
                    45
                );
        }
    )
    .AddHttpMessageHandler<
        SanitizedExternalApiHandler
    >();

builder.Services
    .AddHttpClient<
        IWeatherService,
        WeatherService
    >(
        client =>
        {
            client.BaseAddress =
                new Uri(
                    "https://api.openweathermap.org"
                );

            client.Timeout =
                TimeSpan.FromSeconds(
                    15
                );
        }
    )
    .AddHttpMessageHandler<
        SanitizedExternalApiHandler
    >();

// =====================================================
// CONTROLLERS / LOCALIZATION
// =====================================================

builder.Services.AddControllers();

PersonalProject.Localization
    .PhilaLinkLocalization
    .AddPhilaLinkLocalization(
        builder.Services
    );

builder.Services
    .AddEndpointsApiExplorer();

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
                Title =
                    "PhilaLink API",

                Version =
                    "v1"
            }
        );

        options.AddSecurityDefinition(
            "Bearer",
            new OpenApiSecurityScheme
            {
                Name =
                    "Authorization",

                Type =
                    SecuritySchemeType.Http,

                Scheme =
                    "Bearer",

                BearerFormat =
                    "JWT",

                In =
                    ParameterLocation.Header,

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
                    ] =
                        new List<string>()
                }
        );
    }
);

// =====================================================
// CORS
// =====================================================

builder.Services.AddCors(
    options =>
    {
        options.AddPolicy(
            "FrontendOnly",
            policy =>
            {
                policy
                    .WithOrigins(
                        securitySettings
                            .AllowedOrigins
                            .ToArray()
                    )
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .SetPreflightMaxAge(
                        TimeSpan.FromHours(
                            1
                        )
                    );
            }
        );
    }
);

// =====================================================
// FORWARDED HEADERS
// =====================================================

builder.Services.Configure<
    ForwardedHeadersOptions
>(
    options =>
    {
        options.ForwardedHeaders =
            ForwardedHeaders
                .XForwardedFor
            |
            ForwardedHeaders
                .XForwardedProto;

        options.ForwardLimit =
            2;

        options.RequireHeaderSymmetry =
            false;

        /*
         * Render uses dynamic ingress proxies.
         */
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
    }
);

// =====================================================
// HSTS
// =====================================================

builder.Services.AddHsts(
    options =>
    {
        options.Preload =
            false;

        options.IncludeSubDomains =
            true;

        options.MaxAge =
            TimeSpan.FromDays(
                365
            );
    }
);

// =====================================================
// RATE LIMITING
// =====================================================

builder.Services
    .AddPhilaLinkRateLimiting();

// =====================================================
// JWT AUTHENTICATION
// =====================================================

var jwtKey =
    Encoding.UTF8.GetBytes(
        securitySettings.JwtKey
    );

builder.Services
    .AddAuthentication(
        JwtBearerDefaults
            .AuthenticationScheme
    )
    .AddJwtBearer(
        options =>
        {
            options.IncludeErrorDetails =
                builder.Environment
                    .IsDevelopment();

            options.TokenValidationParameters =
                new TokenValidationParameters
                {
                    ValidateIssuer =
                        true,

                    ValidIssuer =
                        securitySettings
                            .JwtIssuer,

                    ValidateAudience =
                        true,

                    ValidAudience =
                        securitySettings
                            .JwtAudience,

                    ValidateIssuerSigningKey =
                        true,

                    IssuerSigningKey =
                        new SymmetricSecurityKey(
                            jwtKey
                        ),

                    ValidateLifetime =
                        true,

                    RequireExpirationTime =
                        true,

                    RequireSignedTokens =
                        true,

                    ClockSkew =
                        TimeSpan.Zero
                };

            // =================================================
            // LIVE SESSION / TOKEN VERSION VALIDATION
            // =================================================

            options.Events =
                new JwtBearerEvents
                {
                    OnTokenValidated =
                        async context =>
                        {
                            var principal =
                                context.Principal;

                            var userIdValue =
                                principal
                                    ?.FindFirstValue(
                                        ClaimTypes
                                            .NameIdentifier
                                    );

                            var tokenVersionValue =
                                principal
                                    ?.FindFirstValue(
                                        "tokenVersion"
                                    );

                            var roleValue =
                                principal
                                    ?.FindFirstValue(
                                        ClaimTypes.Role
                                    );

                            var passwordStateValue =
                                principal
                                    ?.FindFirstValue(
                                        "mustChangePassword"
                                    );

                            if (
                                !Guid.TryParse(
                                    userIdValue,
                                    out var userId
                                )
                                ||
                                !int.TryParse(
                                    tokenVersionValue,
                                    out var tokenVersion
                                )
                            )
                            {
                                context.Fail(
                                    "Invalid session claims."
                                );

                                return;
                            }

                            var db =
                                context.HttpContext
                                    .RequestServices
                                    .GetRequiredService<
                                        PhilaLinkDbContext
                                    >();

                            /*
                             * Fetch only the fields required for
                             * authorization/session validation.
                             */
                            var state =
                                await db.Users
                                    .AsNoTracking()
                                    .Where(
                                        user =>
                                            user.Id ==
                                            userId
                                    )
                                    .Select(
                                        user =>
                                            new
                                            {
                                                user.IsActive,
                                                user.IsVerified,
                                                user.Role,
                                                user.MustChangePassword,
                                                user.TokenVersion
                                            }
                                    )
                                    .FirstOrDefaultAsync(
                                        context.HttpContext
                                            .RequestAborted
                                    );

                            if (
                                state == null
                                ||
                                !state.IsActive
                                ||
                                !RoleNames.IsValid(
                                    state.Role
                                )
                                ||
                                state.TokenVersion !=
                                    tokenVersion
                                ||
                                !string.Equals(
                                    state.Role,
                                    roleValue,
                                    StringComparison.Ordinal
                                )
                                ||
                                !string.Equals(
                                    state.MustChangePassword
                                        ? "true"
                                        : "false",
                                    passwordStateValue,
                                    StringComparison
                                        .OrdinalIgnoreCase
                                )
                                ||
                                (
                                    state.Role ==
                                        RoleNames.Patient
                                    &&
                                    !state.IsVerified
                                )
                            )
                            {
                                context.Fail(
                                    "Session has been revoked."
                                );
                            }
                        }
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
                policy
                    .RequireAuthenticatedUser()
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
    )
    &&
    !string.IsNullOrWhiteSpace(
        seedIdNumber
    )
    &&
    !string.IsNullOrWhiteSpace(
        seedPhone
    )
    &&
    !string.IsNullOrWhiteSpace(
        seedEmail
    )
    &&
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

                TokenVersion =
                    1,

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

        await context
            .SaveChangesAsync();
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

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();

    app.UseMiddleware<
        SecurityHeadersMiddleware
    >();
}

app.UseMiddleware<
    GlobalExceptionMiddleware
>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(
    "FrontendOnly"
);

PersonalProject.Localization
    .PhilaLinkLocalization
    .UsePhilaLinkLocalization(
        app
    );

app.UseAuthentication();

app.UseRateLimiter();

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