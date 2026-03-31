using cl.MedelCodeFactory.IoT.HeartBeat.Models;

namespace cl.MedelCodeFactory.IoT.HeartBeat.Services
{
    public interface IOperationalStatusEvaluator
    {
        DeviceHealthEvaluation Evaluate(DeviceHeartbeatSnapshot snapshot, DateTime utcNow);
    }
}