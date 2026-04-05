using cl.MedelCodeFactory.IoT.Citadel.Contracts;
using cl.MedelCodeFactory.IoT.Citadel.Repositories;

namespace cl.MedelCodeFactory.IoT.Citadel.Endpoints;

public static class EmpresaEndpoints
{
    public static IEndpointRouteBuilder MapEmpresaEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/empresas").WithTags("Empresas");

        group.MapGet("/", async (IEmpresaRepository repository, CancellationToken cancellationToken) =>
        {
            var rows = await repository.GetAllAsync(cancellationToken);
            return Results.Ok(rows);
        });

        group.MapGet("/{id:int}", async (int id, IEmpresaRepository repository, CancellationToken cancellationToken) =>
        {
            var row = await repository.GetByIdAsync(id, cancellationToken);
            return row is null
                ? Results.NotFound(new { message = $"Empresa {id} no existe." })
                : Results.Ok(row);
        });

        group.MapPost("/", async (CreateEmpresaRequest request, IEmpresaRepository repository, CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.Codigo))
            {
                return Results.BadRequest(new { message = "codigo es obligatorio." });
            }

            if (string.IsNullOrWhiteSpace(request.Nombre))
            {
                return Results.BadRequest(new { message = "nombre es obligatorio." });
            }

            var codigo = request.Codigo.Trim().ToUpperInvariant();
            var nombre = request.Nombre.Trim();

            if (await repository.ExistsByCodigoAsync(codigo, cancellationToken))
            {
                return Results.Conflict(new { message = $"Ya existe una empresa con código {codigo}." });
            }

            var created = await repository.CreateAsync(request with { Codigo = codigo, Nombre = nombre }, cancellationToken);
            return Results.Created($"/api/empresas/{created.Id}", created);
        });

        return app;
    }
}
