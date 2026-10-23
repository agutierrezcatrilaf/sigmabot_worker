# Cómo instalar el worker en QA

Programa que descarga y carga archivos de Aconex. Se copia a una carpeta del servidor y Windows lo ejecuta solo, cada 1 hora.

**Ambiente:** QA  
**Servidor:** QAAPP06 (172.20.1.102), el mismo de la API y del sitio  
**Versión:** 1.3.2 — 15 de octubre de 2026

## 1. Dónde queda instalado

| Qué | Valor para QA |
|-----|----------------|
| Carpeta | La definen al instalar. Sugerida: `E:\inetpub\wwwroot\configsigmabot\worker` |
| Cuenta de Windows | SYSTEM |
| Horario | Todos los días desde las 00:00, cada 1 hora |
| Nombre de la tarea | SigmabotSync |

Ese servidor ya tiene conexión a la base de datos y al File Server. La carga y la descarga de archivos usan el mismo File Server.

Esa carpeta se escribe en la variable `APP_DIR`, en `install-task.bat` y en `run-sigmabot.bat`, antes de crear la tarea. Los dos archivos tienen que tener la misma ruta. Con eso la tarea sabe dónde está `SigmabotSync.Console.exe`.

Si usan la carpeta sugerida, queda dentro del sitio web. Hay que dejarla fuera del sitio para que nadie pueda abrir `settings.json` desde el navegador: ese archivo tiene la clave de la base de datos.

## 2. Qué hay que tener a mano

- La carpeta del release `sigmabot-worker` (el programa, las DLL y la carpeta `deployment`).
- El archivo `settings.json` que se envía junto con este instructivo. El `settings.json.example` del release es solo una plantilla vacía.
- .NET 8, que en este servidor ya está instalado porque ahí corre la API.
- Permiso de administrador en QAAPP06, para crear la tarea programada.

## 3. Cómo debe verse la carpeta

```
E:\inetpub\wwwroot\configsigmabot\worker\
  SigmabotSync.Console.exe
  SigmabotSync.Console.dll
  (el resto de DLL del release)
  settings.json
  deployment\
    install-task.bat
    run-sigmabot.bat
    uninstall-task.bat
  Logs\
```

`settings.json` va al lado del `.exe`, no dentro de `deployment`. La carpeta `Logs` se crea sola en la primera ejecución.

## 4. Pasos de instalación

### Paso 1. Copiar el programa

Elegir la carpeta del servidor y copiar ahí todo el contenido del release. La sugerida es `E:\inetpub\wwwroot\configsigmabot\worker`. El ejecutable queda en esa carpeta, y los `.bat` en la subcarpeta `deployment`.

### Paso 2. Copiar la configuración

Copiar el `settings.json` recibido al lado del ejecutable. Con la carpeta sugerida:

`E:\inetpub\wwwroot\configsigmabot\worker\settings.json`

Ese archivo apunta a la base `Aconex_DataLake` y trae la misma clave de cifrado que la API, para poder leer las credenciales que ya están guardadas.

### Paso 3. Escribir la carpeta del ejecutable

Abrir con el Bloc de notas `deployment\install-task.bat` y `deployment\run-sigmabot.bat`. En los dos, `APP_DIR` es la carpeta elegida en el paso 1, la que contiene `SigmabotSync.Console.exe`.

Si usan la carpeta sugerida, déjenla así en los dos archivos:

```
set APP_DIR=E:\inetpub\wwwroot\configsigmabot\worker
```

Si eligen otra, reemplacen esa ruta en los dos archivos por la carpeta real. Tiene que quedar idéntica en ambos.

En `install-task.bat` también quedan el horario y la cuenta:

```
set RUN_MODE=SYSTEM
set START_TIME=00:00
set INTERVAL_HOURS=1
```

`INTERVAL_HOURS` es cada cuántas horas se repite. Acepta de 1 a 23. Con `1` el programa revisa los trabajos cada hora, a partir de las 00:00.

### Paso 4. Crear la tarea de Windows

