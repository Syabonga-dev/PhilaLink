using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;
using PhilaLink_Backend.Data;
using PhilaLink_Backend.Models;
using PhilaLink_Backend.Models.Entities;
using PhilaLink_Backend.Services.Implementations;
using PhilaLink_Backend.Services.Interfaces;
using System.Text;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddDbContext<PhilaLinkDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));



builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IProxyService, ProxyService>();
builder.Services.AddScoped<IMedicationService, MedicationService>();
builder.Services.AddScoped<IClinicService, ClinicService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IMedicationScheduleService, MedicationScheduleService>();
builder.Services.AddScoped<IMedicationLogService, MedicationLogService>();
builder.Services.AddScoped<ISymptomAssessmentService, SymptomAssessmentService>();
builder.Services.AddScoped<IOtpVerificationService, OtpVerificationService>();


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();


builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "PhilaLink API",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT token like: Bearer {token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});


builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
});



var jwtKey = builder.Configuration["Jwt:Key"];

if (string.IsNullOrEmpty(jwtKey))
{
    throw new Exception("JWT Key is missing in appsettings.json");
}


var key = Encoding.UTF8.GetBytes(jwtKey);


builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };
    });


builder.Services.AddAuthorization();



var app = builder.Build();



using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<PhilaLinkDbContext>();

    if (!context.Users.Any(u => u.Role == "Admin"))
    {
        var adminUserId = Guid.NewGuid();

        var adminUser = new User
        {
            Id = adminUserId,
            FullName = "System Administrator",
            PhoneNumber = "0000000000",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@2004.."),
            Role = "Admin",
            CreatedAt = DateTime.UtcNow
        };


        var admin = new Admin
        {
            UserId = adminUserId,
            FullName = "System Administrator",
            Email = "admin@philalink.com",
            CreatedAt = DateTime.UtcNow
        };


        context.Users.Add(adminUser);
        context.Admins.Add(admin);

        context.SaveChanges();
    }
}



if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}



app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();