using DeviceManagementAPI.Hubs; // ✅ import your hub
using DeviceManagementAPI.Interfaces;
using DeviceManagementAPI.Services;
using DeviceManagementAPI.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});

builder.Services.AddScoped<IDeviceService, DeviceServiceAdoNet>();

// ✅ Add SignalR
builder.Services.AddSignalR();

// ✅ Register RequestCounterService as Singleton (shared dictionary)
builder.Services.AddSingleton<RequestCounterService>();

// ✅ Register middleware
builder.Services.AddTransient<RequestCounterMiddleware>();

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

app.UseRouting();

// Use CORS before Authorization
app.UseCors("AllowFrontend");

// ✅ Add custom middleware to count requests
app.UseMiddleware<RequestCounterMiddleware>();

app.UseAuthorization();

app.MapControllers();

// ✅ Map the hub
app.MapHub<DeviceHub>("/deviceHub");

app.Run();
