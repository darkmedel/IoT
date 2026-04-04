using System.Data;
using cl.MedelCodeFactory.IoT.WatchTower.Contracts;
using Microsoft.Data.SqlClient;

namespace cl.MedelCodeFactory.IoT.WatchTower.Repositories
{
    public sealed class SqlMonitoringQueryRepository : IMonitoringQueryRepository
    {
        private readonly string _connectionString;
        private readonly ILogger<SqlMonitoringQueryRepository> _logger;

        public SqlMonitoringQueryRepository(
            IConfiguration configuration,
            ILogger<SqlMonitoringQueryRepository> logger)
        {
            _logger = logger;
            _connectionString = configuration.GetConnectionString("IoTMonitoreo")
                ?? throw new InvalidOperationException("Connection string 'IoTMonitoreo' no está configurada.");
        }

        public async Task<IReadOnlyList<DeviceListItemDto>> GetDevicesAsync(
            string? status,
            int? empresaId,
            CancellationToken cancellationToken)
        {
            var result = new List<DeviceListItemDto>();

            const string sql = @"
SELECT
    dhc.DeviceId,
    ISNULL(emp.Nombre, 'Sin empresa') AS EmpresaNombre,
    dhc.OperationalStatus,
    dhc.LastHeartbeatReceivedAtUtc,
    dhc.Rssi,
    dhc.WsConnected,
    dhc.EventQueueSize,
    dhc.FreeHeap,
    dhc.IssuesJson
FROM IoT_Monitoreo.dbo.DeviceHeartbeatCurrent dhc
LEFT JOIN IoT_Common.dbo.EmpresaDispositivo ed
    ON ed.DeviceId = dhc.DeviceId
    AND ed.Habilitado = 1
    AND ed.FechaDesasignacion IS NULL
LEFT JOIN IoT_Common.dbo.Empresa emp
    ON emp.Id = ed.EmpresaId
    AND emp.Habilitado = 1
WHERE
    (@status IS NULL OR dhc.OperationalStatus = @status)
    AND (@empresaId IS NULL OR ed.EmpresaId = @empresaId)
ORDER BY
    dhc.LastHeartbeatReceivedAtUtc DESC,
    dhc.DeviceId ASC;";

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.Add(new SqlParameter("@status", SqlDbType.VarChar, 20)
            {
                Value = string.IsNullOrWhiteSpace(status) ? DBNull.Value : status.Trim()
            });
            command.Parameters.Add(new SqlParameter("@empresaId", SqlDbType.Int)
            {
                Value = empresaId.HasValue ? empresaId.Value : DBNull.Value
            });

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                result.Add(new DeviceListItemDto
                {
                    DeviceId = reader.GetString(reader.GetOrdinal("DeviceId")),
                    EmpresaNombre = reader.GetString(reader.GetOrdinal("EmpresaNombre")),
                    OperationalStatus = reader.GetString(reader.GetOrdinal("OperationalStatus")),
                    LastHeartbeatReceivedAtUtc = reader.IsDBNull(reader.GetOrdinal("LastHeartbeatReceivedAtUtc"))
                        ? null
                        : reader.GetDateTime(reader.GetOrdinal("LastHeartbeatReceivedAtUtc")),
                    Rssi = reader.IsDBNull(reader.GetOrdinal("Rssi"))
                        ? null
                        : reader.GetInt32(reader.GetOrdinal("Rssi")),
                    WsConnected = !reader.IsDBNull(reader.GetOrdinal("WsConnected")) &&
                                  reader.GetBoolean(reader.GetOrdinal("WsConnected")),
                    EventQueueSize = reader.IsDBNull(reader.GetOrdinal("EventQueueSize"))
                        ? 0
                        : reader.GetInt32(reader.GetOrdinal("EventQueueSize")),
                    FreeHeap = reader.IsDBNull(reader.GetOrdinal("FreeHeap"))
                        ? null
                        : reader.GetInt64(reader.GetOrdinal("FreeHeap")),
                    IssuesJson = reader.IsDBNull(reader.GetOrdinal("IssuesJson"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("IssuesJson"))
                });
            }

            return result;
        }

