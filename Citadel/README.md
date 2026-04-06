# Citadel.Web Pro

UI oscura con Razor Pages + Bootstrap + SweetAlert2 para operar Citadel.

## Incluye
- Tabla principal de dispositivos con filtro por texto
- Modal crear/editar dispositivo
- Modal crear/editar empresa
- Modal asignar dispositivo
- Confirmación de desasignación
- Toast Bootstrap
- Proxy server-side para evitar CORS

## Puertos por defecto
- Citadel API: http://localhost:5010
- Citadel.Web: http://localhost:5020

## Pasos
1. Copia esta carpeta dentro de tu solución como proyecto `Citadel.Web`
2. Agrega el proyecto a la solución
3. Ejecuta `dotnet restore`
4. Ejecuta `dotnet run`
5. Abre `http://localhost:5020`

## Nota importante sobre edición
La UI ya está preparada para editar empresas y dispositivos, pero si tu API todavía no expone:
- PUT /api/empresas/{id}
- PUT /api/devices/{deviceId}

la UI mostrará una advertencia indicando que el endpoint aún no existe.
