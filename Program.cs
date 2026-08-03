using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PhilaLink_Backend.Data;
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
builder.Services.AddSwaggerGen();



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

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
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