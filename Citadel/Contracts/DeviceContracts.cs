namespace cl.MedelCodeFactory.IoT.Citadel.Contracts;

public sealed record CreateDeviceRequest(
    string DeviceId,
    int TipoHardwareId,
    int? FirmwareId = null,
    string? FirmwareVersion = null,
    string? Usuario = null);

public sealed record DeviceResponse(
    string DeviceId,
    int TipoHardwareId,
    int? FirmwareId,
    string FirmwareVersion,
    bool Habilitado,
    DateTime FechaRegistroInventario);
