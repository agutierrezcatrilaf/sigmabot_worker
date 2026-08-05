# SigmabotSync.Infrastructure.Config

Capa de persistencia SQL usada por **SigmabotConfig.Api** (configurador web).

- Editores CRUD (`ConfigurationEditor/*`)
- `TrabajosEjecucionService` (consulta/registro compartido con la consola)
- Utilidades SQL (`ConnectionStringHelper`, `SqlDataReaderMapper`)

**No incluye** clientes HTTP ni adaptadores Aconex (`SigmabotSync.Infrastructure`).

Para publicar solo el backend del configurador en GitLab, alcanza con:

- `SigmabotConfig.Api`
- `SigmabotSync.Domain`
- `SigmabotSync.Infrastructure.Config`
