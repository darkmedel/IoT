using cl.MedelCodeFactory.IoT.WatchTower.Configuration;
using cl.MedelCodeFactory.IoT.WatchTower.Endpoints;
using cl.MedelCodeFactory.IoT.WatchTower.Repositories;
using cl.MedelCodeFactory.IoT.WatchTower.Services;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:5090");

builder.Services.Configure<WatchTowerOptions>(
    builder.Configuration.GetSection(WatchTowerOptions.SectionName));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IMonitoringQueryRepository, SqlMonitoringQueryRepository>();
builder.Services.AddScoped<MonitoringQueryService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/health", () => Results.Ok(new
{
    service = "IoT.WatchTower",
    status = "Healthy",
    timestampUtc = DateTime.UtcNow
}));

app.MapGet("/api", () => Results.Ok(new
{
    service = "IoT.WatchTower",
    status = "Healthy",
    timestampUtc = DateTime.UtcNow,
    endpoints = new[]
    {
        "GET /health",
        "GET /api/devices",
        "GET /api/devices?status=Online",
        "GET /api/devices?status=Degraded",
        "GET /api/devices?status=Offline",
        "GET /api/devices?empresaId=1",
        "GET /api/devices/{deviceId}",
        "GET /api/devices/{deviceId}/history?limit=100"
    }
}));

app.MapDeviceStatusQueryEndpoints();

app.Run();