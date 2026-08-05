@echo off
setlocal EnableExtensions
REM Publica configurador + consola. Ejecutar desde Scripts\ o doble clic aqui.

set "REPO=%~dp0.."
set "PUBLISH_NOPAUSE=1"

echo.
echo ========================================
echo  Publish completo SigmabotSync
echo ========================================
echo.

call "%~dp0Publish-Configurador.bat"
if errorlevel 1 goto :fail

call "%~dp0Publish-Console.bat"
if errorlevel 1 goto :fail

echo.
echo === Todo listo ===
echo   %REPO%\publish\configurador\
echo   %REPO%\publish\console\
echo.
if not defined PUBLISH_NOPAUSE pause
exit /b 0

:fail
echo.
echo ERROR: publish incompleto.
pause
exit /b 1
