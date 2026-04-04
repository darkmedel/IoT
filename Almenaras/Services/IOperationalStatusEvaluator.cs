using cl.MedelCodeFactory.IoT.Almenaras.Models;

namespace cl.MedelCodeFactory.IoT.Almenaras.Services
{
    public interface IOperationalStatusEvaluator
    {
        DeviceHealthEvaluation Evaluate(HeartbeatEvaluationInput input, DateTime utcNow);
    }
}