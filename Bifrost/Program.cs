using cl.MedelCodeFactory.IoT.Bifrost.Application;
using cl.MedelCodeFactory.IoT.Bifrost.Infrastructure;
using cl.MedelCodeFactory.IoT.Bifrost.WebSockets;
using cl.MedelCodeFactory.IoT.Bifrost.Contracts;
using cl.MedelCodeFactory.IoT.Bifrost.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:5000");

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSingleton<MessageDeduplicationService>();
builder.Services.AddSingleton<ConnectionRegistry>();
builder.Services.AddSingleton<MessageProcessor>();
builder.Services.AddSingleton<WebSocketConnectionHandler>();
builder.Services.AddSingleton<DeviceCommandSender>();
builder.Services.AddSingleton<IButtonEventRepository, NullButtonEventRepository>();

var app = builder.Build();

app.UseWebSockets();

app.MapGet("/", () => Results.Ok(new
{
    service = "IoT.Bifrost",
    status = "Healthy",
    timestampUtc = DateTime.UtcNow
}));

app.MapGet("/health", () => Results.Ok(new
{
    service = "IoT.Bifrost",
    status = "Healthy",
    timestampUtc = DateTime.UtcNow
}));

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

app.MapGet("/devices", (ConnectionRegistry registry) =>
{
    var devices = registry.GetAll();
    return Results.Ok(devices.Select(x => new
    {
        x.ConnectionId,
        x.DeviceId,
        x.RemoteIp,
        x.ConnectedAtUtc,
        WebSocketState = x.WebSocket?.State.ToString()
    }));
});

app.MapPost("/devices/{deviceId}/cmd/status", async (string deviceId, DeviceCommandSender sender) =>
{
    var result = await sender.SendAsync(deviceId, "CMD|STATUS");
    return result.Success ? Results.Ok(result) : Results.NotFound(result);
});

app.MapPost("/devices/{deviceId}/cmd/reboot", async (string deviceId, DeviceCommandSender sender) =>
{
    var result = await sender.SendAsync(deviceId, "CMD|REBOOT");
    return result.Success ? Results.Ok(result) : Results.NotFound(result);
});

app.Run();