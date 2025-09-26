using DeviceManagementAPI.Hubs;
using DeviceManagementAPI.Interfaces;
using DeviceManagementAPI.Services;
using DeviceManagementAPI.Middleware;
using DeviceManagementAPI.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add EF Core
builder.Services.AddDbContext<DeviceDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register EF service instead of ADO.NET
builder.Services.AddScoped<IDeviceService, DeviceServiceEf>();

// SignalR
builder.Services.AddSignalR();

// Request counter service
builder.Services.AddSingleton<RequestCounterService>();
builder.Services.AddTransient<RequestCounterMiddleware>();

builder.Configuration.AddUserSecrets<Program>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy => policy.WithOrigins("http://localhost:3000")
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("AllowFrontend");
app.UseMiddleware<RequestCounterMiddleware>();
app.UseAuthorization();

app.MapControllers();
app.MapHub<DeviceHub>("/deviceHub");

app.Run();
