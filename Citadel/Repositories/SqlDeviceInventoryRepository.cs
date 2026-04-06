using System.Data;
using Dapper;
using cl.MedelCodeFactory.IoT.Citadel.Contracts;
using cl.MedelCodeFactory.IoT.Citadel.Infrastructure.Persistence;
using cl.MedelCodeFactory.IoT.Citadel.Services;

namespace cl.MedelCodeFactory.IoT.Citadel.Repositories;

public sealed class SqlDeviceInventoryRepository : IDeviceInventoryRepository
{
    private readonly CitadelDbConnectionFactory _connectionFactory;
    private readonly ICurrentUserService _currentUserService;

    public SqlDeviceInventoryRepository(
        CitadelDbConnectionFactory connectionFactory,
        ICurrentUserService currentUserService)
    {
        _connectionFactory = connectionFactory;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<DeviceResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                d.DeviceId,
                d.TipoHardwareId,
                d.FirmwareId,
                d.FirmwareVersion,
                d.Habilitado,
                d.FechaRegistroInventario
            FROM dbo.Device d
            ORDER BY d.FechaRegistroInventario DESC, d.DeviceId ASC;
            """;

        using IDbConnection connection = _connectionFactory.CreateConnection();

        var command = new CommandDefinition(sql, cancellationToken: cancellationToken);
        var rows = await connection.QueryAsync<DeviceResponse>(command);

        return rows.AsList();
    }

    public async Task<DeviceResponse?> GetByIdAsync(string deviceId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                d.DeviceId,
                d.TipoHardwareId,
                d.FirmwareId,
                d.FirmwareVersion,
                d.Habilitado,
                d.FechaRegistroInventario
            FROM dbo.Device d
            WHERE d.DeviceId = @DeviceId;
            """;

        using IDbConnection connection = _connectionFactory.CreateConnection();

        var command = new CommandDefinition(
            sql,
            new { DeviceId = NormalizeDeviceId(deviceId) },
            cancellationToken: cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<DeviceResponse>(command);
    }

    public async Task<bool> ExistsAsync(string deviceId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT CASE
                WHEN EXISTS
                (
                    SELECT 1
                    FROM dbo.Device d
                    WHERE d.DeviceId = @DeviceId
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

    public async Task<bool> HardwareTypeExistsAsync(int tipoHardwareId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT CASE
                WHEN EXISTS
                (
                    SELECT 1
                    FROM dbo.TipoHardware th
                    WHERE th.Id = @TipoHardwareId
                      AND th.Habilitado = 1
                )
                THEN CAST(1 AS bit)
                ELSE CAST(0 AS bit)
            END;
            """;

        using IDbConnection connection = _connectionFactory.CreateConnection();

        var command = new CommandDefinition(
            sql,
            new { TipoHardwareId = tipoHardwareId },
            cancellationToken: cancellationToken);

        return await connection.ExecuteScalarAsync<bool>(command);
    }

    public async Task<bool> FirmwareExistsAsync(int firmwareId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT CASE
                WHEN EXISTS
                (
                    SELECT 1
                    FROM dbo.Firmware f
                    WHERE f.Id = @FirmwareId
                      AND f.Habilitado = 1
                )
                THEN CAST(1 AS bit)
                ELSE CAST(0 AS bit)
            END;
            """;

        using IDbConnection connection = _connectionFactory.CreateConnection();

        var command = new CommandDefinition(
            sql,
            new { FirmwareId = firmwareId },
            cancellationToken: cancellationToken);

        return await connection.ExecuteScalarAsync<bool>(command);
    }

    public async Task<bool> FirmwareBelongsToHardwareAsync(int firmwareId, int tipoHardwareId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT CASE
                WHEN EXISTS
                (
                    SELECT 1
                    FROM dbo.Firmware f
                    WHERE f.Id = @FirmwareId
                      AND f.TipoHardwareId = @TipoHardwareId
                      AND f.Habilitado = 1
                )
                THEN CAST(1 AS bit)
                ELSE CAST(0 AS bit)
            END;
            """;

        using IDbConnection connection = _connectionFactory.CreateConnection();

        var command = new CommandDefinition(
            sql,
            new
            {
                FirmwareId = firmwareId,
                TipoHardwareId = tipoHardwareId
            },
            cancellationToken: cancellationToken);

        return await connection.ExecuteScalarAsync<bool>(command);
    }

    public async Task<DeviceResponse> CreateAsync(CreateDeviceRequest request, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO dbo.Device
            (
                DeviceId,
                TipoHardwareId,
                FirmwareId,
                FirmwareVersion,
                Habilitado,
                FechaRegistroInventario,
                UsuarioCreacion,
                FechaCreacion,
                UsuarioModificacion,
                FechaModificacion
            )
            OUTPUT
                INSERTED.DeviceId,
                INSERTED.TipoHardwareId,
                INSERTED.FirmwareId,
                INSERTED.FirmwareVersion,
                INSERTED.Habilitado,
                INSERTED.FechaRegistroInventario
            VALUES
            (
                @DeviceId,
                @TipoHardwareId,
                @FirmwareId,
                @FirmwareVersion,
                1,
                SYSUTCDATETIME(),
                @Usuario,
                SYSUTCDATETIME(),
                @Usuario,
                SYSUTCDATETIME()
            );
            """;

        using IDbConnection connection = _connectionFactory.CreateConnection();

        var command = new CommandDefinition(
            sql,
            new
            {
                DeviceId = NormalizeDeviceId(request.DeviceId),
                TipoHardwareId = request.TipoHardwareId,
                FirmwareId = request.FirmwareId,
                FirmwareVersion = NormalizeFirmwareVersion(request.FirmwareVersion),
                Usuario = ResolveUsuario(request.Usuario)
            },
            cancellationToken: cancellationToken);

        return await connection.QuerySingleAsync<DeviceResponse>(command);
    }

    public async Task<DeviceResponse> UpdateAsync(string deviceId, CreateDeviceRequest request, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE dbo.Device
            SET
                TipoHardwareId = @TipoHardwareId,
                FirmwareId = @FirmwareId,
                FirmwareVersion = @FirmwareVersion,
                Habilitado = @Habilitado,
                UsuarioModificacion = @Usuario,
                FechaModificacion = SYSUTCDATETIME()
            OUTPUT
                INSERTED.DeviceId,
                INSERTED.TipoHardwareId,
                INSERTED.FirmwareId,
                INSERTED.FirmwareVersion,
                INSERTED.Habilitado,
                INSERTED.FechaRegistroInventario
            WHERE DeviceId = @DeviceId;
            """;

        using IDbConnection connection = _connectionFactory.CreateConnection();

        var command = new CommandDefinition(
            sql,
            new
            {
                DeviceId = NormalizeDeviceId(deviceId),
                TipoHardwareId = request.TipoHardwareId,
                FirmwareId = request.FirmwareId,
                FirmwareVersion = NormalizeFirmwareVersion(request.FirmwareVersion),
                Habilitado = request.Habilitado,
                Usuario = ResolveUsuario(request.Usuario)
            },
            cancellationToken: cancellationToken);

        return await connection.QuerySingleAsync<DeviceResponse>(command);
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

    private static string NormalizeFirmwareVersion(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? "UNKNOWN"
            : value.Trim();
    }
}