using cl.MedelCodeFactory.IoT.HeartBeat.DTOs;
using cl.MedelCodeFactory.IoT.HeartBeat.Repositories;
using cl.MedelCodeFactory.IoT.HeartBeat.Services;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://0.0.0.0:5009");

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IHeartbeatRepository, SqlHeartbeatRepository>();
builder.Services.AddScoped<IHeartbeatService, HeartbeatService>();
builder.Services.AddScoped<IOperationalStatusEvaluator, OperationalStatusEvaluator>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/health", () => Results.Ok(new
{
    service = "IoT.HeartBeat",
    status = "OK",
    timestamp = DateTime.UtcNow
}));

app.MapPost("/heartbeat", async (
    HeartbeatRequestDTO request,
    IHeartbeatService service,
    CancellationToken ct) =>
{
    var result = await service.ProcessAsync(request, ct);

    if (!result.Success)
    {
        return Results.BadRequest(new
        {
            success = false,
            code = result.Code,
            message = result.Message
        });
    }

    return Results.Ok(new
    {
        success = true,
        code = result.Code,
        deviceId = result.DeviceId,
        status = result.OperationalStatus,
        receivedAtUtc = result.ReceivedAtUtc
    });
});

app.Run();