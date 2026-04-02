using cl.MedelCodeFactory.IoT.GateWay.Domain;

namespace cl.MedelCodeFactory.IoT.GateWay.Contracts
{
    public interface IButtonEventRepository
    {
        Task<ButtonEventSaveResult> SaveAsync(
            DeviceButtonEvent buttonEvent,
            CancellationToken cancellationToken = default);
    }
}