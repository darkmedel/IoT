using cl.MedelCodeFactory.IoT.Citadel.Contracts;
using cl.MedelCodeFactory.IoT.Citadel.Infrastructure.Persistence;
using Dapper;

namespace cl.MedelCodeFactory.IoT.Citadel.Repositories;

public sealed class SqlEmpresaRepository : IEmpresaRepository
{
    private readonly CitadelDbConnectionFactory _connectionFactory;

    public SqlEmpresaRepository(CitadelDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<EmpresaResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        const string sql = @"
SELECT Id, Codigo, Nombre, Habilitado
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
SELECT Id, Codigo, Nombre, Habilitado
FROM dbo.Empresa
WHERE Id = @Id;";

        using var connection = _connectionFactory.CreateConnection();
        var command = new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<EmpresaResponse>(command);
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
        var command = new CommandDefinition(sql, new { Codigo = codigo }, cancellationToken: cancellationToken);
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
OUTPUT INSERTED.Id, INSERTED.Codigo, INSERTED.Nombre, INSERTED.Habilitado
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
                request.Codigo,
                request.Nombre,
                Usuario = string.IsNullOrWhiteSpace(request.Usuario) ? "system" : request.Usuario
            },
            cancellationToken: cancellationToken);

        return await connection.QuerySingleAsync<EmpresaResponse>(command);
    }
}
