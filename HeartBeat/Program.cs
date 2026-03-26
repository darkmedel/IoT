using cl.MedelCodeFactory.IoT.HeartBeat.Configuration;
using cl.MedelCodeFactory.IoT.HeartBeat.DTOs;
using cl.MedelCodeFactory.IoT.HeartBeat.Repositories;
using cl.MedelCodeFactory.IoT.HeartBeat.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<HeartbeatOptions>(
    builder.Configuration.GetSection("Heartbeat"));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IHeartbeatRepository, SqlHeartbeatRepository>();
builder.Services.AddSingleton<IOperationalStatusEvaluator, OperationalStatusEvaluator>();
builder.Services.AddSingleton<IHeartbeatService, HeartbeatService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapPost("/heartbeat", async (
    HeartbeatRequestDTO request,
    IHeartbeatService heartbeatService,
    ILoggerFactory loggerFactory,
    CancellationToken cancellationToken) =>
{
    ILogger logger = loggerFactory.CreateLogger("HeartbeatEndpoint");

    try
    {
        var result = await heartbeatService.ProcessHeartbeatAsync(request, cancellationToken);

        if (!result.Success)
        {
            var error = new ApiErrorResponseDTO
            {
                Success = false,
                Code = result.Code,
                Message = result.Message,
                DeviceId = result.DeviceId
            };

            return Results.Json(error, statusCode: result.StatusCode);
        }

        if (string.IsNullOrWhiteSpace(result.DeviceId) ||
            string.IsNullOrWhiteSpace(result.OperationalStatus) ||
            !result.ReceivedAtUtc.HasValue)
        {
            logger.LogError(
                "Resultado inconsistente al procesar heartbeat. Success={Success}, DeviceId={DeviceId}, OperationalStatus={OperationalStatus}, ReceivedAtUtc={ReceivedAtUtc}",
                result.Success,
                result.DeviceId,
                result.OperationalStatus,
                result.ReceivedAtUtc);

            return Results.Json(
                new ApiErrorResponseDTO
                {
                    Success = false,
                    Code = "INTERNAL_ERROR",
                    Message = "Se produjo una inconsistencia interna al procesar el heartbeat."
                },
                statusCode: StatusCodes.Status500InternalServerError);
        }

        var response = new HeartbeatAckResponseDTO
        {
            Success = true,
            Code = result.Code,
            Message = result.Message,
            DeviceId = result.DeviceId,
            Status = result.OperationalStatus,
            ReceivedAtUtc = result.ReceivedAtUtc.Value
        };

        return Results.Ok(response);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error no controlado al procesar heartbeat.");

        return Results.Json(
            new ApiErrorResponseDTO
            {
                Success = false,
                Code = "INTERNAL_ERROR",
                Message = "Se produjo un error interno al procesar el heartbeat."
            },
            statusCode: StatusCodes.Status500InternalServerError);
    }
});

app.MapGet("/devices/{deviceId}", async (
    string deviceId,
    IHeartbeatService heartbeatService,
    CancellationToken cancellationToken) =>
{
    var device = await heartbeatService.GetDeviceAsync(deviceId, cancellationToken);

    if (device == null)
    {
        return Results.Json(
            new ApiErrorResponseDTO
            {
                Success = false,
                Code = "DEVICE_NOT_FOUND",
                Message = "El dispositivo no existe en el inventario.",
                DeviceId = deviceId
            },
            statusCode: StatusCodes.Status404NotFound);
    }

    return Results.Ok(device);
});

app.MapGet("/devices/offline", async (
    IHeartbeatService heartbeatService,
    CancellationToken cancellationToken) =>
{
    var items = await heartbeatService.GetOfflineDevicesAsync(cancellationToken);
    return Results.Ok(items);
});

app.MapGet("/devices/degraded", async (
    IHeartbeatService heartbeatService,
    CancellationToken cancellationToken) =>
{
    var items = await heartbeatService.GetDegradedDevicesAsync(cancellationToken);
    return Results.Ok(items);
});

app.Run();