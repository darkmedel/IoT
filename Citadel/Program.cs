using cl.MedelCodeFactory.IoT.Citadel.Endpoints;
using cl.MedelCodeFactory.IoT.Citadel.Infrastructure.Persistence;
using cl.MedelCodeFactory.IoT.Citadel.Repositories;
using cl.MedelCodeFactory.IoT.Citadel.Services;

var builder = WebApplication.CreateBuilder(args);

// ================================
// Services
// ================================

builder.Services.AddRazorPages();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<CitadelDbConnectionFactory>();
builder.Services.AddSingleton<ICurrentUserService, CurrentUserService>();
builder.Services.AddSingleton<IEmpresaRepository, SqlEmpresaRepository>();
builder.Services.AddSingleton<IDeviceInventoryRepository, SqlDeviceInventoryRepository>();
builder.Services.AddSingleton<IDeviceAssignmentRepository, SqlDeviceAssignmentRepository>();

var app = builder.Build();

// ================================
// Middleware
// ================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// En desarrollo local puede comentarse si no usarás HTTPS.
// app.UseHttpsRedirection();

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRouting();

// ================================
// UI
// ================================

app.MapRazorPages();

// ================================
// Health
// ================================

app.MapGet("/health", () => Results.Ok(new
{
    service = "IoT.Citadel",
    status = "Healthy",
    timestampUtc = DateTime.UtcNow
}));

// ================================
// API
// ================================

app.MapEmpresaEndpoints();
app.MapDeviceInventoryEndpoints();
app.MapDeviceAssignmentEndpoints();

// Próxima iteración recomendada
// app.MapTipoHardwareEndpoints();
// app.MapFirmwareEndpoints();

app.Run();
