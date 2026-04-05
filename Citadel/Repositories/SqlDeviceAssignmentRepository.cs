using cl.MedelCodeFactory.IoT.Citadel.Contracts;
using cl.MedelCodeFactory.IoT.Citadel.Infrastructure.Persistence;
using Dapper;

namespace cl.MedelCodeFactory.IoT.Citadel.Repositories;

public sealed class SqlDeviceAssignmentRepository : IDeviceAssignmentRepository
{
    private readonly CitadelDbConnectionFactory _connectionFactory;

    public SqlDeviceAssignmentRepository(CitadelDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<bool> HasActiveAssignmentAsync(string deviceId, CancellationToken cancellationToken)
    {
        const string sql = @"
SELECT CASE WHEN EXISTS(
    SELECT 1
    FROM dbo.EmpresaDispositivo
    WHERE DeviceId = @DeviceId
      AND Habilitado = 1
      AND FechaDesasignacion IS NULL
) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END;";

        using var connection = _connectionFactory.CreateConnection();
        var command = new CommandDefinition(sql, new { DeviceId = deviceId }, cancellationToken: cancellationToken);
        return await connection.ExecuteScalarAsync<bool>(command);
    }

    public async Task<DeviceEmpresaResponse?> GetCurrentAssignmentAsync(string deviceId, CancellationToken cancellationToken)
    {
        const string sql = @"
SELECT TOP (1)
    ed.DeviceId,
    ed.EmpresaId,
    e.Nombre AS EmpresaNombre,
    ed.FechaRegistro AS FechaRegistroUtc
FROM dbo.EmpresaDispositivo ed
INNER JOIN dbo.Empresa e ON e.Id = ed.EmpresaId
WHERE ed.DeviceId = @DeviceId
  AND ed.Habilitado = 1
  AND ed.FechaDesasignacion IS NULL
ORDER BY ed.FechaRegistro DESC, ed.Id DESC;";

        using var connection = _connectionFactory.CreateConnection();
        var command = new CommandDefinition(sql, new { DeviceId = deviceId }, cancellationToken: cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<DeviceEmpresaResponse>(command);
    }

    public async Task<DeviceEmpresaResponse> AssignAsync(CreateAssignmentRequest request, CancellationToken cancellationToken)
    {
        const string sql = @"
INSERT INTO dbo.EmpresaDispositivo
(
    EmpresaId,
    DeviceId,
    NombreDispositivo,
    Descripcion,
    Habilitado,
    FechaRegistro,
    UsuarioCreacion,
    FechaCreacion,
    UsuarioModificacion,
    FechaModificacion
)
VALUES
(
    @EmpresaId,
    @DeviceId,
    @NombreDispositivo,
    @Descripcion,
    1,
    SYSUTCDATETIME(),
    @Usuario,
    SYSUTCDATETIME(),
    @Usuario,
    SYSUTCDATETIME()
);

SELECT TOP (1)
    ed.DeviceId,
    ed.EmpresaId,
    e.Nombre AS EmpresaNombre,
    ed.FechaRegistro AS FechaRegistroUtc
FROM dbo.EmpresaDispositivo ed
INNER JOIN dbo.Empresa e ON e.Id = ed.EmpresaId
WHERE ed.DeviceId = @DeviceId
  AND ed.Habilitado = 1
  AND ed.FechaDesasignacion IS NULL
ORDER BY ed.Id DESC;";

        using var connection = _connectionFactory.CreateConnection();
        var command = new CommandDefinition(
            sql,
            new
            {
                request.EmpresaId,
                request.DeviceId,
                request.NombreDispositivo,
                request.Descripcion,
                Usuario = string.IsNullOrWhiteSpace(request.Usuario) ? "system" : request.Usuario
            },
            cancellationToken: cancellationToken);

        return await connection.QuerySingleAsync<DeviceEmpresaResponse>(command);
    }

    public async Task<UnassignDeviceResponse> UnassignAsync(string deviceId, string? usuario, CancellationToken cancellationToken)
    {
        const string sql = @"
DECLARE @Rows TABLE (FechaDesasignacionUtc datetime2(7));

UPDATE dbo.EmpresaDispositivo
SET FechaDesasignacion = SYSUTCDATETIME(),
    Habilitado = 0,
    UsuarioModificacion = @Usuario,
    FechaModificacion = SYSUTCDATETIME()
OUTPUT INSERTED.FechaDesasignacion INTO @Rows(FechaDesasignacionUtc)
WHERE DeviceId = @DeviceId
  AND Habilitado = 1
  AND FechaDesasignacion IS NULL;

SELECT TOP (1)
    @DeviceId AS DeviceId,
    CAST(CASE WHEN EXISTS(SELECT 1 FROM @Rows) THEN 1 ELSE 0 END AS bit) AS Unassigned,
    (SELECT TOP (1) FechaDesasignacionUtc FROM @Rows) AS FechaDesasignacionUtc,
    CASE WHEN EXISTS(SELECT 1 FROM @Rows)
         THEN 'Device unassigned successfully.'
         ELSE 'No active assignment was found for the device.'
    END AS Message;";

        using var connection = _connectionFactory.CreateConnection();
        var command = new CommandDefinition(
            sql,
            new
            {
                DeviceId = deviceId,
                Usuario = string.IsNullOrWhiteSpace(usuario) ? "system" : usuario
            },
            cancellationToken: cancellationToken);

        return await connection.QuerySingleAsync<UnassignDeviceResponse>(command);
    }
}
