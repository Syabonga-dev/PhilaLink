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
using System.Text;

var builder =
    WebApplication.CreateBuilder(args);

// =====================================================
// SERVER HARDENING
// =====================================================

builder.WebHost.ConfigureKestrel(
    options =>
    {
        /*
         * Do not disclose the Kestrel server implementation.
         */
        options.AddServerHeader =
            false;

        /*
         * PhilaLink currently accepts structured JSON requests,
         * not large file uploads.
         *
         * Keep a conservative global body limit to reduce
         * memory/resource-exhaustion abuse.
         */
        options.Limits.MaxRequestBodySize =
            2 * 1024 * 1024;
    }
);

/*
 * External HTTP APIs use secret keys in server-side requests.
 *
 * Suppress the normal informational HttpClient request logging
 * so complete external request URLs are not written to logs.
 */
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
        KeepAlive =
            30,

        ConnectionIdleLifetime =
            300,

        ConnectionPruningInterval =
            10
    };

builder.Services.AddDbContext<
    PhilaLinkDbContext
>(
    options =>
        options.UseNpgsql(
            connectionStringBuilder
                .ConnectionString,
            npgsqlOptions =>
            {
                npgsqlOptions
                    .EnableRetryOnFailure(
                        maxRetryCount:
                            3,

                        maxRetryDelay:
                            TimeSpan
                                .FromSeconds(
                                    5
                                ),

                        errorCodesToAdd:
                            null
                    );
            }
        )
);

// =====================================================
// DEPENDENCY INJECTION
// =====================================================

builder.Services.AddScoped<
    IAuthService,
    AuthService
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
                    SecuritySchemeType
                        .Http,

                Scheme =
                    "Bearer",

                BearerFormat =
                    "JWT",

                In =
                    ParameterLocation
                        .Header,

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
        /*
         * Render terminates HTTPS in front of the application.
         *
         * Process the platform's forwarded protocol and address
         * before HSTS, authentication and rate limiting.
         */
        options.ForwardedHeaders =
            ForwardedHeaders
                .XForwardedFor |
            ForwardedHeaders
                .XForwardedProto;

        options.ForwardLimit =
            2;

        options.RequireHeaderSymmetry =
            false;

        /*
         * Render's ingress proxy addresses are dynamic.
         *
         * The application is only exposed through Render's
         * platform ingress, so accept forwarded headers from
         * that platform instead of maintaining a static list.
         */
        options.KnownIPNetworks
            .Clear();

        options.KnownProxies
            .Clear();
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
            /*
             * Production clients should not receive token
             * validation internals in WWW-Authenticate details.
             */
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

/*
 * This must run first so Render's external HTTPS scheme and
 * actual client address are available to later middleware.
 */
app.UseForwardedHeaders();

/*
 * Catch every exception that escaped a controller and return a
 * safe response instead of framework/database implementation
 * details.
 */
app.UseMiddleware<
    GlobalExceptionMiddleware
>();

if (
    !app.Environment
        .IsDevelopment()
)
{
    app.UseHsts();

    app.UseMiddleware<
        SecurityHeadersMiddleware
    >();
}

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
 * Resolve CurrentCulture and CurrentUICulture from
 * Accept-Language before authenticated controllers run.
 */
PersonalProject.Localization
    .PhilaLinkLocalization
    .UsePhilaLinkLocalization(
        app
    );

/*
 * Authentication must run before the global rate limiter so
 * authenticated requests can be partitioned by user ID.
 */
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
