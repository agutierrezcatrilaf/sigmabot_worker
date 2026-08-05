# SigmabotSync
## Instructivo de Despliegue y Operacion

**Proyecto:** SigmabotSync  
**Documento:** Guia de despliegue para equipo receptor  
**Version de entrega:** 1.0.0  
**Fecha:** 2026-05-07  
**Responsable de entrega:** ______________________

---

### Control de cambios

| Version | Fecha       | Descripcion                          |
|---------|-------------|--------------------------------------|
| 1.0.0   | 2026-05-07  | Version inicial para entrega formal. |

## 1) Objetivo

Este documento describe como desplegar `SigmabotSync` en servidores Windows, programar su ejecucion cada una hora mediante el Programador de tareas y validar su funcionamiento inicial.

---

## 2) Artefactos a entregar

Entregar una carpeta de release que incluya:

- `SigmabotSync.Console.exe`
- `SigmabotSync.Console.dll`
- Todas las DLL dependientes necesarias para la ejecucion
- `settings.json` configurado para el ambiente destino
- Scripts operativos:
  - `run-sigmabot.bat`
  - `install-task.bat`
  - `uninstall-task.bat`

**Recomendacion:** entregar siempre la carpeta completa de ejecutables y librerias (no solo el `.exe`).

---

## 3) Requisitos en servidor

- Windows Server con permisos para crear tareas programadas.
- Usuario de servicio con permisos de:
  - Lectura/escritura sobre carpeta de despliegue y logs.
  - Acceso a SQL Server para las bases requeridas por el proceso.
  - Conectividad hacia Aconex.
- Archivo `settings.json` con `DatabaseConnectionString` valido.

### Requisitos de red y acceso

- Permitir salida HTTPS hacia Aconex (puerto `443`) desde el servidor.
- Verificar acceso a SQL Server en el puerto configurado (habitualmente `1433`).
- Asegurar permisos de lectura/escritura en la ruta de descarga definida en `BasePath` (si aplica al trabajo).
- Confirmar que la cuenta que ejecuta la tarea (`SYSTEM` o `SERVICE_ACCOUNT`) tenga permisos sobre carpetas locales, rutas compartidas y base de datos segun la configuracion.
- Asegurar que, desde el contexto donde se ejecuta `SigmabotSync.Console.exe`, exista permiso de lectura y escritura sobre el `BasePath` definido para guardar archivos fisicos.

---

## 4) Comportamiento operativo del ejecutable

Al ejecutarse, `SigmabotSync.Console.exe`:

1. Lee `settings.json`.
2. Consulta trabajos pendientes segun la configuracion en base de datos.
3. Ejecuta los trabajos activos configurados.
4. Evita duplicar corridas cuando detecta ejecuciones en curso.
5. Registra trazas y resultados de ejecucion.

En terminos operativos: cada corrida revisa que trabajos corresponde ejecutar y procesa los pendientes.

---

## 5) Validacion post-despliegue

1. Ejecutar manualmente una vez:
   - `SigmabotSync.Console.exe`
2. Verificar:
   - El proceso inicia y finaliza sin error critico.
   - Se generan logs del proceso.
   - Se registran estados/resultados en tablas de control de ejecucion.

---

## 6) Programacion de tarea cada 1 hora

La tarea se instala mediante `install-task.bat`.  
Este script:

- Crea carpeta de logs si no existe.
- Elimina la tarea anterior (si existe).
- Crea la tarea recurrente cada 1 hora.

Antes de ejecutarlo, ajustar las variables de ambiente del script.

### Parametros editables en `install-task.bat`

- `TASK_NAME`: nombre de la tarea en el Programador de tareas.
- `APP_DIR`: carpeta donde quedo desplegado el ejecutable.
- `RUN_BAT`: ruta del script `run-sigmabot.bat`.
- `START_TIME`: hora de inicio de la primera corrida (formato `HH:mm`).
- `RUN_MODE`: modo de ejecucion de la tarea:
  - `SYSTEM`: usa cuenta del sistema local (no requiere usuario/password).
  - `SERVICE_ACCOUNT`: usa una cuenta de servicio.
- `RUN_AS`: usuario de servicio (solo si `RUN_MODE=SERVICE_ACCOUNT`).
- `RUN_PWD`: password de la cuenta de servicio (solo si `RUN_MODE=SERVICE_ACCOUNT`).

### Recomendacion de seguridad

- No almacenar credenciales reales en repositorios.
- Completar `RUN_PWD` solo en el servidor destino y con acceso restringido.

### Ubicacion de logs

- Log principal del aplicativo (definido en el codigo): `%APP_DIR%\Logs\SigmabotSync_yyyy-MM-dd.log`
- Log de tarea programada (opcional, definido en `run-sigmabot.bat`): `%APP_DIR%\logs\task-run.log`
- `APP_DIR` se define en los scripts de despliegue. Ejemplo habitual: `C:\Sigmabot\SigmabotSync`.

