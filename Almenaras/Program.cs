using cl.MedelCodeFactory.IoT.Almenaras.Options;
using cl.MedelCodeFactory.IoT.Almenaras.Repositories;
using cl.MedelCodeFactory.IoT.Almenaras.Services;
using cl.MedelCodeFactory.IoT.Common.Contracts.Heartbeat;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();

builder.Services.Configure<HeartbeatRulesOptions>(
    builder.Configuration.GetSection("HeartbeatRules"));

builder.Services.AddSingleton<IHeartbeatRepository, SqlHeartbeatRepository>();
builder.Services.AddSingleton<HeartbeatStatusEvaluator>();
builder.Services.AddSingleton<HeartbeatIngestionService>();

var app = builder.Build();

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
    HeartbeatRequest request,
    HeartbeatIngestionService service) =>
{
    var result = await service.ProcessAsync(request);
    return Results.Ok(result);
});

app.Run();