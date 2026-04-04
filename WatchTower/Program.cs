using cl.MedelCodeFactory.IoT.WatchTower.Repositories;
using cl.MedelCodeFactory.IoT.WatchTower.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IMonitoringQueryRepository, SqlMonitoringQueryRepository>();
builder.Services.AddSingleton<MonitoringQueryService>();
builder.Services.AddSingleton<AlertService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/", () => Results.Ok(new
{
    service = "IoT.WatchTower",
    status = "Healthy",
    timestampUtc = DateTime.UtcNow
}));

app.MapGet("/health", () => Results.Ok(new
{
    service = "IoT.WatchTower",
    status = "Healthy",
    timestampUtc = DateTime.UtcNow
}));

app.MapGet("/queries/devices/offline", async (MonitoringQueryService service) =>
{
    var result = await service.GetOfflineDevicesAsync();
    return Results.Ok(result);
});

app.MapGet("/queries/devices/degraded", async (MonitoringQueryService service) =>
{
    var result = await service.GetDegradedDevicesAsync();
    return Results.Ok(result);
});

app.MapGet("/queries/devices/{deviceId}/history", async (string deviceId, MonitoringQueryService service) =>
{
    var result = await service.GetHistoryAsync(deviceId);
    return Results.Ok(result);
});

app.Run();