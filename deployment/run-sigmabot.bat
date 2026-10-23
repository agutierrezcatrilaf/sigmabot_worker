@echo off
setlocal

REM QA: misma raiz del sitio configsigmabot, subcarpeta worker.
set APP_DIR=E:\inetpub\wwwroot\configsigmabot\worker
set EXE_NAME=SigmabotSync.Console.exe
set LOG_DIR=%APP_DIR%\logs
set LOG_FILE=%LOG_DIR%\task-run.log

if not exist "%LOG_DIR%" mkdir "%LOG_DIR%"

cd /d "%APP_DIR%"
"%APP_DIR%\%EXE_NAME%" >> "%LOG_FILE%" 2>&1

endlocal
