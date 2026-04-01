using cl.MedelCodeFactory.IoT.GateWay.Endpoints;
using cl.MedelCodeFactory.IoT.GateWay.Services;
using cl.MedelCodeFactory.IoT.GateWay.WebSockets;

var builder = WebApplication.CreateBuilder(args);

// Servicios
builder.Services.AddSingleton<MessageProcessor>();
builder.Services.AddSingleton<WebSocketConnectionHandler>();
builder.Services.AddSingleton<MessageDeduplicationService>();
builder.Services.AddSingleton<ConnectionRegistry>();
builder.Services.AddSingleton<DeviceCommandSender>();

var app = builder.Build();

// WebSockets
app.UseWebSockets();

// Health básico
app.MapGet("/", () => Results.Ok(new
{
    service = "IoT.GateWay",
    status = "running",
    timestampUtc = DateTime.UtcNow
}));

// WebSocket endpoint
app.Map("/ws", async context =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsync("WebSocket request expected.");
        return;
    }

    var handler = context.RequestServices.GetRequiredService<WebSocketConnectionHandler>();
    await handler.HandleAsync(context);
});

// Consulta simple de dispositivos conectados
app.MapGet("/devices", (ConnectionRegistry registry) =>
{
    var devices = registry
        .GetAll()
        .Select(x => new
        {
            x.ConnectionId,
            x.DeviceId,
            x.RemoteIp,
            x.ConnectedAtUtc,
            WebSocketState = x.WebSocket?.State.ToString()
        });

    return Results.Ok(devices);
});

// Endpoints HTTP del dominio Device
app.MapDeviceEndpoints();

app.Run("http://0.0.0.0:5000");