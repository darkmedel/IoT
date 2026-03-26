using cl.MedelCodeFactory.IoT.HeartBeat.Models;

namespace cl.MedelCodeFactory.IoT.HeartBeat.Services
{
    public interface IOperationalStatusEvaluator
    {
        string Evaluate(DeviceHeartbeatSnapshot snapshot, DateTime utcNow);
    }
}