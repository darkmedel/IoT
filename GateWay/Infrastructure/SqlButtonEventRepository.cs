using cl.MedelCodeFactory.IoT.GateWay.Contracts;
using cl.MedelCodeFactory.IoT.GateWay.Domain;
using Microsoft.Data.SqlClient;
using System.Data;

namespace cl.MedelCodeFactory.IoT.GateWay.Infrastructure
{
    public class SqlButtonEventRepository : IButtonEventRepository
    {
        private const int DuplicateKeyErrorNumber = 2627;
        private const int DuplicateIndexErrorNumber = 2601;

        private readonly ILogger<SqlButtonEventRepository> _logger;
        private readonly string _connectionString;

        public SqlButtonEventRepository(
            ILogger<SqlButtonEventRepository> logger,
            IConfiguration configuration)
        {
            _logger = logger;
            _connectionString = configuration.GetConnectionString("IoTOperacion")
                ?? throw new InvalidOperationException(
                    "Connection string 'IoTOperacion' was not found.");
        }

        public async Task<ButtonEventSaveResult> SaveAsync(
            DeviceButtonEvent buttonEvent,
            CancellationToken cancellationToken = default)
        {
            const string sql = @"
INSERT INTO dbo.DeviceButtonEvent
(
    DeviceId,
    MsgId,
    Uptime,
    ButtonNumber,
    ReceivedAtUtc,
    RawMessage,
    ConnectionId,
    RemoteIp,
    CreatedAtUtc
)
VALUES
(
    @DeviceId,
    @MsgId,
    @Uptime,
    @ButtonNumber,
    @ReceivedAtUtc,
    @RawMessage,
    @ConnectionId,
    @RemoteIp,
    SYSUTCDATETIME()
);";

            try
            {
                await using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);

                await using var command = new SqlCommand(sql, connection)
                {
                    CommandType = CommandType.Text
                };

                command.Parameters.Add("@DeviceId", SqlDbType.VarChar, 12).Value = buttonEvent.DeviceId;
                command.Parameters.Add("@MsgId", SqlDbType.BigInt).Value = buttonEvent.MsgId;
                command.Parameters.Add("@Uptime", SqlDbType.BigInt).Value = buttonEvent.Uptime;
                command.Parameters.Add("@ButtonNumber", SqlDbType.Int).Value = buttonEvent.ButtonNumber;
                command.Parameters.Add("@ReceivedAtUtc", SqlDbType.DateTime2).Value = buttonEvent.ReceivedAtUtc;

                command.Parameters.Add("@RawMessage", SqlDbType.NVarChar, 500).Value =
                    (object?)Truncate(buttonEvent.RawMessage, 500) ?? DBNull.Value;

                command.Parameters.Add("@ConnectionId", SqlDbType.VarChar, 100).Value =
                    (object?)Truncate(buttonEvent.ConnectionId, 100) ?? DBNull.Value;

                command.Parameters.Add("@RemoteIp", SqlDbType.VarChar, 50).Value =
                    (object?)Truncate(buttonEvent.RemoteIp, 50) ?? DBNull.Value;

                await command.ExecuteNonQueryAsync(cancellationToken);

                return ButtonEventSaveResult.Inserted();
            }
            catch (SqlException ex) when (ex.Number == DuplicateKeyErrorNumber || ex.Number == DuplicateIndexErrorNumber)
            {
                _logger.LogWarning(
                    ex,
                    "[BTN-REPO] Duplicate event ignored by SQL unique index | DeviceId={deviceId} | MsgId={msgId}",
                    buttonEvent.DeviceId,
                    buttonEvent.MsgId);

                return ButtonEventSaveResult.Duplicate();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "[BTN-REPO] Persistence error | DeviceId={deviceId} | MsgId={msgId}",
                    buttonEvent.DeviceId,
                    buttonEvent.MsgId);

                return ButtonEventSaveResult.Failed();
            }
        }

        private static string? Truncate(string? value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            return value.Length <= maxLength
                ? value
                : value[..maxLength];
        }
    }
}