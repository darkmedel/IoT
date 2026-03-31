using System.Data;
using Microsoft.Data.SqlClient;
using cl.MedelCodeFactory.IoT.HeartBeat.DTOs;
using cl.MedelCodeFactory.IoT.HeartBeat.Models;

namespace cl.MedelCodeFactory.IoT.HeartBeat.Repositories
{
    public sealed class SqlDeviceHeartbeatRepository : IHeartbeatRepository
    {
        private readonly string _connectionString;
        private readonly ILogger<SqlDeviceHeartbeatRepository> _logger;

        public SqlDeviceHeartbeatRepository(
            IConfiguration configuration,
            ILogger<SqlDeviceHeartbeatRepository> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("No se encontró la cadena de conexión 'DefaultConnection'.");

            _logger = logger;
        }

        public async Task<bool> DeviceExistsAsync(string deviceId, CancellationToken cancellationToken)
        {
            const string sql = @"
SELECT CASE
         WHEN EXISTS
         (
             SELECT 1
             FROM dbo.Device d
             WHERE d.DeviceId = @DeviceId
               AND d.Habilitado = 1
               AND d.FechaBaja IS NULL
         )
         THEN 1
         ELSE 0
       END;";

            await using SqlConnection connection = new(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using SqlCommand command = CreateCommand(connection, sql);
            command.Parameters.AddWithValue("@DeviceId", deviceId);

            object? result = await command.ExecuteScalarAsync(cancellationToken);
            return result != null && Convert.ToInt32(result) == 1;
        }

        public async Task<bool> HasActiveAssignmentAsync(string deviceId, CancellationToken cancellationToken)
        {
            const string sql = @"
SELECT CASE
         WHEN EXISTS
         (
             SELECT 1
             FROM dbo.EmpresaDispositivo ed
             WHERE ed.DeviceId = @DeviceId
               AND ed.Habilitado = 1
               AND ed.FechaDesasignacion IS NULL
         )
         THEN 1
         ELSE 0
       END;";

            await using SqlConnection connection = new(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using SqlCommand command = CreateCommand(connection, sql);
            command.Parameters.AddWithValue("@DeviceId", deviceId);

            object? result = await command.ExecuteScalarAsync(cancellationToken);
            return result != null && Convert.ToInt32(result) == 1;
        }

        public async Task<DateTime?> GetLastHeartbeatReceivedAtUtcAsync(
            string deviceId,
            CancellationToken cancellationToken)
        {
            const string sql = @"
SELECT c.LastHeartbeatReceivedAtUtc
FROM dbo.DeviceHeartbeatCurrent c
WHERE c.DeviceId = @DeviceId;";

            await using SqlConnection connection = new(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using SqlCommand command = CreateCommand(connection, sql);
            command.Parameters.AddWithValue("@DeviceId", deviceId);

            object? result = await command.ExecuteScalarAsync(cancellationToken);

            if (result == null || result == DBNull.Value)
            {
                return null;
            }

            return Convert.ToDateTime(result);
        }

        public async Task UpsertHeartbeatCurrentAsync(
            HeartbeatRequestDTO request,
            DeviceHealthEvaluation evaluation,
            DateTime receivedAtUtc,
            CancellationToken cancellationToken)
        {
            const string sql = @"
MERGE dbo.DeviceHeartbeatCurrent AS target
USING
(
    SELECT
        @DeviceId AS DeviceId,
        @Uptime AS Uptime,
        @Rssi AS Rssi,
        @WsConnected AS WsConnected,
        @EventQueueSize AS EventQueueSize,
        @FreeHeap AS FreeHeap,
        @OperationalStatus AS OperationalStatus,
        @LastHeartbeatReceivedAtUtc AS LastHeartbeatReceivedAtUtc,
        @IssuesJson AS IssuesJson
) AS source
ON target.DeviceId = source.DeviceId
WHEN MATCHED THEN
    UPDATE SET
        target.Uptime = source.Uptime,
        target.Rssi = source.Rssi,
        target.WsConnected = source.WsConnected,
        target.EventQueueSize = source.EventQueueSize,
        target.FreeHeap = source.FreeHeap,
        target.OperationalStatus = source.OperationalStatus,
        target.LastHeartbeatReceivedAtUtc = source.LastHeartbeatReceivedAtUtc,
        target.IssuesJson = source.IssuesJson,
        target.UpdatedAtUtc = SYSUTCDATETIME()
WHEN NOT MATCHED THEN
    INSERT
    (
        DeviceId,
        Uptime,
        Rssi,
        WsConnected,
        EventQueueSize,
        FreeHeap,
        OperationalStatus,
        LastHeartbeatReceivedAtUtc,
        IssuesJson,
        CreatedAtUtc,
        UpdatedAtUtc
    )
    VALUES
    (
        source.DeviceId,
        source.Uptime,
        source.Rssi,
        source.WsConnected,
        source.EventQueueSize,
        source.FreeHeap,
        source.OperationalStatus,
        source.LastHeartbeatReceivedAtUtc,
        source.IssuesJson,
        SYSUTCDATETIME(),
        SYSUTCDATETIME()
    );";

            await using SqlConnection connection = new(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using SqlCommand command = CreateCommand(connection, sql);
            AddHeartbeatParameters(command, request, evaluation, receivedAtUtc);

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task InsertHeartbeatHistoryAsync(
            HeartbeatRequestDTO request,
            DeviceHealthEvaluation evaluation,
            DateTime receivedAtUtc,
            CancellationToken cancellationToken)
        {
            const string sql = @"
INSERT INTO dbo.DeviceHeartbeatHistory
(
    DeviceId,
    Uptime,
    Rssi,
    WsConnected,
    EventQueueSize,
    FreeHeap,
    OperationalStatus,
    ReceivedAtUtc,
    IssuesJson
)
VALUES
(
    @DeviceId,
    @Uptime,
    @Rssi,
    @WsConnected,
    @EventQueueSize,
    @FreeHeap,
    @OperationalStatus,
    @LastHeartbeatReceivedAtUtc,
    @IssuesJson
);";

            await using SqlConnection connection = new(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using SqlCommand command = CreateCommand(connection, sql);
            AddHeartbeatParameters(command, request, evaluation, receivedAtUtc);

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task<DeviceHeartbeatSnapshot?> GetDeviceSnapshotAsync(
            string deviceId,
            CancellationToken cancellationToken)
        {
            const string sql = @"
SELECT TOP 1
    d.DeviceId,
    COALESCE(edActiva.NombreDispositivo, d.Nombre) AS DeviceName,
    th.Nombre AS TipoHardware,
    c.Uptime,
    c.Rssi,
    c.WsConnected,
    c.EventQueueSize,
    c.FreeHeap,
    c.OperationalStatus AS PersistedOperationalStatus,
    c.LastHeartbeatReceivedAtUtc,
    c.IssuesJson,
    e.Id AS EmpresaId,
    e.Nombre AS EmpresaNombre,
    CAST(CASE WHEN edActiva.Id IS NULL THEN 0 ELSE 1 END AS bit) AS HasActiveAssignment
FROM dbo.Device d
LEFT JOIN dbo.TipoHardware th
    ON th.Id = d.TipoHardwareId
LEFT JOIN dbo.DeviceHeartbeatCurrent c
    ON c.DeviceId = d.DeviceId
LEFT JOIN dbo.EmpresaDispositivo edActiva
    ON edActiva.DeviceId = d.DeviceId
   AND edActiva.Habilitado = 1
   AND edActiva.FechaDesasignacion IS NULL
LEFT JOIN dbo.Empresa e
    ON e.Id = edActiva.EmpresaId
WHERE d.DeviceId = @DeviceId
  AND d.Habilitado = 1
  AND d.FechaBaja IS NULL;";

            await using SqlConnection connection = new(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using SqlCommand command = CreateCommand(connection, sql);
            command.Parameters.AddWithValue("@DeviceId", deviceId);

            await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            return MapSnapshot(reader);
        }

        public async Task<IReadOnlyList<DeviceHeartbeatSnapshot>> GetAllCurrentSnapshotsAsync(
            CancellationToken cancellationToken)
        {
            const string sql = @"
SELECT
    d.DeviceId,
    COALESCE(edActiva.NombreDispositivo, d.Nombre) AS DeviceName,
    th.Nombre AS TipoHardware,
    c.Uptime,
    c.Rssi,
    c.WsConnected,
    c.EventQueueSize,
    c.FreeHeap,
    c.OperationalStatus AS PersistedOperationalStatus,
    c.LastHeartbeatReceivedAtUtc,
    c.IssuesJson,
    e.Id AS EmpresaId,
    e.Nombre AS EmpresaNombre,
    CAST(CASE WHEN edActiva.Id IS NULL THEN 0 ELSE 1 END AS bit) AS HasActiveAssignment
FROM dbo.DeviceHeartbeatCurrent c
INNER JOIN dbo.Device d
    ON d.DeviceId = c.DeviceId
LEFT JOIN dbo.TipoHardware th
    ON th.Id = d.TipoHardwareId
LEFT JOIN dbo.EmpresaDispositivo edActiva
    ON edActiva.DeviceId = d.DeviceId
   AND edActiva.Habilitado = 1
   AND edActiva.FechaDesasignacion IS NULL
LEFT JOIN dbo.Empresa e
    ON e.Id = edActiva.EmpresaId
WHERE d.Habilitado = 1
  AND d.FechaBaja IS NULL;";

            List<DeviceHeartbeatSnapshot> results = new();

            await using SqlConnection connection = new(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using SqlCommand command = CreateCommand(connection, sql);
            await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                results.Add(MapSnapshot(reader));
            }

            return results;
        }

        private static SqlCommand CreateCommand(SqlConnection connection, string sql)
        {
            SqlCommand command = connection.CreateCommand();
            command.CommandText = sql;
            command.CommandType = CommandType.Text;
            command.CommandTimeout = 30;
            return command;
        }

        private static void AddHeartbeatParameters(
            SqlCommand command,
            HeartbeatRequestDTO request,
            DeviceHealthEvaluation evaluation,
            DateTime receivedAtUtc)
        {
            command.Parameters.AddWithValue("@DeviceId", request.DeviceId);
            command.Parameters.AddWithValue("@Uptime", request.Uptime);
            command.Parameters.AddWithValue("@Rssi", request.Rssi);
            command.Parameters.AddWithValue("@WsConnected", request.WsConnected);
            command.Parameters.AddWithValue("@EventQueueSize", request.EventQueueSize);
            command.Parameters.AddWithValue("@FreeHeap", request.FreeHeap);
            command.Parameters.AddWithValue("@OperationalStatus", evaluation.OperationalStatus);
            command.Parameters.AddWithValue("@LastHeartbeatReceivedAtUtc", receivedAtUtc);

            string? issuesJson = evaluation.GetIssuesJson();

            SqlParameter issuesParam = command.Parameters.Add("@IssuesJson", SqlDbType.NVarChar, -1);
            issuesParam.Value = string.IsNullOrWhiteSpace(issuesJson)
                ? DBNull.Value
                : issuesJson;
        }

        private static DeviceHeartbeatSnapshot MapSnapshot(SqlDataReader reader)
        {
            return new DeviceHeartbeatSnapshot
            {
                DeviceId = GetString(reader, "DeviceId"),
                DeviceName = GetString(reader, "DeviceName"),
                TipoHardware = GetNullableString(reader, "TipoHardware"),
                Uptime = GetNullableInt64(reader, "Uptime"),
                Rssi = GetNullableInt32(reader, "Rssi"),
                WsConnected = GetNullableBoolean(reader, "WsConnected"),
                EventQueueSize = GetNullableInt32(reader, "EventQueueSize"),
                FreeHeap = GetNullableInt64(reader, "FreeHeap"),
                PersistedOperationalStatus = GetNullableString(reader, "PersistedOperationalStatus"),
                LastHeartbeatReceivedAtUtc = GetNullableDateTime(reader, "LastHeartbeatReceivedAtUtc"),
                IssuesJson = GetNullableString(reader, "IssuesJson"),
                EmpresaId = GetNullableInt32(reader, "EmpresaId"),
                EmpresaNombre = GetNullableString(reader, "EmpresaNombre"),
                HasActiveAssignment = GetBoolean(reader, "HasActiveAssignment")
            };
        }

        private static string GetString(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.GetString(ordinal);
        }

        private static string? GetNullableString(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
        }

        private static int? GetNullableInt32(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);
        }

        private static long? GetNullableInt64(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : reader.GetInt64(ordinal);
        }

        private static bool? GetNullableBoolean(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : reader.GetBoolean(ordinal);
        }

        private static bool GetBoolean(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return !reader.IsDBNull(ordinal) && reader.GetBoolean(ordinal);
        }

        private static DateTime? GetNullableDateTime(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
        }
    }
}