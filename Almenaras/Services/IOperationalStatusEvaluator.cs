using Almenaras.Models;

namespace Almenaras.Services
{
    public interface IOperationalStatusEvaluator
    {
        DeviceHealthEvaluation Evaluate(HeartbeatEvaluationInput input, DateTime utcNow);
    }
}