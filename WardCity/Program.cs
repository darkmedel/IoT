using cl.MedelCodeFactory.IoT.WardCity.Repositories;
using cl.MedelCodeFactory.IoT.WardCity.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient<BifrostConfigClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:BifrostBaseUrl"]!);
});

builder.Services.AddSingleton<IDeviceConfigurationRepository, SqlDeviceConfigurationRepository>();
builder.Services.AddSingleton<ICommandQueueRepository, SqlCommandQueueRepository>();

builder.Services.AddSingleton<DeviceConfigurationService>();
builder.Services.AddSingleton<CommandQueueService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/", () => Results.Ok(new
{
    service = "IoT.WardCity",
    status = "Healthy",
    timestampUtc = DateTime.UtcNow
}));

app.MapGet("/health", () => Results.Ok(new
{
    service = "IoT.WardCity",
    status = "Healthy",
    timestampUtc = DateTime.UtcNow
}));

app.MapPost("/devices/{deviceId}/button-config", async (
    string deviceId,
    cl.MedelCodeFactory.IoT.Common.Contracts.Commands.ButtonConfigRequest request,
    DeviceConfigurationService configurationService,
    BifrostConfigClient bifrostClient) =>
{
    if (!string.Equals(deviceId, request.DeviceId, StringComparison.OrdinalIgnoreCase))
    {
        return Results.BadRequest(new { message = "Route deviceId and body DeviceId do not match." });
    }

    await configurationService.SaveAsync(request);
    var dispatchResult = await bifrostClient.SendButtonConfigAsync(request);

    return Results.Ok(new
    {
        persisted = true,
        dispatch = dispatchResult
    });
});

app.Run();