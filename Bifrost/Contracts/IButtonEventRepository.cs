using cl.MedelCodeFactory.IoT.Bifrost.Domain;

namespace cl.MedelCodeFactory.IoT.Bifrost.Contracts
{
    public interface IButtonEventRepository
    {
        Task<ButtonEventSaveResult> SaveAsync(
            DeviceButtonEvent buttonEvent,
            CancellationToken cancellationToken = default);
    }
}