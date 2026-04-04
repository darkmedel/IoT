using cl.MedelCodeFactory.IoT.Common.Contracts.Firmware;

namespace cl.MedelCodeFactory.IoT.Citadel.Endpoints
{
    public static class FirmwareEndpoints
    {
        public static IEndpointRouteBuilder MapFirmwareEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapPost("/firmware", (FirmwareCreateRequest request) =>
            {
                return Results.Ok(new
                {
                    success = true,
                    action = "firmware_created_stub",
                    request.Version,
                    request.Url
                });
            });

            return app;
        }
    }
}