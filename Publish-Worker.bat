@echo off
setlocal EnableExtensions
REM Publica la consola (worker). Doble clic en la raiz del repo.

set "REPO=%~dp0"
set "PUBLISH_NOPAUSE=1"

echo.
echo ========================================
echo  Publish SigmabotSync.Worker
echo ========================================
echo.

call "%REPO%Scripts\Publish-Console.bat"
if errorlevel 1 goto :fail

echo.
echo === Listo ===
echo   publish\console\
echo.
set "PUBLISH_NOPAUSE="
pause
exit /b 0

:fail
echo.
echo ERROR: publish incompleto.
pause
exit /b 1
