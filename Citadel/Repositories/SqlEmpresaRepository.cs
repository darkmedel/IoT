using cl.MedelCodeFactory.IoT.Citadel.Contracts;
using cl.MedelCodeFactory.IoT.Citadel.Infrastructure.Persistence;
using cl.MedelCodeFactory.IoT.Citadel.Services;
using Dapper;

namespace cl.MedelCodeFactory.IoT.Citadel.Repositories;

public sealed class SqlEmpresaRepository : IEmpresaRepository
{
    private readonly CitadelDbConnectionFactory _connectionFactory;
    private readonly ICurrentUserService _currentUserService;

    public SqlEmpresaRepository(
        CitadelDbConnectionFactory connectionFactory,
        ICurrentUserService currentUserService)
    {
        _connectionFactory = connectionFactory;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<EmpresaResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        const string sql = @"
SELECT
    Id,
    Codigo,
    Nombre,
    Habilitado
FROM dbo.Empresa
ORDER BY Nombre, Id;";

        using var connection = _connectionFactory.CreateConnection();
        var command = new CommandDefinition(sql, cancellationToken: cancellationToken);
        var rows = await connection.QueryAsync<EmpresaResponse>(command);
        return rows.AsList();
    }

    public async Task<EmpresaResponse?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        const string sql = @"
SELECT
    Id,
    Codigo,
    Nombre,
    Habilitado
FROM dbo.Empresa
WHERE Id = @Id;";

        using var connection = _connectionFactory.CreateConnection();
        var command = new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<EmpresaResponse>(command);
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken)
    {
        const string sql = @"
SELECT CASE WHEN EXISTS(
    SELECT 1
    FROM dbo.Empresa
    WHERE Id = @Id
      AND Habilitado = 1
) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END;";

        using var connection = _connectionFactory.CreateConnection();
        var command = new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken);
        return await connection.ExecuteScalarAsync<bool>(command);
    }

    public async Task<bool> ExistsByCodigoAsync(string codigo, CancellationToken cancellationToken)
    {
        const string sql = @"
SELECT CASE WHEN EXISTS(
    SELECT 1
    FROM dbo.Empresa
    WHERE Codigo = @Codigo
) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END;";

        using var connection = _connectionFactory.CreateConnection();
        var command = new CommandDefinition(
            sql,
            new { Codigo = NormalizeCodigo(codigo) },
            cancellationToken: cancellationToken);

        return await connection.ExecuteScalarAsync<bool>(command);
    }

    public async Task<EmpresaResponse> CreateAsync(CreateEmpresaRequest request, CancellationToken cancellationToken)
    {
        const string sql = @"
INSERT INTO dbo.Empresa
(
    Codigo,
    Nombre,
    Habilitado,
    UsuarioCreacion,
    FechaCreacion,
    UsuarioModificacion,
    FechaModificacion
)
OUTPUT
    INSERTED.Id,
    INSERTED.Codigo,
    INSERTED.Nombre,
    INSERTED.Habilitado
VALUES
(
    @Codigo,
    @Nombre,
    1,
    @Usuario,
    SYSUTCDATETIME(),
    @Usuario,
    SYSUTCDATETIME()
);";

        using var connection = _connectionFactory.CreateConnection();

        var command = new CommandDefinition(
            sql,
            new
            {
                Codigo = NormalizeCodigo(request.Codigo),
                Nombre = NormalizeNombre(request.Nombre),
                Usuario = ResolveUsuario(request.Usuario)
            },
            cancellationToken: cancellationToken);

        return await connection.QuerySingleAsync<EmpresaResponse>(command);
    }

    private string ResolveUsuario(string? requestUsuario)
    {
        if (!string.IsNullOrWhiteSpace(requestUsuario))
        {
            return requestUsuario.Trim();
        }

        return _currentUserService.GetCurrentUser();
    }

    private static string NormalizeCodigo(string? value)
    {
        return (value ?? string.Empty).Trim().ToUpperInvariant();
    }

    private static string NormalizeNombre(string? value)
    {
        return (value ?? string.Empty).Trim();
    }
}
