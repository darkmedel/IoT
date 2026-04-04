using cl.MedelCodeFactory.IoT.Almenaras.Configuration;
using cl.MedelCodeFactory.IoT.Almenaras.Endpoints;
using cl.MedelCodeFactory.IoT.Almenaras.Repositories;
using cl.MedelCodeFactory.IoT.Almenaras.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();

builder.Services.Configure<HeartbeatOptions>(
    builder.Configuration.GetSection("Heartbeat"));

builder.Services.AddSingleton<IHeartbeatRepository, SqlHeartbeatRepository>();
builder.Services.AddSingleton<IOperationalStatusEvaluator, OperationalStatusEvaluator>();
builder.Services.AddSingleton<IHeartbeatService, HeartbeatService>();
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

app.MapHeartbeatEndpoints();

app.Run();