using System.Net.Http.Json;
using cl.MedelCodeFactory.IoT.Common.Contracts.Commands;
using cl.MedelCodeFactory.IoT.Common.Contracts.Firmware;

namespace cl.MedelCodeFactory.IoT.Citadel.Services
{
    public class BifrostOtaClient
    {
        private readonly HttpClient _httpClient;

        public BifrostOtaClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<object> ApplyOtaAsync(FirmwareApplyRequest request, CancellationToken cancellationToken = default)
        {
            var payload = new OtaRequest
            {
                DeviceId = request.DeviceId,
                FirmwareVersion = request.FirmwareVersion,
                Url = request.Url
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"/devices/{request.DeviceId}/ota",
                payload,
                cancellationToken);

            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            return new
            {
                success = response.IsSuccessStatusCode,
                statusCode = (int)response.StatusCode,
                response = body
            };
        }
    }
}