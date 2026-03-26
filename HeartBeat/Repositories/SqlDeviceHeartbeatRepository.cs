using System.Data;
using cl.MedelCodeFactory.IoT.HeartBeat.DTOs;
using cl.MedelCodeFactory.IoT.HeartBeat.Models;
using Microsoft.Data.SqlClient;

namespace cl.MedelCodeFactory.IoT.HeartBeat.Repositories
{
    public sealed class SqlHeartbeatRepository : IHeartbeatRepository
    {
        private const int DeviceIdLength = 12;
        private const int OperationalStatusLength = 20;

        private readonly string _connectionString;

        public SqlHeartbeatRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("No se encontró la cadena de conexión 'DefaultConnection'.");
        }

        public async Task<bool> DeviceExistsAsync(string deviceId, CancellationToken cancellationToken)
        {
            const string sql = @"
SELECT COUNT(1)
FROM dbo.Device
WHERE DeviceId = @DeviceId
  AND Habilitado = 1;";

            await using SqlConnection connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using SqlCommand command = new SqlCommand(sql, connection);
            AddDeviceIdParameter(command, deviceId);

            object result = await command.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt32(result) > 0;
        }

        public async Task<bool> HasActiveAssignmentAsync(string deviceId, CancellationToken cancellationToken)
        {
            const string sql = @"
SELECT COUNT(1)
FROM dbo.EmpresaDispositivo ed
WHERE ed.DeviceId = @DeviceId
  AND ed.Habilitado = 1
  AND ed.FechaDesasignacion IS NULL;";

            await using SqlConnection connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using SqlCommand command = new SqlCommand(sql, connection);
            AddDeviceIdParameter(command, deviceId);

            object result = await command.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt32(result) > 0;
        }

        public async Task UpsertHeartbeatCurrentAsync(
            HeartbeatRequestDTO request,
            string operationalStatus,
            DateTime receivedAtUtc,
            CancellationToken cancellationToken)
        {
            const string sql = @"
UPDATE dbo.DeviceHeartbeatCurrent
SET
    Uptime = @Uptime,
    Rssi = @Rssi,
    WsConnected = @WsConnected,
    EventQueueSize = @EventQueueSize,
    FreeHeap = @FreeHeap,
    OperationalStatus = @OperationalStatus,
    LastHeartbeatReceivedAtUtc = @LastHeartbeatReceivedAtUtc,
    UpdatedAtUtc = SYSUTCDATETIME()
WHERE DeviceId = @DeviceId;

IF @@ROWCOUNT = 0
BEGIN
    INSERT INTO dbo.DeviceHeartbeatCurrent
    (
        DeviceId,
        Uptime,
        Rssi,
        WsConnected,
        EventQueueSize,
        FreeHeap,
        OperationalStatus,
        LastHeartbeatReceivedAtUtc,
        CreatedAtUtc,
        UpdatedAtUtc
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
        SYSUTCDATETIME(),
        SYSUTCDATETIME()
    );
END;";

            await using SqlConnection connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using SqlCommand command = new SqlCommand(sql, connection);
            AddHeartbeatParameters(
                command,
                request,
                operationalStatus,
                receivedAtUtc,
                "@LastHeartbeatReceivedAtUtc");

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task InsertHeartbeatHistoryAsync(
            HeartbeatRequestDTO request,
            string operationalStatus,
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
    ReceivedAtUtc
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
    @ReceivedAtUtc
);";

            await using SqlConnection connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using SqlCommand command = new SqlCommand(sql, connection);
            AddHeartbeatParameters(
                command,
                request,
                operationalStatus,
                receivedAtUtc,
                "@ReceivedAtUtc");

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task<DeviceHeartbeatSnapshot?> GetDeviceSnapshotAsync(
            string deviceId,
            CancellationToken cancellationToken)
        {
            const string sql = @"
SELECT TOP 1
    d.DeviceId,
    d.Nombre AS DeviceName,
    th.Nombre AS TipoHardware,
    c.Uptime,
    c.Rssi,
    c.WsConnected,
    c.EventQueueSize,
    c.FreeHeap,
    c.OperationalStatus,
    c.LastHeartbeatReceivedAtUtc,
    c.IssuesJson,
    e.Id AS EmpresaId,
    e.Nombre AS EmpresaNombre,
    CASE
        WHEN ed.DeviceId IS NULL THEN CAST(0 AS bit)
        ELSE CAST(1 AS bit)
    END AS HasActiveAssignment
FROM dbo.Device d
LEFT JOIN dbo.TipoHardware th
    ON th.Id = d.TipoHardwareId
LEFT JOIN dbo.DeviceHeartbeatCurrent c
    ON c.DeviceId = d.DeviceId
LEFT JOIN dbo.EmpresaDispositivo ed
    ON ed.DeviceId = d.DeviceId
   AND ed.Habilitado = 1
   AND ed.FechaDesasignacion IS NULL
LEFT JOIN dbo.Empresa e
    ON e.Id = ed.EmpresaId
WHERE d.DeviceId = @DeviceId
  AND d.Habilitado = 1;";

            await using SqlConnection connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using SqlCommand command = new SqlCommand(sql, connection);
            AddDeviceIdParameter(command, deviceId);

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
    d.Nombre AS DeviceName,
    th.Nombre AS TipoHardware,
    c.Uptime,
    c.Rssi,
    c.WsConnected,
    c.EventQueueSize,
    c.FreeHeap,
    c.OperationalStatus,
    c.LastHeartbeatReceivedAtUtc,
    c.IssuesJson,
    e.Id AS EmpresaId,
    e.Nombre AS EmpresaNombre,
    CASE
        WHEN ed.DeviceId IS NULL THEN CAST(0 AS bit)
        ELSE CAST(1 AS bit)
    END AS HasActiveAssignment
FROM dbo.Device d
LEFT JOIN dbo.TipoHardware th
    ON th.Id = d.TipoHardwareId
LEFT JOIN dbo.DeviceHeartbeatCurrent c
    ON c.DeviceId = d.DeviceId
LEFT JOIN dbo.EmpresaDispositivo ed
    ON ed.DeviceId = d.DeviceId
   AND ed.Habilitado = 1
   AND ed.FechaDesasignacion IS NULL
LEFT JOIN dbo.Empresa e
    ON e.Id = ed.EmpresaId
WHERE d.Habilitado = 1;";

            List<DeviceHeartbeatSnapshot> result = new List<DeviceHeartbeatSnapshot>();

            await using SqlConnection connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using SqlCommand command = new SqlCommand(sql, connection);
            await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                result.Add(MapSnapshot(reader));
            }

            return result;
        }

