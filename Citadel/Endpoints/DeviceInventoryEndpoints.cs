using cl.MedelCodeFactory.IoT.Common.Contracts.Devices;

namespace cl.MedelCodeFactory.IoT.Citadel.Endpoints
{
    public static class DeviceInventoryEndpoints
    {
        public static IEndpointRouteBuilder MapDeviceInventoryEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapPost("/devices", (DeviceRegistrationRequest request) =>
            {
                return Results.Ok(new
                {
                    success = true,
                    action = "device_registered_stub",
                    request.DeviceId,
                    request.Nombre
                });
            });

            return app;
        }
    }
}