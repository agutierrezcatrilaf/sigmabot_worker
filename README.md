# SigmabotSync.Worker

**Consola batch** y workers de extracción/sincronización con Aconex. Repo independiente para GitLab (worker / tarea programada).

## Proyectos

| Proyecto | Rol |
|----------|-----|
| `SigmabotSync.Console` | Host / scheduler |
| `SigmabotSync.Application` | Workers de sync y extracción |
| `SigmabotSync.Domain` | Entidades y reglas (copia; puede divergir de la API) |
| `SigmabotSync.Infrastructure` | Clientes HTTP Aconex + servicios runtime |
| `SigmabotSync.Infrastructure.Config` | `TrabajosEjecucionService` y utilidades SQL compartidas |
| `SigmabotSync.Tools.NetShareSmokeTest` | Diagnóstico de shares |

## Build

```bash
dotnet build SigmabotSync.Worker.sln -c Release
```

## Publish

**Desde la raíz del repo** (recomendado):

```bash
Publish-Worker.bat
```

o:

```bash
Scripts\Publish-Console.bat
```

Salida: `publish\console\` y ZIP `publish\SigmabotSync.Console-Release.zip` (incluye `settings.json.example` y scripts `deployment\`).

## Despliegue

Ver `deployment\README-OPERACION.md` e `install-task.bat`.

## Relación con otros repos

- **SigmabotSync.Api** — configurador web (misma BD; no llama a esta consola).
- **SigmabotConfig** — front Angular (no depende del worker).

Scripts SQL en `Scripts/` aplican a la BD compartida.
