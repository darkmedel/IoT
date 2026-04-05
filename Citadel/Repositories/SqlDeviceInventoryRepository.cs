using cl.MedelCodeFactory.IoT.Citadel.Contracts;
using cl.MedelCodeFactory.IoT.Citadel.Infrastructure.Persistence;
using Dapper;

namespace cl.MedelCodeFactory.IoT.Citadel.Repositories;

public sealed class SqlDeviceInventoryRepository : IDeviceInventoryRepository
{
    private readonly CitadelDbConnectionFactory _connectionFactory;

    public SqlDeviceInventoryRepository(CitadelDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<DeviceResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        const string sql = @"
SELECT
    DeviceId,
    TipoHardwareId,
    Nombre,
    Descripcion,
    SerialNumber,
    FirmwareVersion,
    MacAddress,
    Habilitado,
    FechaRegistroInventario
FROM dbo.Device
ORDER BY FechaRegistroInventario DESC, DeviceId;";

        using var connection = _connectionFactory.CreateConnection();
        var command = new CommandDefinition(sql, cancellationToken: cancellationToken);
        var rows = await connection.QueryAsync<DeviceResponse>(command);
        return rows.AsList();
    }

    public async Task<DeviceResponse?> GetByIdAsync(string deviceId, CancellationToken cancellationToken)
    {
        const string sql = @"
SELECT
    DeviceId,
    TipoHardwareId,
    Nombre,
    Descripcion,
    SerialNumber,
    FirmwareVersion,
    MacAddress,
    Habilitado,
    FechaRegistroInventario
FROM dbo.Device
WHERE DeviceId = @DeviceId;";

        using var connection = _connectionFactory.CreateConnection();
        var command = new CommandDefinition(sql, new { DeviceId = deviceId }, cancellationToken: cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<DeviceResponse>(command);
    }

    public async Task<bool> ExistsAsync(string deviceId, CancellationToken cancellationToken)
    {
        const string sql = @"
SELECT CASE WHEN EXISTS(
    SELECT 1
    FROM dbo.Device
    WHERE DeviceId = @DeviceId
) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END;";

        using var connection = _connectionFactory.CreateConnection();
        var command = new CommandDefinition(sql, new { DeviceId = deviceId }, cancellationToken: cancellationToken);
        return await connection.ExecuteScalarAsync<bool>(command);
    }

    public async Task<bool> HardwareTypeExistsAsync(int tipoHardwareId, CancellationToken cancellationToken)
    {
        const string sql = @"
SELECT CASE WHEN EXISTS(
    SELECT 1
    FROM dbo.TipoHardware
    WHERE Id = @TipoHardwareId
) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END;";

        using var connection = _connectionFactory.CreateConnection();
        var command = new CommandDefinition(sql, new { TipoHardwareId = tipoHardwareId }, cancellationToken: cancellationToken);
        return await connection.ExecuteScalarAsync<bool>(command);
    }

    public async Task<DeviceResponse> CreateAsync(CreateDeviceRequest request, CancellationToken cancellationToken)
    {
        const string sql = @"
INSERT INTO dbo.Device
(
    DeviceId,
    TipoHardwareId,
    Nombre,
    Descripcion,
    SerialNumber,
    FirmwareVersion,
    MacAddress,
    Habilitado,
    FechaRegistroInventario,
    UsuarioCreacion,
    FechaCreacion,
    UsuarioModificacion,
    FechaModificacion
)
OUTPUT INSERTED.DeviceId,
       INSERTED.TipoHardwareId,
       INSERTED.Nombre,
       INSERTED.Descripcion,
       INSERTED.SerialNumber,
       INSERTED.FirmwareVersion,
       INSERTED.MacAddress,
       INSERTED.Habilitado,
       INSERTED.FechaRegistroInventario
VALUES
(
    @DeviceId,
    @TipoHardwareId,
    @Nombre,
    @Descripcion,
    @SerialNumber,
    @FirmwareVersion,
    @MacAddress,
    1,
    SYSUTCDATETIME(),
    @Usuario,
    SYSUTCDATETIME(),
    @Usuario,
    SYSUTCDATETIME()
);";

        using var connection = _connectionFactory.CreateConnection();
        var command = new CommandDefinition(
            sql,
            new
            {
                request.DeviceId,
                request.TipoHardwareId,
                request.Nombre,
                request.Descripcion,
                request.SerialNumber,
                request.FirmwareVersion,
                request.MacAddress,
                Usuario = string.IsNullOrWhiteSpace(request.Usuario) ? "system" : request.Usuario
            },
            cancellationToken: cancellationToken);

        return await connection.QuerySingleAsync<DeviceResponse>(command);
    }
}
