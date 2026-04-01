using cl.MedelCodeFactory.IoT.HeartBeat.Models;

namespace cl.MedelCodeFactory.IoT.HeartBeat.Services
{
    public interface IOperationalStatusEvaluator
    {
        DeviceHealthEvaluation Evaluate(HeartbeatEvaluationInput input, DateTime utcNow);
    }
}