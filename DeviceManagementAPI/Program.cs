using DeviceManagementAPI.Data;
using DeviceManagementAPI.Entities;
using DeviceManagementAPI.Hubs;
using DeviceManagementAPI.Interfaces;
using DeviceManagementAPI.Middleware;
using DeviceManagementAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ----------------------- JWT Authentication -----------------------
var key = builder.Configuration["Jwt:Key"] ?? "supersecretkey12345678901234567890";

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
        RoleClaimType = ClaimTypes.Role
    };

    // ✅ For WebSocket (SignalR)
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            // Normal cookie usage for APIs
            if (context.Request.Cookies.ContainsKey("jwt"))
            {
                context.Token = context.Request.Cookies["jwt"];
            }

            // ✅ Also allow token from query string (SignalR WebSocket fallback)
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/deviceHub"))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        }
    };
});


// ----------------------- Controllers & Swagger -----------------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer {your token}'"
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

// ----------------------- Services -----------------------
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
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<DeviceHub>("/deviceHub");

// ----------------------- SEED ADMIN USER -----------------------
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<DeviceDbContext>();

    // Apply pending migrations automatically
    context.Database.Migrate();

    // Check if Admin exists
    if (!context.Users.Any(u => u.Role == "Admin"))
    {
        var admin = new User
        {
            Username = "admin",
            Role = "Admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123") // match login logic
        };
        context.Users.Add(admin);
        context.SaveChanges();

        Console.WriteLine("Seeded Admin user successfully!"); 
    }
    else
    {
        Console.WriteLine("Admin user already exists. Skipping seed...");
    }
}

// ----------------------- Run App -----------------------
app.Run();
