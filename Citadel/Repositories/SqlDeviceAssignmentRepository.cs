using System.Data;
using Dapper;
using cl.MedelCodeFactory.IoT.Citadel.Contracts;
using cl.MedelCodeFactory.IoT.Citadel.Infrastructure.Persistence;
using cl.MedelCodeFactory.IoT.Citadel.Services;

namespace cl.MedelCodeFactory.IoT.Citadel.Repositories;

public sealed class SqlDeviceAssignmentRepository : IDeviceAssignmentRepository
{
    private readonly CitadelDbConnectionFactory _connectionFactory;
    private readonly ICurrentUserService _currentUserService;

    public SqlDeviceAssignmentRepository(
        CitadelDbConnectionFactory connectionFactory,
        ICurrentUserService currentUserService)
    {
        _connectionFactory = connectionFactory;
        _currentUserService = currentUserService;
    }

    public async Task<bool> HasActiveAssignmentAsync(string deviceId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT CASE
                WHEN EXISTS
                (
                    SELECT 1
                    FROM dbo.EmpresaDispositivo ed
                    WHERE ed.DeviceId = @DeviceId
                      AND ed.Habilitado = 1
                      AND ed.FechaDesasignacion IS NULL
                )
                THEN CAST(1 AS bit)
                ELSE CAST(0 AS bit)
            END;
            """;

        using IDbConnection connection = _connectionFactory.CreateConnection();

        var command = new CommandDefinition(
            sql,
            new { DeviceId = NormalizeDeviceId(deviceId) },
            cancellationToken: cancellationToken);

        return await connection.ExecuteScalarAsync<bool>(command);
    }

    public async Task<DeviceEmpresaResponse?> GetCurrentAssignmentAsync(string deviceId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT TOP (1)
                ed.DeviceId,
                ed.EmpresaId,
                e.Nombre AS EmpresaNombre,
                ed.NombreDispositivo,
                ed.Descripcion,
                ed.FechaRegistro AS FechaRegistroUtc
            FROM dbo.EmpresaDispositivo ed
            INNER JOIN dbo.Empresa e
                ON e.Id = ed.EmpresaId
            WHERE ed.DeviceId = @DeviceId
              AND ed.Habilitado = 1
              AND ed.FechaDesasignacion IS NULL
            ORDER BY ed.FechaRegistro DESC, ed.Id DESC;
            """;

        using IDbConnection connection = _connectionFactory.CreateConnection();

        var command = new CommandDefinition(
            sql,
            new { DeviceId = NormalizeDeviceId(deviceId) },
            cancellationToken: cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<DeviceEmpresaResponse>(command);
    }

    public async Task<DeviceEmpresaResponse> AssignAsync(CreateAssignmentRequest request, CancellationToken cancellationToken)
    {
        const string sql = """
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
                ed.NombreDispositivo,
                ed.Descripcion,
                ed.FechaRegistro AS FechaRegistroUtc
            FROM dbo.EmpresaDispositivo ed
            INNER JOIN dbo.Empresa e
                ON e.Id = ed.EmpresaId
            WHERE ed.DeviceId = @DeviceId
              AND ed.Habilitado = 1
              AND ed.FechaDesasignacion IS NULL
            ORDER BY ed.Id DESC;
            """;

        using IDbConnection connection = _connectionFactory.CreateConnection();

        var command = new CommandDefinition(
            sql,
            new
            {
                EmpresaId = request.EmpresaId,
                DeviceId = NormalizeDeviceId(request.DeviceId),
                NombreDispositivo = request.NombreDispositivo.Trim(),
                Descripcion = request.Descripcion.Trim(),
                Usuario = ResolveUsuario(request.Usuario)
            },
            cancellationToken: cancellationToken);

        return await connection.QuerySingleAsync<DeviceEmpresaResponse>(command);
    }

    public async Task<UnassignDeviceResponse> UnassignAsync(string deviceId, string? usuario, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE dbo.EmpresaDispositivo
            SET
                Habilitado = 0,
                FechaDesasignacion = SYSUTCDATETIME(),
                UsuarioModificacion = @Usuario,
                FechaModificacion = SYSUTCDATETIME()
            WHERE DeviceId = @DeviceId
              AND Habilitado = 1
              AND FechaDesasignacion IS NULL;

            IF @@ROWCOUNT = 0
            BEGIN
                SELECT
                    @DeviceId AS DeviceId,
                    CAST(0 AS bit) AS Unassigned,
                    CAST(NULL AS datetime2(7)) AS FechaDesasignacionUtc,
                    CAST('El dispositivo no tiene asignación activa.' AS varchar(200)) AS Message;
            END
            ELSE
            BEGIN
                SELECT TOP (1)
                    @DeviceId AS DeviceId,
                    CAST(1 AS bit) AS Unassigned,
                    ed.FechaDesasignacion AS FechaDesasignacionUtc,
                    CAST('Desasignación realizada correctamente.' AS varchar(200)) AS Message
                FROM dbo.EmpresaDispositivo ed
                WHERE ed.DeviceId = @DeviceId
                ORDER BY ed.Id DESC;
            END
            """;

        using IDbConnection connection = _connectionFactory.CreateConnection();

        var normalizedDeviceId = NormalizeDeviceId(deviceId);

        var command = new CommandDefinition(
            sql,
            new
            {
                DeviceId = normalizedDeviceId,
                Usuario = ResolveUsuario(usuario)
            },
            cancellationToken: cancellationToken);

        return await connection.QuerySingleAsync<UnassignDeviceResponse>(command);
    }

    private string ResolveUsuario(string? requestUsuario)
    {
        if (!string.IsNullOrWhiteSpace(requestUsuario))
        {
            return requestUsuario.Trim();
        }

        return _currentUserService.GetCurrentUser();
    }

    private static string NormalizeDeviceId(string? value)
    {
        return (value ?? string.Empty).Trim().ToUpperInvariant();
    }
}