        private static void AddDeviceIdParameter(SqlCommand command, string deviceId)
        {
            command.Parameters.Add("@DeviceId", SqlDbType.VarChar, DeviceIdLength).Value = deviceId;
        }

        private static void AddHeartbeatParameters(
            SqlCommand command,
            HeartbeatRequestDTO request,
            string operationalStatus,
            DateTime receivedAtUtc,
            string receivedAtParameterName)
        {
            command.Parameters.Add("@DeviceId", SqlDbType.VarChar, DeviceIdLength).Value = request.DeviceId;
            command.Parameters.Add("@Uptime", SqlDbType.BigInt).Value = request.Uptime;
            command.Parameters.Add("@Rssi", SqlDbType.Int).Value = request.Rssi;
            command.Parameters.Add("@WsConnected", SqlDbType.Bit).Value = request.WsConnected;
            command.Parameters.Add("@EventQueueSize", SqlDbType.Int).Value = request.EventQueueSize;
            command.Parameters.Add("@FreeHeap", SqlDbType.BigInt).Value = request.FreeHeap;
            command.Parameters.Add("@OperationalStatus", SqlDbType.VarChar, OperationalStatusLength).Value = operationalStatus;
            command.Parameters.Add(receivedAtParameterName, SqlDbType.DateTime2).Value = receivedAtUtc;
        }

        private static DeviceHeartbeatSnapshot MapSnapshot(SqlDataReader reader)
        {
            return new DeviceHeartbeatSnapshot
            {
                DeviceId = reader["DeviceId"] as string ?? string.Empty,
                DeviceName = reader["DeviceName"] as string ?? string.Empty,
                TipoHardware = reader["TipoHardware"] == DBNull.Value ? null : reader["TipoHardware"].ToString(),

                Uptime = reader["Uptime"] == DBNull.Value ? null : Convert.ToInt64(reader["Uptime"]),
                Rssi = reader["Rssi"] == DBNull.Value ? null : Convert.ToInt32(reader["Rssi"]),
                WsConnected = reader["WsConnected"] == DBNull.Value ? null : Convert.ToBoolean(reader["WsConnected"]),
                EventQueueSize = reader["EventQueueSize"] == DBNull.Value ? null : Convert.ToInt32(reader["EventQueueSize"]),
                FreeHeap = reader["FreeHeap"] == DBNull.Value ? null : Convert.ToInt64(reader["FreeHeap"]),

                PersistedOperationalStatus = reader["OperationalStatus"] == DBNull.Value
                    ? null
                    : reader["OperationalStatus"].ToString(),

                LastHeartbeatReceivedAtUtc = reader["LastHeartbeatReceivedAtUtc"] == DBNull.Value
                    ? null
                    : Convert.ToDateTime(reader["LastHeartbeatReceivedAtUtc"]),

                IssuesJson = reader["IssuesJson"] == DBNull.Value
                    ? null
                    : reader["IssuesJson"].ToString(),

                EmpresaId = reader["EmpresaId"] == DBNull.Value
                    ? null
                    : Convert.ToInt32(reader["EmpresaId"]),

                EmpresaNombre = reader["EmpresaNombre"] == DBNull.Value
                    ? null
                    : reader["EmpresaNombre"].ToString(),

                HasActiveAssignment = reader["HasActiveAssignment"] != DBNull.Value
                    && Convert.ToBoolean(reader["HasActiveAssignment"])
            };
        }
    }
}