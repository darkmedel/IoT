using System.Data;
using Microsoft.Data.SqlClient;
using cl.MedelCodeFactory.IoT.HeartBeat.DTOs;
using cl.MedelCodeFactory.IoT.HeartBeat.Models;
using cl.MedelCodeFactory.IoT.HeartBeat.Options;
using Microsoft.Extensions.Options;

namespace cl.MedelCodeFactory.IoT.HeartBeat.Repositories
{
    public class SqlHeartbeatRepository : IHeartbeatRepository
    {
        private readonly string _connectionString;

        public SqlHeartbeatRepository(IOptions<DatabaseOptions> options)
        {
            _connectionString = options.Value.ConnectionString;
        }

        public async Task<DeviceInventoryRecord?> GetDeviceAsync(string deviceId, CancellationToken cancellationToken)
        {
            const string sql = @"
SELECT
    DeviceId,
    Habilitado,
    FechaBaja
FROM dbo.Device
WHERE DeviceId = @DeviceId;";

            using SqlConnection connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            using SqlCommand command = new SqlCommand(sql, connection);
            command.Parameters.Add("@DeviceId", SqlDbType.VarChar, 100).Value = deviceId;

            using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            return new DeviceInventoryRecord
            {
                DeviceId = reader["DeviceId"].ToString() ?? string.Empty,
                Habilitado = Convert.ToBoolean(reader["Habilitado"]),
                FechaBaja = reader["FechaBaja"] == DBNull.Value
                    ? null
                    : Convert.ToDateTime(reader["FechaBaja"])
            };
        }

        public async Task SaveHeartbeatAsync(
            HeartbeatRequest request,
            string operationalStatus,
            string? issuesJson,
            DateTime receivedAtUtc,
            CancellationToken cancellationToken)
        {
            const string sql = @"
BEGIN TRANSACTION;

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
        @IssuesJson AS IssuesJson,
        @NowUtc AS NowUtc
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
        target.UpdatedAtUtc = source.NowUtc

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
        source.NowUtc,
        source.NowUtc
    );

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
);

COMMIT TRANSACTION;";

            using SqlConnection connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            using SqlCommand command = new SqlCommand(sql, connection);

            command.Parameters.Add("@DeviceId", SqlDbType.VarChar, 100).Value = request.DeviceId;
            command.Parameters.Add("@Uptime", SqlDbType.BigInt).Value = request.Uptime;
            command.Parameters.Add("@Rssi", SqlDbType.Int).Value = request.Rssi;
            command.Parameters.Add("@WsConnected", SqlDbType.Bit).Value = request.WsConnected;
            command.Parameters.Add("@EventQueueSize", SqlDbType.Int).Value = request.EventQueueSize;
            command.Parameters.Add("@FreeHeap", SqlDbType.BigInt).Value = request.FreeHeap;
            command.Parameters.Add("@OperationalStatus", SqlDbType.VarChar, 20).Value = operationalStatus;
            command.Parameters.Add("@LastHeartbeatReceivedAtUtc", SqlDbType.DateTime2).Value = receivedAtUtc;
            command.Parameters.Add("@NowUtc", SqlDbType.DateTime2).Value = DateTime.UtcNow;
            command.Parameters.Add("@IssuesJson", SqlDbType.NVarChar).Value =
                string.IsNullOrWhiteSpace(issuesJson) ? DBNull.Value : issuesJson;

            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}