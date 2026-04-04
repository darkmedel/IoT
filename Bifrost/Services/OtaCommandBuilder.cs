using cl.MedelCodeFactory.IoT.Common.Contracts.Commands;

namespace cl.MedelCodeFactory.IoT.Bifrost.Services
{
    public class OtaCommandBuilder
    {
        public string Build(OtaRequest request)
        {
            return string.Format(
                "CMD|OTA|{0}|{1}",
                request.FirmwareVersion,
                request.Url);
        }
    }
}