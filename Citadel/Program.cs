using cl.MedelCodeFactory.IoT.Citadel.Infrastructure.Persistence.Repositories;
using cl.MedelCodeFactory.IoT.Citadel.Repositories;
using cl.MedelCodeFactory.IoT.Citadel.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient<BifrostOtaClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:BifrostBaseUrl"]!);
});

builder.Services.AddSingleton<IDeviceInventoryRepository, SqlDeviceInventoryRepository>();
builder.Services.AddSingleton<IEmpresaRepository, SqlEmpresaRepository>();
builder.Services.AddSingleton<IDeviceAssignmentRepository, SqlDeviceAssignmentRepository>();
builder.Services.AddSingleton<IFirmwareRepository, SqlFirmwareRepository>();

builder.Services.AddSingleton<DeviceInventoryService>();
builder.Services.AddSingleton<DeviceAssignmentService>();
builder.Services.AddSingleton<FirmwareCatalogService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/", () => Results.Ok(new
{
    service = "IoT.Citadel",
    status = "Healthy",
    timestampUtc = DateTime.UtcNow
}));

app.MapGet("/health", () => Results.Ok(new
{
    service = "IoT.Citadel",
    status = "Healthy",
    timestampUtc = DateTime.UtcNow
}));

app.MapPost("/devices", async (
    cl.MedelCodeFactory.IoT.Common.Contracts.Devices.DeviceRegistrationRequest request,
    DeviceInventoryService service) =>
{
    var result = await service.CreateAsync(request);
    return Results.Ok(result);
});

app.MapPost("/empresas", async (
    cl.MedelCodeFactory.IoT.Common.Contracts.Devices.EmpresaRequest request,
    IEmpresaRepository repository) =>
{
    var result = await repository.CreateAsync(request);
    return Results.Ok(result);
});

app.MapPost("/devices/{deviceId}/assign", async (
    string deviceId,
    cl.MedelCodeFactory.IoT.Common.Contracts.Devices.DeviceAssignmentRequest request,
    DeviceAssignmentService service) =>
{
    if (!string.Equals(deviceId, request.DeviceId, StringComparison.OrdinalIgnoreCase))
    {
        return Results.BadRequest(new { message = "Route deviceId and body DeviceId do not match." });
    }

    var result = await service.AssignAsync(request);
    return Results.Ok(result);
});

app.MapPost("/firmware", async (
    cl.MedelCodeFactory.IoT.Common.Contracts.Firmware.FirmwareCreateRequest request,
    FirmwareCatalogService service) =>
{
    var result = await service.CreateAsync(request);
    return Results.Ok(result);
});

app.MapPost("/devices/{deviceId}/firmware/apply", async (
    string deviceId,
    cl.MedelCodeFactory.IoT.Common.Contracts.Firmware.FirmwareApplyRequest request,
    BifrostOtaClient bifrostClient) =>
{
    if (!string.Equals(deviceId, request.DeviceId, StringComparison.OrdinalIgnoreCase))
    {
        return Results.BadRequest(new { message = "Route deviceId and body DeviceId do not match." });
    }

    var result = await bifrostClient.ApplyOtaAsync(request);
    return Results.Ok(result);
});

app.Run();