using DeviceManagementAPI.Data;
using DeviceManagementAPI.Hubs;
using DeviceManagementAPI.Interfaces;
using DeviceManagementAPI.Middleware;
using DeviceManagementAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ----------------------- JWT Authentication -----------------------
var key = builder.Configuration["Jwt:Key"] ?? "supersecretkey12345678901234567890"; // fallback key

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
        RoleClaimType = ClaimTypes.Role // <- ensures "Admin"/"User" roles are recognized
    };
});

// ----------------------- Controllers & Swagger -----------------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // JWT Auth in Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer {your token}' to authorize"
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
            new string[] {}
        }
    });
});

// ----------------------- EF Core -----------------------
builder.Services.AddDbContext<DeviceDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register EF service instead of ADO.NET
builder.Services.AddScoped<IDeviceService, DeviceServiceEf>();

// ----------------------- SignalR -----------------------
builder.Services.AddSignalR();

// ----------------------- Middleware -----------------------
builder.Services.AddSingleton<RequestCounterService>();
builder.Services.AddTransient<RequestCounterMiddleware>();

builder.Configuration.AddUserSecrets<Program>();

// ----------------------- CORS -----------------------
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy => policy.WithOrigins("http://localhost:3000")
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials());
});

// ----------------------- Build App -----------------------
var app = builder.Build();

// ----------------------- Swagger -----------------------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ----------------------- Middleware Pipeline -----------------------
app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("AllowFrontend");
app.UseMiddleware<RequestCounterMiddleware>();

// Authentication & Authorization
app.UseAuthentication();  // Must be before UseAuthorization
app.UseAuthorization();

// ----------------------- Map Endpoints -----------------------
app.MapControllers();
app.MapHub<DeviceHub>("/deviceHub");

// ----------------------- Run App -----------------------
app.Run();