        public async Task<DeviceDetailDto?> GetDeviceByIdAsync(
            string deviceId,
            CancellationToken cancellationToken)
        {
            const string sql = @"
SELECT TOP (1)
    dhc.DeviceId,
    ISNULL(emp.Nombre, 'Sin empresa') AS EmpresaNombre,
    dhc.OperationalStatus,
    dhc.LastHeartbeatReceivedAtUtc,
    dhc.Uptime,
    dhc.Rssi,
    dhc.WsConnected,
    dhc.EventQueueSize,
    dhc.FreeHeap,
    dhc.IssuesJson
FROM IoT_Monitoreo.dbo.DeviceHeartbeatCurrent dhc
LEFT JOIN IoT_Common.dbo.EmpresaDispositivo ed
    ON ed.DeviceId = dhc.DeviceId
    AND ed.Habilitado = 1
    AND ed.FechaDesasignacion IS NULL
LEFT JOIN IoT_Common.dbo.Empresa emp
    ON emp.Id = ed.EmpresaId
    AND emp.Habilitado = 1
WHERE dhc.DeviceId = @deviceId;";

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.Add(new SqlParameter("@deviceId", SqlDbType.VarChar, 12)
            {
                Value = deviceId
            });

            await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            return new DeviceDetailDto
            {
                DeviceId = reader.GetString(reader.GetOrdinal("DeviceId")),
                EmpresaNombre = reader.GetString(reader.GetOrdinal("EmpresaNombre")),
                OperationalStatus = reader.GetString(reader.GetOrdinal("OperationalStatus")),
                LastHeartbeatReceivedAtUtc = reader.IsDBNull(reader.GetOrdinal("LastHeartbeatReceivedAtUtc"))
                    ? null
                    : reader.GetDateTime(reader.GetOrdinal("LastHeartbeatReceivedAtUtc")),
                Uptime = reader.IsDBNull(reader.GetOrdinal("Uptime"))
                    ? null
                    : reader.GetInt64(reader.GetOrdinal("Uptime")),
                Rssi = reader.IsDBNull(reader.GetOrdinal("Rssi"))
                    ? null
                    : reader.GetInt32(reader.GetOrdinal("Rssi")),
                WsConnected = !reader.IsDBNull(reader.GetOrdinal("WsConnected")) &&
                              reader.GetBoolean(reader.GetOrdinal("WsConnected")),
                EventQueueSize = reader.IsDBNull(reader.GetOrdinal("EventQueueSize"))
                    ? 0
                    : reader.GetInt32(reader.GetOrdinal("EventQueueSize")),
                FreeHeap = reader.IsDBNull(reader.GetOrdinal("FreeHeap"))
                    ? null
                    : reader.GetInt64(reader.GetOrdinal("FreeHeap")),
                IssuesJson = reader.IsDBNull(reader.GetOrdinal("IssuesJson"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("IssuesJson"))
            };
        }

        public async Task<IReadOnlyList<DeviceHistoryItemDto>> GetDeviceHistoryAsync(
            string deviceId,
            int limit,
            CancellationToken cancellationToken)
        {
            var result = new List<DeviceHistoryItemDto>();

            const string sql = @"
SELECT TOP (@limit)
    dhh.DeviceId,
    dhh.ReceivedAtUtc,
    dhh.OperationalStatus,
    dhh.Rssi,
    dhh.WsConnected,
    dhh.EventQueueSize,
    dhh.FreeHeap,
    dhh.IssuesJson
FROM IoT_Monitoreo.dbo.DeviceHeartbeatHistory dhh
WHERE dhh.DeviceId = @deviceId
ORDER BY dhh.ReceivedAtUtc DESC;";

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.Add(new SqlParameter("@deviceId", SqlDbType.VarChar, 12)
            {
                Value = deviceId
            });
            command.Parameters.Add(new SqlParameter("@limit", SqlDbType.Int)
            {
                Value = limit
            });

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                result.Add(new DeviceHistoryItemDto
                {
                    DeviceId = reader.GetString(reader.GetOrdinal("DeviceId")),
                    ReceivedAtUtc = reader.GetDateTime(reader.GetOrdinal("ReceivedAtUtc")),
                    OperationalStatus = reader.GetString(reader.GetOrdinal("OperationalStatus")),
                    Rssi = reader.IsDBNull(reader.GetOrdinal("Rssi"))
                        ? null
                        : reader.GetInt32(reader.GetOrdinal("Rssi")),
                    WsConnected = !reader.IsDBNull(reader.GetOrdinal("WsConnected")) &&
                                  reader.GetBoolean(reader.GetOrdinal("WsConnected")),
                    EventQueueSize = reader.IsDBNull(reader.GetOrdinal("EventQueueSize"))
                        ? 0
                        : reader.GetInt32(reader.GetOrdinal("EventQueueSize")),
                    FreeHeap = reader.IsDBNull(reader.GetOrdinal("FreeHeap"))
                        ? null
                        : reader.GetInt64(reader.GetOrdinal("FreeHeap")),
                    IssuesJson = reader.IsDBNull(reader.GetOrdinal("IssuesJson"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("IssuesJson"))
                });
            }

            return result;
        }
    }
}