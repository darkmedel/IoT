using cl.MedelCodeFactory.IoT.Almenaras.Options;
using cl.MedelCodeFactory.IoT.Almenaras.Repositories;
using cl.MedelCodeFactory.IoT.Almenaras.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<HeartbeatRulesOptions>(
    builder.Configuration.GetSection("HeartbeatRules"));

builder.Services.AddSingleton<IHeartbeatRepository, SqlHeartbeatRepository>();
builder.Services.AddSingleton<HeartbeatStatusEvaluator>();
builder.Services.AddSingleton<HeartbeatIngestionService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/", () => Results.Ok(new
{
    service = "IoT.Almenaras",
    status = "Healthy",
    timestampUtc = DateTime.UtcNow
}));

app.MapGet("/health", () => Results.Ok(new
{
    service = "IoT.Almenaras",
    status = "Healthy",
    timestampUtc = DateTime.UtcNow
}));

app.MapPost("/heartbeat", async (
    cl.MedelCodeFactory.IoT.Common.Contracts.Heartbeat.HeartbeatRequest request,
    HeartbeatIngestionService service) =>
{
    var result = await service.ProcessAsync(request);
    return Results.Ok(result);
});

app.MapGet("/devices/{deviceId}", async (string deviceId, IHeartbeatRepository repository) =>
{
    var device = await repository.GetCurrentByDeviceIdAsync(deviceId);
    return device is null ? Results.NotFound() : Results.Ok(device);
});

app.MapGet("/devices/offline", async (IHeartbeatRepository repository) =>
{
    var devices = await repository.GetOfflineDevicesAsync();
    return Results.Ok(devices);
});

app.MapGet("/devices/degraded", async (IHeartbeatRepository repository) =>
{
    var devices = await repository.GetDegradedDevicesAsync();
    return Results.Ok(devices);
});

app.Run();