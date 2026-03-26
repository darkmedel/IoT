using cl.MedelCodeFactory.IoT.HeartBeat.DTOs;
using cl.MedelCodeFactory.IoT.HeartBeat.Options;
using cl.MedelCodeFactory.IoT.HeartBeat.Repositories;
using cl.MedelCodeFactory.IoT.HeartBeat.Services;

var builder = WebApplication.CreateBuilder(args);

// 🔥 IMPORTANTE: escuchar en red local (para ESP32)
builder.WebHost.UseUrls("http://0.0.0.0:5009");

// 🔧 Configuración DB
builder.Services.Configure<DatabaseOptions>(options =>
{
    options.ConnectionString = builder.Configuration.GetConnectionString("IoTMonitoreo")
        ?? throw new InvalidOperationException("No se encontró la cadena de conexión IoTMonitoreo.");
});

// 🔧 DI
builder.Services.AddScoped<IHeartbeatRepository, SqlHeartbeatRepository>();
builder.Services.AddScoped<IHeartbeatService, HeartbeatService>();

// 🔧 Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 🔧 Swagger solo en desarrollo
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ==============================
// 🏠 ROOT
// ==============================
app.MapGet("/", () => Results.Ok(new
{
    service = "IoT.HeartBeat",
    message = "Servicio activo",
    endpoints = new[]
    {
        "/health",
        "/heartbeat",
        "/swagger"
    },
    timestampUtc = DateTime.UtcNow
}));

// ==============================
// ❤️ HEALTH CHECK
// ==============================
app.MapGet("/health", () => Results.Ok(new
{
    service = "IoT.HeartBeat",
    status = "Healthy",
    timestampUtc = DateTime.UtcNow
}));

// ==============================
// 📡 HEARTBEAT
// ==============================
app.MapPost("/heartbeat",
    async (
        HeartbeatRequest request,
        IHeartbeatService heartbeatService,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken) =>
    {
        ILogger logger = loggerFactory.CreateLogger("HeartbeatEndpoint");

        if (request == null)
        {
            return Results.BadRequest(new
            {
                ack = false,
                message = "Payload inválido."
            });
        }

        if (string.IsNullOrWhiteSpace(request.DeviceId))
        {
            return Results.BadRequest(new
            {
                ack = false,
                message = "deviceId es obligatorio."
            });
        }

        try
        {
            HeartbeatAckResponse response =
                await heartbeatService.ProcessAsync(request, cancellationToken);

            return Results.Ok(response);
        }
        catch (InvalidOperationException ex) when (ex.Message == "DEVICE_NOT_REGISTERED")
        {
            logger.LogWarning("Heartbeat rechazado por dispositivo no registrado. DeviceId: {DeviceId}", request.DeviceId);

            return Results.NotFound(new
            {
                ack = false,
                deviceId = request.DeviceId,
                message = "El dispositivo no está registrado en inventario."
            });
        }
        catch (UnauthorizedAccessException ex) when (ex.Message == "DEVICE_DISABLED")
        {
            logger.LogWarning("Heartbeat rechazado por dispositivo deshabilitado. DeviceId: {DeviceId}", request.DeviceId);

            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error procesando heartbeat. DeviceId: {DeviceId}", request.DeviceId);

            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    });

app.Run();