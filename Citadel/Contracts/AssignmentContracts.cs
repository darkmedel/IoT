namespace cl.MedelCodeFactory.IoT.Citadel.Contracts;

public sealed record CreateAssignmentRequest(
    int EmpresaId,
    string DeviceId,
    string NombreDispositivo,
    string Descripcion,
    string? Usuario = null);

public sealed record DeviceEmpresaResponse(
    string DeviceId,
    int EmpresaId,
    string EmpresaNombre,
    string NombreDispositivo,
    string Descripcion,
    DateTime FechaRegistroUtc);

public sealed record UnassignDeviceResponse(
    string DeviceId,
    bool Unassigned,
    DateTime? FechaDesasignacionUtc,
    string Message);
