using cl.MedelCodeFactory.IoT.Common.Contracts.Devices;

namespace cl.MedelCodeFactory.IoT.Citadel.Endpoints
{
    public static class EmpresaEndpoints
    {
        public static IEndpointRouteBuilder MapEmpresaEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapPost("/empresas", (EmpresaRequest request) =>
            {
                return Results.Ok(new
                {
                    success = true,
                    action = "empresa_created_stub",
                    request.Codigo,
                    request.Nombre
                });
            });

            return app;
        }
    }
}