using BCrypt.Net;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using PersonalProject.Data;
using PersonalProject.Models;
using PersonalProject.Models.Constants;
using PersonalProject.Models.Entities;
using PersonalProject.Services.Implementations;
using PersonalProject.Services.Interfaces;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// =====================================================
// DATABASE
// =====================================================

builder.Services.AddDbContext<PhilaLinkDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

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

// =====================================================
// CONTROLLERS
// =====================================================

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// =====================================================
// SWAGGER
// =====================================================

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "PhilaLink API",
        Version = "v1"
    });

    options.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter JWT token like: Bearer {token}"
        }
    );

    options.AddSecurityRequirement(document =>
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
});

// =====================================================
// CORS
// =====================================================

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "AllowAll",
        policy =>
        {
            policy
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
        }
    );
});

// =====================================================
// JWT AUTHENTICATION
// =====================================================

var jwtKey = builder.Configuration["Jwt:Key"];

if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException(
        "JWT Key is missing in configuration."
    );
}

var key = Encoding.UTF8.GetBytes(jwtKey);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,

                ValidateAudience = false,

                ValidateIssuerSigningKey = true,

                ValidateLifetime = true,

                ClockSkew = TimeSpan.Zero,

                ValidIssuer =
                    builder.Configuration["Jwt:Issuer"],

                IssuerSigningKey =
                    new SymmetricSecurityKey(key)
            };
    });

// =====================================================
// AUTHORIZATION POLICIES
// =====================================================

builder.Services.AddAuthorization(options =>
{
    // System-wide administration only.
    options.AddPolicy(
        "SuperAdminOnly",
        policy =>
            policy.RequireRole(RoleNames.SuperAdmin)
    );

    // Both administrative roles.
    options.AddPolicy(
        "AdminOnly",
        policy =>
            policy.RequireRole(
                RoleNames.SuperAdmin,
                RoleNames.ClinicAdmin
            )
    );

    // Clinic operations.
    options.AddPolicy(
        "ClinicStaff",
        policy =>
            policy.RequireRole(
                RoleNames.ClinicAdmin,
                RoleNames.Nurse
            )
    );

    options.AddPolicy(
        "PatientOnly",
        policy =>
            policy.RequireRole(RoleNames.Patient)
    );

    options.AddPolicy(
        "ProxyOnly",
        policy =>
            policy.RequireRole(RoleNames.Proxy)
    );

    // Useful for endpoints that may be accessed by
    // either the patient or their authorised proxy.
    options.AddPolicy(
        "PatientOrProxy",
        policy =>
            policy.RequireRole(
                RoleNames.Patient,
                RoleNames.Proxy
            )
    );
});

var app = builder.Build();

// =====================================================
// SUPER ADMIN SEED
// =====================================================

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<PhilaLinkDbContext>();

    /*
     * During the architecture migration there may still be an old
     * User with Role = "Admin".
     *
     * We no longer create new generic Admin users.
     */

    var superAdminExists = await context.Users.AnyAsync(u => u.Role == RoleNames.SuperAdmin);

    if (!superAdminExists)
    {
        var seedFullName =
            builder.Configuration["Seed:SuperAdminFullName"];

        var seedIdNumber =
            builder.Configuration["Seed:SuperAdminIdNumber"];

        var seedPhone =
            builder.Configuration["Seed:SuperAdminPhoneNumber"];

        var seedEmail =
            builder.Configuration["Seed:SuperAdminEmail"];

        var seedPassword =
            builder.Configuration["Seed:SuperAdminPassword"];

        /*
         * We don't store a SuperAdmin password in source code.
         *
         * If seed configuration has not been supplied,
         * simply skip automatic account creation.
         */
        var seedConfigured =
            !string.IsNullOrWhiteSpace(seedFullName) &&
            !string.IsNullOrWhiteSpace(seedIdNumber) &&
            !string.IsNullOrWhiteSpace(seedPhone) &&
            !string.IsNullOrWhiteSpace(seedEmail) &&
            !string.IsNullOrWhiteSpace(seedPassword);

        if (seedConfigured)
        {
            var adminUserId = Guid.NewGuid();

            var adminUser = new User
            {
                Id = adminUserId,

                FullName = seedFullName!,

                IdNumber = seedIdNumber!,

                PhoneNumber = seedPhone!,

                Email = seedEmail!,

                PasswordHash =
                    BCrypt.Net.BCrypt.HashPassword(
                        seedPassword!
                    ),

                Role = RoleNames.SuperAdmin,

                IsActive = true,

                CreatedAt = DateTime.UtcNow
            };

            var admin = new Admin
            {
                UserId = adminUserId,

                FullName = seedFullName!,

                Email = seedEmail!,

                // SuperAdmin is not clinic-scoped.
                ClinicId = null,

                CreatedAt = DateTime.UtcNow
            };

            context.Users.Add(adminUser);
            context.Admins.Add(admin);

            await context.SaveChangesAsync();
        }
        else
        {
            Console.WriteLine(
                "SuperAdmin seed skipped. " +
                "Seed:SuperAdmin* configuration is incomplete."
            );
        }
    }
}

// =====================================================
// HTTP PIPELINE
// =====================================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHttpsRedirection();
}

app.UseCors("AllowAll");

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();