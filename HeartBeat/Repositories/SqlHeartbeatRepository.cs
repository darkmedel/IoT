using System.Data;
using Microsoft.Data.SqlClient;
using cl.MedelCodeFactory.IoT.HeartBeat.DTOs;
using cl.MedelCodeFactory.IoT.HeartBeat.Models;

namespace cl.MedelCodeFactory.IoT.HeartBeat.Repositories
{
    public sealed class SqlHeartbeatRepository : IHeartbeatRepository
    {
        private readonly string _connectionString;

        public SqlHeartbeatRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string not found.");
        }

        public async Task<DateTime?> GetLastHeartbeatReceivedAtUtcAsync(
            string deviceId,
            CancellationToken cancellationToken)
        {
            const string sql = @"
SELECT LastHeartbeatReceivedAtUtc
FROM dbo.DeviceHeartbeatCurrent
WHERE DeviceId = @DeviceId;";

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@DeviceId", deviceId);

            var result = await command.ExecuteScalarAsync(cancellationToken);

            if (result == null || result == DBNull.Value)
                return null;

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
USING (SELECT @DeviceId AS DeviceId) AS source
ON target.DeviceId = source.DeviceId

WHEN MATCHED THEN
    UPDATE SET
        Uptime = @Uptime,
        Rssi = @Rssi,
        WsConnected = @WsConnected,
        EventQueueSize = @EventQueueSize,
        FreeHeap = @FreeHeap,
        OperationalStatus = @OperationalStatus,
        LastHeartbeatReceivedAtUtc = @ReceivedAtUtc,
        IssuesJson = @IssuesJson,
        UpdatedAtUtc = SYSUTCDATETIME()

WHEN NOT MATCHED THEN
    INSERT (
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
    VALUES (
        @DeviceId,
        @Uptime,
        @Rssi,
        @WsConnected,
        @EventQueueSize,
        @FreeHeap,
        @OperationalStatus,
        @ReceivedAtUtc,
        @IssuesJson,
        SYSUTCDATETIME(),
        SYSUTCDATETIME()
    );";

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand(sql, connection);

            AddParameters(command, request, evaluation, receivedAtUtc);

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
    @ReceivedAtUtc,
    @IssuesJson
);";

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand(sql, connection);

            AddParameters(command, request, evaluation, receivedAtUtc);

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private static void AddParameters(
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
            command.Parameters.AddWithValue("@ReceivedAtUtc", receivedAtUtc);

            var issues = evaluation.GetIssuesJson();

            var param = command.Parameters.Add("@IssuesJson", SqlDbType.NVarChar, -1);
            param.Value = string.IsNullOrWhiteSpace(issues) ? DBNull.Value : issues;
        }
    }
}