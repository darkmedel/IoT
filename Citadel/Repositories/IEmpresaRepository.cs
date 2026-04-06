using cl.MedelCodeFactory.IoT.Citadel.Contracts;

namespace cl.MedelCodeFactory.IoT.Citadel.Repositories;

public interface IEmpresaRepository
{
    Task<IReadOnlyList<EmpresaResponse>> GetAllAsync(CancellationToken cancellationToken);
    Task<EmpresaResponse?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken);
    Task<bool> ExistsByCodigoAsync(string codigo, CancellationToken cancellationToken);
    Task<EmpresaResponse> CreateAsync(CreateEmpresaRequest request, CancellationToken cancellationToken);
    Task<EmpresaResponse> UpdateAsync(int id, CreateEmpresaRequest request, CancellationToken cancellationToken);
}