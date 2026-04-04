using cl.MedelCodeFactory.IoT.Common.Contracts.Commands;

namespace cl.MedelCodeFactory.IoT.GateWay.Application
{
    public class OtaCommandBuilder
    {
        public string Build(OtaRequest request)
        {
            return $"CMD|OTA|{request.FirmwareVersion}|{request.Url}";
        }
    }
}