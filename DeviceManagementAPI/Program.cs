using DeviceManagementAPI.Services;
using DeviceManagementAPI.Interfaces;
using DeviceManagementAPI.Hubs; // ✅ import your hub

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IDeviceService, DeviceServiceAdoNet>();

// ✅ Add SignalR
builder.Services.AddSignalR();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy => policy.WithOrigins("http://localhost:3000") // React dev server
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials()); // ✅ needed for SignalR
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Use CORS before Authorization
app.UseCors("AllowFrontend");

app.UseAuthorization();

app.MapControllers();

// ✅ Map the hub
app.MapHub<DeviceHub>("/deviceHub");

app.Run();
