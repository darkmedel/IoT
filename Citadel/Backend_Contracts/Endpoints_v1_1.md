# Citadel v1.1 - Contratos recomendados

## PUT /api/empresas/{id}
Actualiza empresa existente.

### Request
```json
{
  "codigo": "EMP001",
  "nombre": "MedelCodeFactory Demo",
  "habilitado": true
}
```

### Response
```json
{
  "id": 1,
  "codigo": "EMP001",
  "nombre": "MedelCodeFactory Demo",
  "habilitado": true
}
```

---

## PUT /api/devices/{deviceId}
Actualiza device existente.

### Request
```json
{
  "deviceId": "808A26A50528",
  "tipoHardwareId": 1,
  "firmwareId": 1,
  "firmwareVersion": "V01.00.000",
  "habilitado": true
}
```

### Response
```json
{
  "deviceId": "808A26A50528",
  "tipoHardwareId": 1,
  "firmwareId": 1,
  "firmwareVersion": "V01.00.000",
  "habilitado": true
}
```

---

## GET /api/devices/{deviceId}/asignaciones
Devuelve historial de asignaciones.

### Response
```json
[
  {
    "empresaId": 1,
    "empresaNombre": "MedelCodeFactory Demo",
    "nombreDispositivo": "Botonera Planta 1",
    "descripcion": "Equipo de prueba",
    "habilitado": false,
    "fechaRegistroUtc": "2026-04-05T19:32:50.4255465",
    "fechaDesasignacionUtc": "2026-04-05T19:35:59.3405649"
  }
]
```

---

## GET /api/tipohardware
### Response
```json
[
  {
    "id": 1,
    "codigo": "ESP32-HW.01",
    "nombre": "ESP32 HW.01",
    "habilitado": true
  }
]
```

---

## GET /api/firmwares?tipoHardwareId=1
### Response
```json
[
  {
    "id": 1,
    "tipoHardwareId": 1,
    "version": "V01.00.000",
    "habilitado": true
  }
]
```
