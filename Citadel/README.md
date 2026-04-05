# IoT.Citadel v1.0 – implementación base

Esta propuesta deja lista la base de **inventario** y **asignación** para `IoT.Citadel` sobre `IoT_Common`.

## Incluye

- `Program.cs` con Swagger, health y mapeo de endpoints.
- Endpoints para:
  - `GET /api/empresas`
  - `POST /api/empresas`
  - `GET /api/empresas/{id}`
  - `GET /api/devices`
  - `POST /api/devices`
  - `GET /api/devices/{deviceId}`
  - `POST /api/asignaciones`
  - `POST /api/asignaciones/{deviceId}/desasignar`
  - `GET /api/devices/{deviceId}/empresa`
- Repositorios SQL con Dapper.
- Factory de conexión para `IoT_Common`.
- Contratos request/response.

## Paquetes NuGet necesarios

Si todavía no están en el proyecto:

```powershell
dotnet add package Dapper
dotnet add package Microsoft.Data.SqlClient
dotnet add package Swashbuckle.AspNetCore
```

## Connection string esperada

`appsettings.json`

```json
{
  "ConnectionStrings": {
    "IoTCommon": "Server=...;Database=IoT_Common;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

También soporta `DefaultConnection` como fallback.

## Decisiones aplicadas

- `DeviceId` se normaliza a mayúsculas.
- Se valida `TipoHardwareId` antes de crear dispositivo.
- Se valida empresa existente, dispositivo existente y unicidad de asignación activa antes de asignar.
- La desasignación actualiza `FechaDesasignacion`, `Habilitado`, `UsuarioModificacion` y `FechaModificacion`.
- Se respeta la regla de una sola asignación activa por `DeviceId`.

## Ajuste recomendado a nivel SQL

En `IoT_Common`, el índice filtrado actual permite una sola fila con `FechaDesasignacion IS NULL`, aunque `Habilitado = 0`.

Hoy el endpoint de desasignación hace ambas cosas:
- `FechaDesasignacion = SYSUTCDATETIME()`
- `Habilitado = 0`

Eso está bien y evita conflicto.

Pero para dejar la regla más alineada al negocio, recomiendo más adelante reemplazar el índice filtrado por uno así:

```sql
DROP INDEX UX_EmpresaDispositivo_DeviceId_Activo ON dbo.EmpresaDispositivo;
GO

CREATE UNIQUE NONCLUSTERED INDEX UX_EmpresaDispositivo_DeviceId_Activo
ON dbo.EmpresaDispositivo(DeviceId)
WHERE Habilitado = 1 AND FechaDesasignacion IS NULL;
GO
```

## Respuesta de negocio ya cubierta

Esto deja resuelto el núcleo de la fase:
- inventario centralizado
- empresas registradas
- asignación única activa por dispositivo
- consulta de empresa actual por device
- base lista para que `HeartBeat` y `WatchTower` consuman información de tenancy
