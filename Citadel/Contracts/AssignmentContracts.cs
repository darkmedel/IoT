namespace cl.MedelCodeFactory.IoT.Citadel.Contracts;

public sealed record CreateAssignmentRequest(int EmpresaId, string DeviceId, string? NombreDispositivo = null, string? Descripcion = null, string? Usuario = null);
public sealed record DeviceEmpresaResponse(string DeviceId, int EmpresaId, string EmpresaNombre, DateTime FechaRegistroUtc);
public sealed record UnassignDeviceResponse(string DeviceId, bool Unassigned, DateTime? FechaDesasignacionUtc, string Message);
