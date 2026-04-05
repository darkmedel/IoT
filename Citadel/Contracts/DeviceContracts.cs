namespace cl.MedelCodeFactory.IoT.Citadel.Contracts;

public sealed record CreateDeviceRequest(
    string DeviceId,
    int TipoHardwareId,
    string Nombre,
    string? Descripcion = null,
    string? SerialNumber = null,
    string? FirmwareVersion = null,
    string? MacAddress = null,
    string? Usuario = null);

public sealed record DeviceResponse(
    string DeviceId,
    int TipoHardwareId,
    string Nombre,
    string? Descripcion,
    string? SerialNumber,
    string? FirmwareVersion,
    string? MacAddress,
    bool Habilitado,
    DateTime FechaRegistroInventario);
