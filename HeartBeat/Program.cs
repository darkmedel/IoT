using cl.MedelCodeFactory.IoT.HeartBeat.Configuration;
using cl.MedelCodeFactory.IoT.HeartBeat.DTOs;
using cl.MedelCodeFactory.IoT.HeartBeat.Repositories;
using cl.MedelCodeFactory.IoT.HeartBeat.Services;

var builder = WebApplication.CreateBuilder(args);

// ================================
// Configuration
// ================================

builder.Services.Configure<HeartbeatOptions>(
    builder.Configuration.GetSection("Heartbeat"));

// ================================
// Logging
// ================================

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// ================================
// DI - Repositories
// ================================

builder.Services.AddScoped<IHeartbeatRepository, SqlHeartbeatRepository>();

// ================================
// DI - Services
// ================================

builder.Services.AddScoped<IHeartbeatService, HeartbeatService>();
builder.Services.AddScoped<IOperationalStatusEvaluator, OperationalStatusEvaluator>();

// ================================
// Swagger (opcional pero recomendado)
// ================================

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ================================
// Build
// ================================

var app = builder.Build();

// ================================
// Middleware
// ================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ================================
// Endpoints
// ================================

app.MapGet("/health", () =>
{
    return Results.Ok(new
    {
        status = "ok",
        service = "IoT.HeartBeat",
        timestamp = DateTime.UtcNow
    });
});

app.MapPost("/heartbeat", async (
    HeartbeatRequestDTO request,
    IHeartbeatService service,
    CancellationToken cancellationToken) =>
{
    var result = await service.ProcessAsync(request, cancellationToken);

    if (!result.Success)
    {
        return Results.Json(
            new
            {
                code = result.Code,
                message = result.Message,
                deviceId = result.DeviceId
            },
            statusCode: result.StatusCode);
    }

    return Results.Ok(new HeartbeatAckResponseDTO
    {
        DeviceId = result.DeviceId!,
        Status = result.OperationalStatus!,
        ReceivedAtUtc = result.ReceivedAtUtc ?? DateTime.UtcNow
    });
});

app.Run();