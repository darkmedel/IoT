using cl.MedelCodeFactory.IoT.Bifrost.Contracts;
using cl.MedelCodeFactory.IoT.Bifrost.Domain;

namespace cl.MedelCodeFactory.IoT.Bifrost.Infrastructure.Persistence
{
    public sealed class NullButtonEventRepository : IButtonEventRepository
    {
        public Task<ButtonEventSaveResult> SaveAsync(
            DeviceButtonEvent buttonEvent,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ButtonEventSaveResult.Inserted());
        }
    }
}