namespace cl.MedelCodeFactory.IoT.Citadel.Contracts;

public sealed record CreateEmpresaRequest(string Codigo, string Nombre, string? Usuario = null);
public sealed record EmpresaResponse(int Id, string Codigo, string Nombre, bool Habilitado);
