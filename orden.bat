@echo off
echo ===============================
echo REFACTOR GATEWAY - INICIANDO
echo ===============================

cd GateWay

echo.
echo Creando carpetas (si no existen)...

mkdir Application 2>nul
mkdir Domain 2>nul
mkdir Infrastructure 2>nul

echo.
echo Moviendo archivos...

REM =========================
REM MODELS -> DOMAIN
REM =========================
echo Moviendo ConnectedDevice...
move Models\ConnectedDevice.cs Domain\ConnectedDevice.cs

REM =========================
REM SERVICES -> INFRASTRUCTURE
REM =========================
echo Moviendo ConnectionRegistry...
move Services\ConnectionRegistry.cs Infrastructure\ConnectionRegistry.cs

echo Moviendo MessageDeduplicationService...
move Services\MessageDeduplicationService.cs Infrastructure\MessageDeduplicationService.cs

REM =========================
REM SERVICES -> APPLICATION
REM =========================
echo Moviendo MessageProcessor...
move Services\MessageProcessor.cs Application\MessageProcessor.cs

echo Moviendo DeviceCommandSender...
move Services\DeviceCommandSender.cs Application\CommandService.cs

echo.
echo Limpieza opcional (carpetas vacías)...

rmdir Models 2>nul
rmdir Services 2>nul

echo.
echo ===============================
echo REFACTOR COMPLETADO
echo ===============================
pause