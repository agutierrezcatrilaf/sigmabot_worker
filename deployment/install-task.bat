@echo off
setlocal

REM ===========================
REM CONFIGURAR ESTAS VARIABLES
REM ===========================
set TASK_NAME=SigmabotSync
REM QA: misma raiz del sitio configsigmabot, subcarpeta worker.
set APP_DIR=E:\inetpub\wwwroot\configsigmabot\worker
set RUN_BAT=%APP_DIR%\deployment\run-sigmabot.bat
set RUN_MODE=SYSTEM
REM RUN_MODE admite:
REM - SYSTEM          -> ejecuta como cuenta LocalSystem (no requiere password)
REM - SERVICE_ACCOUNT -> ejecuta con usuario/password de servicio
set RUN_AS=DOMINIO\svc_sigmabot
set RUN_PWD=REEMPLAZAR_PASSWORD
set START_TIME=00:00
REM Intervalo en horas (1 a 23). Instalacion inicial: cada 1 hora, primera corrida a START_TIME.
set INTERVAL_HOURS=1

set LOG_DIR=%APP_DIR%\logs
if not exist "%LOG_DIR%" mkdir "%LOG_DIR%"

if not exist "%RUN_BAT%" (
  echo ERROR: No se encontro el script de ejecucion en:
  echo %RUN_BAT%
  exit /b 1
)

echo Eliminando tarea previa (si existe)...
schtasks /Delete /TN "%TASK_NAME%" /F >nul 2>&1

if "%INTERVAL_HOURS%"=="" set INTERVAL_HOURS=1

echo Creando tarea programada cada %INTERVAL_HOURS% hora(s)...
if /I "%RUN_MODE%"=="SYSTEM" (
  schtasks /Create ^
   /TN "%TASK_NAME%" ^
   /TR "\"%RUN_BAT%\"" ^
   /SC HOURLY ^
   /MO %INTERVAL_HOURS% ^
   /ST %START_TIME% ^
   /RU "SYSTEM" ^
   /RL HIGHEST ^
   /F
) else (
  schtasks /Create ^
   /TN "%TASK_NAME%" ^
   /TR "\"%RUN_BAT%\"" ^
   /SC HOURLY ^
   /MO %INTERVAL_HOURS% ^
   /ST %START_TIME% ^
   /RU "%RUN_AS%" ^
   /RP "%RUN_PWD%" ^
   /RL HIGHEST ^
   /F
)

if errorlevel 1 (
  echo ERROR: No se pudo crear la tarea programada.
  exit /b 1
)

echo.
echo Tarea "%TASK_NAME%" creada/actualizada correctamente.
echo Ejecutable: %RUN_BAT%
echo Frecuencia: cada %INTERVAL_HOURS% hora(s), desde las %START_TIME%
echo Modo de ejecucion: %RUN_MODE%

endlocal
