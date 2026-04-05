using cl.MedelCodeFactory.IoT.Citadel.Endpoints;
using cl.MedelCodeFactory.IoT.Citadel.Infrastructure.Persistence;
using cl.MedelCodeFactory.IoT.Citadel.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<CitadelDbConnectionFactory>();
builder.Services.AddSingleton<IEmpresaRepository, SqlEmpresaRepository>();
builder.Services.AddSingleton<IDeviceInventoryRepository, SqlDeviceInventoryRepository>();
builder.Services.AddSingleton<IDeviceAssignmentRepository, SqlDeviceAssignmentRepository>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();

app.MapGet("/", () => Results.Ok(new
{
    service = "IoT.Citadel",
    status = "Healthy",
    timestampUtc = DateTime.UtcNow,
    endpoints = new[]
    {
        "GET    /api/empresas",
        "POST   /api/empresas",
        "GET    /api/empresas/{id}",
        "GET    /api/devices",
        "POST   /api/devices",
        "GET    /api/devices/{deviceId}",
        "POST   /api/asignaciones",
        "POST   /api/asignaciones/{deviceId}/desasignar",
        "GET    /api/devices/{deviceId}/empresa"
    }
}));

app.MapGet("/health", () => Results.Ok(new
{
    service = "IoT.Citadel",
    status = "Healthy",
    timestampUtc = DateTime.UtcNow
}));

app.MapEmpresaEndpoints();
app.MapDeviceInventoryEndpoints();
app.MapDeviceAssignmentEndpoints();

app.Run();
