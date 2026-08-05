@echo off
setlocal EnableExtensions
REM Publica SigmabotSync.Console en publish\console\

set "REPO_ROOT=%~dp0.."
set "OUT=%REPO_ROOT%\publish\console"

echo.
echo === Publish consola (SigmabotSync.Console) ===
echo Salida: %OUT%
echo.

dotnet publish "%REPO_ROOT%\SigmabotSync.Console\SigmabotSync.Console.csproj" -c Release -o "%OUT%"
if errorlevel 1 (
  echo.
  echo ERROR: el publish fallo.
  if not defined PUBLISH_NOPAUSE pause
  exit /b 1
)

if exist "%REPO_ROOT%\deployment\run-sigmabot.bat" (
  if not exist "%OUT%\deployment" mkdir "%OUT%\deployment"
  copy /Y "%REPO_ROOT%\deployment\run-sigmabot.bat" "%OUT%\deployment\" >nul
  copy /Y "%REPO_ROOT%\deployment\install-task.bat" "%OUT%\deployment\" >nul
  copy /Y "%REPO_ROOT%\deployment\uninstall-task.bat" "%OUT%\deployment\" >nul
)

powershell -NoProfile -Command "Compress-Archive -Path '%OUT%\*' -DestinationPath '%REPO_ROOT%\publish\SigmabotSync.Console-Release.zip' -Force"

echo.
echo OK. Salida en: %OUT%
echo ZIP:          %REPO_ROOT%\publish\SigmabotSync.Console-Release.zip
echo.
if not defined PUBLISH_NOPAUSE pause
endlocal