Abrir el Símbolo del sistema como administrador, entrar a la subcarpeta `deployment` de la carpeta elegida y ejecutar `install-task.bat`. Con la carpeta sugerida:

```
cd /d E:\inetpub\wwwroot\configsigmabot\worker\deployment
install-task.bat
```

Si sale bien, la última línea dice que la tarea `SigmabotSync` quedó creada, cada 1 hora, desde las 00:00, con la cuenta SYSTEM.

Para verla: Programador de tareas de Windows, buscar `SigmabotSync`.

### Paso 5. Probarlo una vez

```
cd /d E:\inetpub\wwwroot\configsigmabot\worker
SigmabotSync.Console.exe
```

Si eligieron otra carpeta, usen esa en lugar de la ruta del ejemplo.

El programa lee la configuración, busca en la base los trabajos activos y procesa los que correspondan. Si ya hay una ejecución en curso, no abre otra.

Quedó bien si el programa termina y aparece un archivo en `E:\inetpub\wwwroot\configsigmabot\worker\Logs`.

## 5. Si hay que cambiar la frecuencia

Editar `INTERVAL_HOURS` en `install-task.bat` y volver a ejecutar ese `.bat` como administrador. Por ejemplo, `6` lo deja cada 6 horas, siempre contando desde las 00:00.

Para cambiar la hora de la primera ejecución del día, editar `START_TIME` con formato `HH:mm` y ejecutar de nuevo el mismo `.bat`.

## 6. Si no puede leer el File Server

La instalación usa la cuenta SYSTEM. Si la prueba del paso 5 no puede leer o guardar archivos en el File Server, en `install-task.bat` cambiar:

```
set RUN_MODE=SERVICE_ACCOUNT
set RUN_AS=DOMINIO\usuario
set RUN_PWD=la contraseña
```

La contraseña se escribe solo en el servidor. Después ejecutar otra vez `install-task.bat` como administrador.

Para eliminar la tarea: ejecutar `deployment\uninstall-task.bat` como administrador.

## 7. Botón Ejecutar ahora del configurador

Ese botón lo habilita la API, en el mismo servidor. En el `appsettings` de la API dejar:

```json
"OnDemandExecution": {
  "Enabled": true,
  "WorkerExePath": "E:\\inetpub\\wwwroot\\configsigmabot\\worker\\SigmabotSync.Console.exe"
}
```

- `Enabled` en `true` muestra el botón. En `false` no aparece.
- `WorkerExePath` es la ruta completa del `.exe` de la carpeta elegida en el paso 1. El archivo tiene que existir. Si la carpeta no es la sugerida, cambien esta ruta.

Reiniciar la API. El botón queda en Trabajos y en Historial.

## 8. Dónde ver los logs

La tarea de las 00:00 y el botón Ejecutar ahora usan el mismo programa y la misma carpeta de logs.

| Archivo | Para qué sirve |
|---------|----------------|
| `Logs\SigmabotSync_yyyy-MM-dd.log` | Todo lo que hizo el programa ese día. |
| `Logs\job-{idTrabajo}-ejec-{idEjecucion}.log` | El detalle de una ejecución. Es el archivo que muestra el historial del configurador. |
| `Logs\task-run.log` | La salida de la tarea programada. |

La carpeta de logs queda junto al ejecutable, en `Logs`. Con la carpeta sugerida es `E:\inetpub\wwwroot\configsigmabot\worker\Logs`. En `settings.json`, `Logging.Directory` queda en `Logs` y `Logging.Level` en `Info`.

Para abrir el log desde el configurador, en el `appsettings` de la API incluir la carpeta y reiniciar la API:

```json
"WorkerLogs": {
  "AllowedDirectories": [
    "E:\\inetpub\\wwwroot\\configsigmabot\\worker\\Logs"
  ]
}
```

Si la carpeta del programa es otra, cambien también esa ruta. Si `AllowedDirectories` queda vacío, el configurador no abre el archivo. Después de usar Ejecutar ahora, el historial debe mostrar el `job-...` de esa corrida.
