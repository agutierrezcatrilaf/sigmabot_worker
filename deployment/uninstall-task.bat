@echo off
setlocal

set TASK_NAME=SigmabotSync_Hourly

echo Eliminando tarea "%TASK_NAME%"...
schtasks /Delete /TN "%TASK_NAME%" /F

if errorlevel 1 (
  echo AVISO: La tarea no existe o no pudo eliminarse.
  exit /b 1
)

echo Tarea eliminada correctamente.
endlocal
