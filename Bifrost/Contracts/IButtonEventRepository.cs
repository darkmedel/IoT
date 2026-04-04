using Bifrost.Domain;

namespace Bifrost.Contracts
{
    public interface IButtonEventRepository
    {
        Task<ButtonEventSaveResult> SaveAsync(
            DeviceButtonEvent buttonEvent,
            CancellationToken cancellationToken = default);
    }
}