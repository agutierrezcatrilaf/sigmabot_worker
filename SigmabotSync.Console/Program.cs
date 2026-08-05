using SigmabotSync.Application.Extraction;
using SigmabotSync.Application.FileExtraction;
using SigmabotSync.Application.Synchronization;
using SigmabotSync.Domain.Config;
using SigmabotSync.Domain.Configuration;
using SigmabotSync.Domain.Entities;
using SigmabotSync.Domain.Ports;
using SigmabotSync.Infrastructure.External;
using SigmabotSync.Infrastructure.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SigmabotSync.Console
{
    class Program
    {
        /// <summary>
        /// Al depurar: pon aquí el Id del trabajo a ejecutar (ej. 1) y al dar F5 se ejecutará solo ese trabajo.
        /// Pon null para usar argumentos de línea de comandos o el scheduler.
        /// </summary>
#if DEBUG
        private static readonly int? DebugIdTrabajo = 10008;
#else
        private static readonly int? DebugIdTrabajo = null;
#endif

        static async Task Main(string[] args)
        {
            DailyLog.Inicializar();
            SigmabotSync.Application.Common.Utilities.Wlog("[SigmaBot] Inicio proceso " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " | BaseDirectory=" + AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\'), 0);
            SigmabotSync.Application.Common.Utilities.Wlog("=== SigmaBot File Extraction Console ===", 0);
            SigmabotSync.Application.Common.Utilities.Wlog("Log: " + DailyLog.GetRutaLogActual(), 0);
            SigmabotSync.Application.Common.Utilities.Wlog("", 0);

            string connectionString = ObtenerConnectionStringDesdeSettings();
            if (connectionString == null)
                return;

            // Al debuggear: si DebugIdTrabajo está definido, ejecutar solo ese trabajo (ignora args)
            if (DebugIdTrabajo.HasValue)
            {
                SigmabotSync.Application.Common.Utilities.Wlog("[Debug] Ejecutando trabajo Id=" + DebugIdTrabajo.Value + " (DebugIdTrabajo en código)", 0);
                SigmabotSync.Application.Common.Utilities.Wlog("", 0);
                await EjecutarUnTrabajoAsync(connectionString, DebugIdTrabajo.Value, "Local");
                return;
            }

            // Modo local: --local <id> o -l <id> (para desarrollo; ejecuta solo ese trabajo)
            var (idLocal, esLocal) = ObtenerIdTrabajoLocal(args);
            if (idLocal.HasValue)
            {
                SigmabotSync.Application.Common.Utilities.Wlog(esLocal ? "Modo local: ejecutando trabajo Id=" + idLocal.Value : "Modo manual: ejecutando trabajo Id=" + idLocal.Value, 0);
                SigmabotSync.Application.Common.Utilities.Wlog("", 0);
                await EjecutarUnTrabajoAsync(connectionString, idLocal.Value, esLocal ? "Local" : "Manual");
                return;
            }

            var pendientes = ObtenerTrabajosPendientesParaScheduler(connectionString);
            if (pendientes != null && pendientes.Count > 0)
            {
                var pendientesNoEnCurso = new List<int>();
                foreach (var id in pendientes)
                {
                    if (ExisteEjecucionEnCurso(connectionString, id))
                    {
                        var msg = "Trabajo Id=" + id + " omitido: ya está en ejecución (FechaHoraFin NULL en TrabajosEjecucion).";
                        SigmabotSync.Application.Common.Utilities.Wlog("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + msg, 0);
                    }
                    else
                        pendientesNoEnCurso.Add(id);
                }
                pendientes = pendientesNoEnCurso;
            }
            if (pendientes != null && pendientes.Count > 0)
            {
                SigmabotSync.Application.Common.Utilities.Wlog("Modo scheduler: " + pendientes.Count + " trabajo(s) pendiente(s) según TrabajosProgramacion.", 0);
                SigmabotSync.Application.Common.Utilities.Wlog("", 0);
                foreach (var idTrabajo in pendientes)
                {
                    SigmabotSync.Application.Common.Utilities.Wlog("--- Ejecutando trabajo Id=" + idTrabajo + " ---", 0);
                    await EjecutarUnTrabajoAsync(connectionString, idTrabajo, "Scheduler");
                    SigmabotSync.Application.Common.Utilities.Wlog("", 0);
                }
                SigmabotSync.Application.Common.Utilities.Wlog("Scheduler: ejecución finalizada.", 0);
            }
            else
            {
                var enCurso = ObtenerIdsTrabajosEnCurso(connectionString);
                if (enCurso != null && enCurso.Count > 0)
                {
                    var msg = "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] No hay trabajos pendientes. Hay " + enCurso.Count +
                        " trabajo(s) en ejecución (Id=" + string.Join(", Id=", enCurso) + "). No se relanza ninguna instancia.";
                    SigmabotSync.Application.Common.Utilities.Wlog(msg, 0);
                }
                else
                {
                    var msg = "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] Scheduler: ejecución completada sin trabajos pendientes.";
                    SigmabotSync.Application.Common.Utilities.Wlog(msg, 0);
                }
            }
        }

        /// <summary>Devuelve los IdTrabajo que tienen una ejecución en curso (para informar en log).</summary>
        static IReadOnlyList<int> ObtenerIdsTrabajosEnCurso(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString)) return new int[0];
            try
            {
                var servicio = new TrabajosEjecucionService(connectionString);
                return servicio.ObtenerIdsTrabajosEnCurso();
            }
            catch (Exception ex)
            {
                SigmabotSync.Application.Common.Utilities.Wlog($"[Aviso] ObtenerIdsTrabajosEnCurso: no se pudo consultar TrabajosEjecucion: {ex.Message}", 0);
                return new int[0];
            }
        }

        /// <summary>
        /// Parsea los argumentos y devuelve el IdTrabajo si se solicitó ejecución manual o local.
        /// Formas: --local 2, -l 2 (modo local), --manual 2, -m 2, o solo 2 (un único número).
        /// En local se registra como tipo ejecución "Local" en el historial.
        /// </summary>
        /// <returns>Tupla (idTrabajo, esLocal). esLocal true solo para --local/-l.</returns>
        static (int? id, bool esLocal) ObtenerIdTrabajoLocal(string[] args)
        {
            if (args == null || args.Length == 0)
                return (null, false);
            for (int i = 0; i < args.Length; i++)
            {
                var arg = (args[i] ?? "").Trim();
                if (arg == "--local" || arg == "-l")
                {
                    if (i + 1 < args.Length && int.TryParse(args[i + 1].Trim(), out int id) && id > 0)
                        return (id, true);
                    return (null, false);
                }
                if (arg == "--manual" || arg == "-m")
                {
                    if (i + 1 < args.Length && int.TryParse(args[i + 1].Trim(), out int id) && id > 0)
                        return (id, false);
                    return (null, false);
                }
                if (args.Length == 1 && int.TryParse(arg, out int idUnico) && idUnico > 0)
                    return (idUnico, false);
            }
            return (null, false);
        }

        /// <summary>
        /// Ejecuta un solo trabajo: configuración, credenciales, extracción de archivos, sincronización, guardado de resultado e historial.
        /// </summary>
        /// <param name="tipoEjecucion">"Manual" o "Scheduler" (se guarda en TrabajosEjecucion.TipoEjecucion).</param>
        static async Task EjecutarUnTrabajoAsync(string connectionString, int idTrabajo, string tipoEjecucion = "Scheduler")
        {
            DateTime? fechaInicioEjecucion = null;
            int? idEjecucion = null;
            var etapasEjecutadas = new List<string>();
            bool exito = false;
            string mensajeError = null;
            string detalleError = null;

            try
            {
                if (ExisteEjecucionEnCurso(connectionString, idTrabajo))
                {
                    var msg = "El trabajo Id=" + idTrabajo + " ya está en ejecución. No se inicia otra instancia.";
                    SigmabotSync.Application.Common.Utilities.Wlog("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + msg, 0);
                    return;
                }

                TrabajoConfiguracion trabajoConfig = ObtenerYValidarConfiguracionTrabajo(idTrabajo, connectionString);
                if (trabajoConfig == null)
                    return;

                if (!ObtenerYValidarCredenciales(trabajoConfig, connectionString, out var credAconex, out var credBd))
                    return;

                string tipoTrabajo = (trabajoConfig.TipoTrabajo ?? "").Trim();
                bool tipoValido = tipoTrabajo == TipoTrabajoIds.FileExtraction
                    || tipoTrabajo == TipoTrabajoIds.ProjectSync
                    || tipoTrabajo == TipoTrabajoIds.FullExtraction
                    || tipoTrabajo == TipoTrabajoIds.FileUploadWithMetadata;

                if (!tipoValido)
                {
                    mensajeError = string.IsNullOrEmpty(tipoTrabajo)
                        ? "Tipo de trabajo no configurado (campo Tipo en tabla Trabajos). Use: FileExtraction, ProjectSync, FullExtraction o FileUploadWithMetadata."
                        : "Tipo de trabajo no reconocido: " + tipoTrabajo + ". Use: FileExtraction, ProjectSync, FullExtraction o FileUploadWithMetadata.";
                    SigmabotSync.Application.Common.Utilities.Wlog("No se ejecuta: " + mensajeError, 0);
                    GuardarResultadoTrabajo(connectionString, idTrabajo, exito: false, mensajeError);
                    return;
                }

                SigmabotSync.Application.Common.Utilities.Wlog("Tipo de trabajo: " + tipoTrabajo, 0);
                SigmabotSync.Application.Common.Utilities.Wlog("", 0);

                fechaInicioEjecucion = DateTime.Now;
                idEjecucion = InsertarInicioEjecucion(connectionString, idTrabajo, fechaInicioEjecucion.Value, tipoEjecucion);

                switch (tipoTrabajo)
                {
                    case TipoTrabajoIds.FileExtraction:
                        await EjecutarExtraccionArchivosAsync(connectionString, trabajoConfig, credAconex, credBd, etapasEjecutadas);
                        SincronizarMetadataDocumentos(trabajoConfig, credAconex, credBd, etapasEjecutadas);
                        break;
                    case TipoTrabajoIds.ProjectSync:
                        await EjecutarProjectSyncAsync(trabajoConfig, credAconex, credBd, etapasEjecutadas);
                        break;
                    case TipoTrabajoIds.FullExtraction:
                        await EjecutarFullExtractionAsync(trabajoConfig, credAconex, credBd, etapasEjecutadas);
                        break;
                    case TipoTrabajoIds.FileUploadWithMetadata:
                        await EjecutarFileUploadWithMetadataAsync(trabajoConfig, credAconex, credBd, etapasEjecutadas);
                        break;
                }

                SigmabotSync.Application.Common.Utilities.Wlog("=== Extracción completada exitosamente (IdTrabajo=" + idTrabajo + ") ===", 0);
                exito = true;
                GuardarResultadoTrabajo(connectionString, idTrabajo, exito: true, null);
            }
            catch (Exception ex)
            {
                SigmabotSync.Application.Common.Utilities.Wlog("ERROR: " + ex.Message, 0);
                SigmabotSync.Application.Common.Utilities.Wlog("Stack Trace: " + ex.StackTrace, 0);

                mensajeError = ex.Message;
                detalleError = FormatearDetalleEjecucionParaHistorial(ex);
                GuardarResultadoTrabajo(connectionString, idTrabajo, exito: false, ex.Message);
            }
            finally
            {
                if (idEjecucion.HasValue)
                {
                    ActualizarFinEjecucion(
                        connectionString,
                        idEjecucion.Value,
                        DateTime.Now,
                        exito,
                        mensajeError,
                        etapasEjecutadas,
                        exito ? null : detalleError);
                }
            }
        }

        /// <summary>
        /// Texto legible para <c>TrabajosEjecucion.DetalleEjecucion</c>: tipo y mensajes (incl. internos o agregados), sin rutas de archivo ni stack trace.
        /// El stack trace completo sigue yendo al log en consola/archivo.
        /// </summary>
        private static string FormatearDetalleEjecucionParaHistorial(Exception ex)
        {
            if (ex == null) return null;

            var sb = new StringBuilder();

            if (ex is AggregateException agg)
            {
                sb.Append(agg.GetType().Name).Append(": ").AppendLine(agg.Message ?? "");
                int i = 0;
                foreach (Exception inner in agg.Flatten().InnerExceptions)
                {
                    i++;
                    sb.Append("  [").Append(i).Append("] ").Append(inner.GetType().Name).Append(": ").AppendLine(inner.Message ?? "");
                }
                return sb.ToString().TrimEnd();
            }

            Exception e = ex;
            bool first = true;
            while (e != null)
            {
                if (!first)
                    sb.AppendLine().Append("Causa interna: ");
                sb.Append(e.GetType().Name).Append(": ").Append(e.Message ?? "");
                e = e.InnerException;
                first = false;
            }

            return sb.ToString();
        }

        /// <summary>Comprueba si el trabajo tiene una ejecución en curso (FechaHoraFin NULL) para no lanzar duplicados.</summary>
        static bool ExisteEjecucionEnCurso(string connectionString, int idTrabajo)
        {
            if (string.IsNullOrWhiteSpace(connectionString)) return false;
            try
            {
                var servicio = new TrabajosEjecucionService(connectionString);
                return servicio.ExisteEjecucionEnCurso(idTrabajo);
            }
            catch (Exception ex)
            {
                SigmabotSync.Application.Common.Utilities.Wlog($"[Aviso] ExisteEjecucionEnCurso IdTrabajo={idTrabajo}: {ex.Message}", 0);
                return false;
            }
        }

        /// <summary>Registra el inicio de la ejecución en TrabajosEjecucion (FechaHoraFin NULL). Devuelve el Id del registro para actualizarlo al finalizar.</summary>
        static int InsertarInicioEjecucion(string connectionString, int idTrabajo, DateTime fechaHoraInicio, string tipoEjecucion)
        {
            var servicio = new TrabajosEjecucionService(connectionString);
            return servicio.InsertarInicio(idTrabajo, fechaHoraInicio, tipoEjecucion);
        }

        /// <summary>Actualiza el registro de ejecución con la hora fin y el resultado.</summary>
        static void ActualizarFinEjecucion(string connectionString, int idEjecucion, DateTime fechaHoraFin, bool exito, string mensajeError, List<string> etapasEjecutadas, string detalleEjecucion)
        {
            if (string.IsNullOrWhiteSpace(connectionString)) return;
            try
            {
                var servicio = new TrabajosEjecucionService(connectionString);
                servicio.ActualizarFin(idEjecucion, fechaHoraFin, exito, mensajeError, etapasEjecutadas, detalleEjecucion);
            }
            catch (Exception ex)
            {
                SigmabotSync.Application.Common.Utilities.Wlog($"[Aviso] No se pudo actualizar historial en TrabajosEjecucion: {ex.Message}", 0);
            }
        }

        /// <summary>
        /// Obtiene los IdTrabajo que deben ejecutarse ahora según TrabajosProgramacion
        /// y que aún no se han ejecutado hoy en su ventana horaria (evita repetir ejecución).
        /// Para usar desde un scheduler: llamar cada X minutos y por cada id ejecutar el flujo del trabajo.
        /// </summary>
        public static IReadOnlyList<int> ObtenerTrabajosPendientesParaScheduler(string connectionString, DateTime? ahora = null)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                return new int[0];
            try
            {
                var servicio = new TrabajosProgramacionService(connectionString);
                return servicio.ObtenerTrabajosPendientesDeEjecucion(ahora ?? DateTime.Now);
            }
            catch (Exception ex)
            {
                SigmabotSync.Application.Common.Utilities.Wlog($"[Aviso] ObtenerTrabajosPendientesParaScheduler: no se pudo consultar TrabajosProgramacion: {ex.Message}", 0);
                return new int[0];
            }
        }

        /// <summary>
        /// Guarda en la tabla Trabajos el resultado de la última ejecución (éxito o error).
        /// No lanza si falla la actualización (ej. tabla no existe) para no ocultar el error original.
        /// </summary>
        static void GuardarResultadoTrabajo(string connectionString, int idTrabajo, bool exito, string mensajeError)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                return;
            try
            {
                var trabajosService = new TrabajosService(connectionString);
                trabajosService.ActualizarResultadoEjecucion(idTrabajo, exito, mensajeError);
            }
            catch (Exception ex)
            {
                SigmabotSync.Application.Common.Utilities.Wlog($"[Aviso] No se pudo actualizar resultado en tabla Trabajos: {ex.Message}", 0);
            }
        }

        /// <summary>
        /// Inserta un registro histórico en TrabajosEjecucion (detalle, error, etapas ejecutadas, tipo ejecución).
        /// No lanza si falla para no ocultar el error original.
        /// </summary>
        static void GuardarHistorialEjecucion(
            string connectionString,
            int idTrabajo,
            DateTime fechaHoraInicio,
            DateTime fechaHoraFin,
            bool exito,
            string mensajeError,
            List<string> etapasEjecutadas,
            string detalleEjecucion,
            string tipoEjecucion = "Scheduler")
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                return;
            try
            {
                var servicio = new TrabajosEjecucionService(connectionString);
                servicio.Insertar(idTrabajo, fechaHoraInicio, fechaHoraFin, exito, mensajeError, etapasEjecutadas, detalleEjecucion, tipoEjecucion);
            }
            catch (Exception ex)
            {
                SigmabotSync.Application.Common.Utilities.Wlog($"[Aviso] No se pudo guardar historial en TrabajosEjecucion: {ex.Message}", 0);
            }
        }

        /// <summary>
        /// Ejecuta la extracción de archivos desde Aconex usando FileExtractionWorker.
        /// Configura logging, eventos y registra la etapa "FileExtraction".
        /// </summary>
        private static async Task EjecutarExtraccionArchivosAsync(
            string connectionString,
            TrabajoConfiguracion trabajoConfig,
            Credencial credAconex,
            Credencial credBd,
            List<string> etapasEjecutadas)
        {
            string projectId = trabajoConfig.IdProyecto ?? string.Empty;
            string projectName = ObtenerNombreProyectoParaCarpeta(connectionString, trabajoConfig);
            string basePath = !string.IsNullOrWhiteSpace(trabajoConfig.BasePath) ? trabajoConfig.BasePath.Trim() : null;

            SigmabotSync.Application.Common.Utilities.Wlog("Configuración desde TrabajosConfiguracion (IdTrabajo=" + trabajoConfig.IdTrabajo + "):", 0);
            SigmabotSync.Application.Common.Utilities.Wlog($"  Proyecto={projectName}, IdProyecto={projectId}, BasePath={basePath ?? "(default)"}", 0);
            SigmabotSync.Application.Common.Utilities.Wlog($"  Credencial Aconex: {credAconex.Nombre} ({credAconex.Aconex_Instancia})", 0);
            SigmabotSync.Application.Common.Utilities.Wlog($"  Credencial BD: {credBd.Nombre}", 0);

            var config = FileExtractionConfig.FromCredencial(credAconex, projectId, basePath);
            config.ProjectName = projectName;
            var returnFields = trabajoConfig.ToReturnFields();
            if (returnFields != null && returnFields.Count > 0)
                config.ReturnFields = returnFields;

            using (var searchPort = new AconexRegisterSearchAdapter())
            using (var contentPort = new AconexRegisterDocumentContentAdapter())
            using (var worker = new FileExtractionWorker(config, searchPort, contentPort))
            {
                // Configurar eventos
                worker.OnProgress += (current, total) =>
                {
                    SigmabotSync.Application.Common.Utilities.Wlog($"[Progreso] Página {current} de {total} ({(current * 100 / total)}%)", 0);
                };

                worker.OnStatus += (status) =>
                {
                    SigmabotSync.Application.Common.Utilities.Wlog($"[Estado] {status}", 0);
                };

                SigmabotSync.Application.Common.Utilities.Wlog("Iniciando extracción de archivos...", 0);
                SigmabotSync.Application.Common.Utilities.Wlog("", 0);

                // Ejecutar extracción de archivos (Aconex) — descarga de documentos
                await worker.ProcessAllPagesAsync();
                etapasEjecutadas.Add("FileExtraction");
            }
        }

        /// <summary>
        /// Obtiene el nombre del proyecto para usar como carpeta raíz en FileExtraction.
        /// Prioriza la tabla Proyectos por ACXProjectId; si no hay dato, usa TrabajosConfiguracion.Proyecto.
        /// </summary>
        private static string ObtenerNombreProyectoParaCarpeta(string connectionString, TrabajoConfiguracion trabajoConfig)
        {
            string fallback = !string.IsNullOrWhiteSpace(trabajoConfig?.Proyecto) ? trabajoConfig.Proyecto.Trim() : "Proyecto";
            if (trabajoConfig == null || string.IsNullOrWhiteSpace(trabajoConfig.IdProyecto) || string.IsNullOrWhiteSpace(connectionString))
                return fallback;

            try
            {
                var trabajosService = new TrabajosService(connectionString);
                var nombre = trabajosService.GetNombreProyectoByAcxProjectId(trabajoConfig.IdProyecto.Trim());
                return !string.IsNullOrWhiteSpace(nombre) ? nombre : fallback;
            }
            catch (Exception ex)
            {
                SigmabotSync.Application.Common.Utilities.Wlog($"[Aviso] No se pudo resolver nombre de proyecto desde Proyectos (ACXProjectId={trabajoConfig.IdProyecto}): {ex.Message}", 0);
                return fallback;
            }
        }

        /// <summary>
        /// Sincroniza la metadata de documentos en la base de datos indicada por la credencial BD.
        /// Registra la etapa "DocumentExtraction" si se ejecuta.
        /// </summary>
        private static void SincronizarMetadataDocumentos(
            TrabajoConfiguracion trabajoConfig,
            Credencial credAconex,
            Credencial credBd,
            List<string> etapasEjecutadas)
        {
            string projectId = trabajoConfig.IdProyecto ?? string.Empty;
            string projectName = !string.IsNullOrWhiteSpace(trabajoConfig.Proyecto) ? trabajoConfig.Proyecto.Trim() : "Proyecto";
            var documentFieldMappings = trabajoConfig.ToDocumentFieldMappings();

            // Tras descargar archivos, sincronizar metadata de documentos en la BD indicada por la credencial BD
            var connectionStringDocs = credBd.GetConnectionString();
            if (!string.IsNullOrWhiteSpace(connectionStringDocs))
            {
                SigmabotSync.Application.Common.Utilities.Wlog("", 0);
                SigmabotSync.Application.Common.Utilities.Wlog("Sincronizando metadata de documentos en base de datos...", 0);

                var docConfig = ExtractionConfig.FromCredenciales(
                    credAconex,
                    credBd,
                    projectName,
                    documentFieldMappings
                );

                using (var registerSearchPort = new AconexRegisterSearchAdapter())
                {
                    var docWorker = new DocumentExtractionWorker(docConfig.ToDictionary(), connectionStringDocs, registerSearchPort);
                    docWorker.Documentos(projectId);
                }

                SigmabotSync.Application.Common.Utilities.Wlog("Sincronización de documentos completada.", 0);
                etapasEjecutadas.Add("DocumentExtraction");
            }
            else
            {
                SigmabotSync.Application.Common.Utilities.Wlog("(Credencial BD sin Servidor/BaseDatos: no se ejecuta sincronización de documentos)", 0);
            }
        }

        /// <summary>
        /// Ejecuta ProjectSync: lee transmitals (inbox lado 1 / sentbox lado 2) y sincroniza en el otro registro.
        /// </summary>
        private static async Task EjecutarProjectSyncAsync(
            TrabajoConfiguracion trabajoConfig,
            Credencial credAconex,
            Credencial credBd,
            List<string> etapasEjecutadas)
        {
            var proyectos = trabajoConfig.GetProyectosSync();
            string auth = SigmabotSync.Application.Common.Utilities.EncodeTexto(
                (credAconex.Aconex_Usuario ?? "") + ":" + (credAconex.Aconex_Clave ?? ""));
            string baseUrl = credAconex.GetAconexBaseUrl();
            int diasLookback = trabajoConfig.ResolverDiasLookbackTransmittal();
            string bdConnection = credBd.GetConnectionString();

            SigmabotSync.Application.Common.Utilities.Wlog("Configuración ProjectSync (IdTrabajo=" + trabajoConfig.IdTrabajo + "):", 0);
            foreach (var p in proyectos)
                SigmabotSync.Application.Common.Utilities.Wlog($"  Proyecto: {p.Label} ({p.ProjectId})", 0);
            SigmabotSync.Application.Common.Utilities.Wlog($"  DiasLookbackTransmittal={diasLookback}", 0);
            if (!string.IsNullOrWhiteSpace(trabajoConfig.IdEstatusDocumentoDestino))
                SigmabotSync.Application.Common.Utilities.Wlog(
                    $"  IdEstatusDocumentoDestino={trabajoConfig.IdEstatusDocumentoDestino.Trim()} (proyecto destino {trabajoConfig.IdProyecto})", 0);
            if (!string.IsNullOrWhiteSpace(trabajoConfig.SubjectFiltroTransmittalVuelta))
                SigmabotSync.Application.Common.Utilities.Wlog(
                    $"  SubjectFiltroTransmittalVuelta={trabajoConfig.SubjectFiltroTransmittalVuelta.Trim()}", 0);
            var camposRegistroDestino = trabajoConfig.ToReturnFieldsRegistroDestino();
            if (camposRegistroDestino != null && camposRegistroDestino.Count > 0)
                SigmabotSync.Application.Common.Utilities.Wlog(
                    $"  CamposConsultaRegistroDestino (Codelco)={camposRegistroDestino.Count} campo(s)", 0);
            var camposRegistroDestinoSalfa = trabajoConfig.ToReturnFieldsRegistroDestinoSalfa();
            if (camposRegistroDestinoSalfa != null && camposRegistroDestinoSalfa.Count > 0)
                SigmabotSync.Application.Common.Utilities.Wlog(
                    $"  CamposConsultaRegistroDestinoSalfa={camposRegistroDestinoSalfa.Count} campo(s)", 0);
            SigmabotSync.Application.Common.Utilities.Wlog("", 0);

            var httpGet = new AconexHttpGetAdapter();
            IMailTransmittalReadPort mailRead = new AconexMailTransmittalAdapter(httpGet);
            ITransmittalSyncStatePort syncState = new TransmittalSyncStateService(bdConnection);
            ITransmittalSyncFieldMapPort fieldMap = new TransmittalSyncFieldMapService(bdConnection);
            IAconexDocumentCatalogPort documentCatalog = new AconexDocumentCatalogService(bdConnection);

            using (var registerWritePort = new AconexRegisterWriteAdapter())
            using (var contentPort = new AconexRegisterDocumentContentAdapter())
            using (var registerSearchPort = new AconexRegisterSearchAdapter())
            using (var registerMetadataPort = new AconexRegisterMetadataAdapter())
            {
                var syncService = new TransmittalSyncService(
                    mailRead, registerWritePort, contentPort, registerSearchPort, registerMetadataPort, fieldMap, syncState, documentCatalog);
                var syncWorker = new TransmittalSyncWorker(syncService);

                syncWorker.OnStatus += status =>
                {
                    SigmabotSync.Application.Common.Utilities.Wlog($"[ProjectSync] {status}", 0);
                };

                var request = new TransmittalSyncRunRequest
                {
                    IdTrabajo = trabajoConfig.IdTrabajo,
                    BaseUrl = baseUrl,
                    AuthorizationHeaderBase64 = auth,
                    IntegrationId = credAconex.Aconex_IntegrationId ?? "",
                    OrgId = credAconex.Aconex_OrganizationId ?? "",
                    UserId = credAconex.Aconex_UserId ?? "",
                    DiasLookback = diasLookback,
                    Proyectos = proyectos,
                    IdEstatusDocumentoDestino = trabajoConfig.IdEstatusDocumentoDestino?.Trim(),
                    IdProyectoEstatusFijo = string.IsNullOrWhiteSpace(trabajoConfig.IdProyecto)
                        ? null
                        : trabajoConfig.IdProyecto.Trim(),
                    SubjectFiltroTransmittalVuelta = trabajoConfig.SubjectFiltroTransmittalVuelta?.Trim(),
                    IdProyecto2 = string.IsNullOrWhiteSpace(trabajoConfig.IdProyecto2)
                        ? null
                        : trabajoConfig.IdProyecto2.Trim(),
                    CamposConsultaRegistroDestino = camposRegistroDestino,
                    CamposConsultaRegistroDestinoSalfa = camposRegistroDestinoSalfa
                };

                await syncWorker.RunAsync(request);
                etapasEjecutadas.Add("ProjectSync");
            }
        }

        /// <summary>
        /// Ejecuta FullExtraction: Documentos, ProcessIncidents, Correos y FlujosdeTrabajo (workers de Extraction).
        /// </summary>
        private static async Task EjecutarFullExtractionAsync(
            TrabajoConfiguracion trabajoConfig,
            Credencial credAconex,
            Credencial credBd,
            List<string> etapasEjecutadas)
        {
            string projectId = trabajoConfig.IdProyecto ?? string.Empty;
            string projectName = !string.IsNullOrWhiteSpace(trabajoConfig.Proyecto) ? trabajoConfig.Proyecto.Trim() : "Proyecto";
            var documentFieldMappings = trabajoConfig.ToDocumentFieldMappings();
            var connectionStringDocs = credBd.GetConnectionString();

            if (string.IsNullOrWhiteSpace(connectionStringDocs))
            {
                throw new InvalidOperationException("FullExtraction requiere credencial BD con Servidor/BaseDatos configurado.");
            }

            var docConfig = ExtractionConfig.FromCredenciales(
                credAconex,
                credBd,
                projectName,
                documentFieldMappings);
            var configDict = docConfig.ToDictionary();

            using (var registerSearchPort = new AconexRegisterSearchAdapter())
            using (var httpGetPort = new AconexHttpGetAdapter())
            {
                SigmabotSync.Application.Common.Utilities.Wlog("FullExtraction: Documentos...", 0);
                var docWorker = new DocumentExtractionWorker(configDict, connectionStringDocs, registerSearchPort);
                docWorker.Documentos(projectId);
                etapasEjecutadas.Add("Documentos");

                //SigmabotSync.Application.Common.Utilities.Wlog("FullExtraction: ProcessIncidents...", 0);
                //var incidentWorker = new IncidentExtractionWorker(configDict, connectionStringDocs, httpGetPort);
                //incidentWorker.ProcessIncidents(projectId);
                //etapasEjecutadas.Add("ProcessIncidents");

                SigmabotSync.Application.Common.Utilities.Wlog("FullExtraction: Correos...", 0);
                var mailWorker = new MailExtractionWorker(configDict, connectionStringDocs, httpGetPort);
                mailWorker.Correos(projectId);
                etapasEjecutadas.Add("Correos");

                SigmabotSync.Application.Common.Utilities.Wlog("FullExtraction: FlujosdeTrabajo...", 0);
                var workflowWorker = new WorkflowExtractionWorker(configDict, connectionStringDocs, httpGetPort);
                workflowWorker.FlujosdeTrabajo(projectId);
                etapasEjecutadas.Add("FlujosdeTrabajo");
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Ejecuta FileUploadWithMetadata: lee DocumentosMetadata + DocumentosPath (CredencialBD) y envía a Aconex.
        /// </summary>
        private static async Task EjecutarFileUploadWithMetadataAsync(
            TrabajoConfiguracion trabajoConfig,
            Credencial credAconex,
            Credencial credBd,
            List<string> etapasEjecutadas)
        {
            string projectId = trabajoConfig.IdProyecto ?? string.Empty;
            string projectName = !string.IsNullOrWhiteSpace(trabajoConfig.Proyecto) ? trabajoConfig.Proyecto.Trim() : "Proyecto";
            string basePath = !string.IsNullOrWhiteSpace(trabajoConfig.BasePath) ? trabajoConfig.BasePath.Trim() : null;
            string tablaMetadata = FileUploadWithMetadataDefaults.ResolverTablaMetadata(trabajoConfig.TablaMetadata);
            string tablaPaths = FileUploadWithMetadataDefaults.ResolverTablaPaths(trabajoConfig.TablaPaths);

            SigmabotSync.Application.Common.Utilities.Wlog("Configuración FileUploadWithMetadata (IdTrabajo=" + trabajoConfig.IdTrabajo + "):", 0);
            SigmabotSync.Application.Common.Utilities.Wlog($"  Proyecto={projectName}, IdProyecto={projectId}, TablaMetadata={tablaMetadata}, TablaPaths={tablaPaths}", 0);
            SigmabotSync.Application.Common.Utilities.Wlog($"  Credencial Aconex: {credAconex.Nombre} ({credAconex.Aconex_Instancia})", 0);
            SigmabotSync.Application.Common.Utilities.Wlog($"  Credencial BD: {credBd.Nombre} → {credBd.BD_Servidor}/{credBd.BD_BaseDatos}", 0);
            SigmabotSync.Application.Common.Utilities.Wlog("", 0);

            using (var registerWritePort = new AconexRegisterWriteAdapter())
            {
                var worker = new FileUploadWithMetadataWorker(trabajoConfig, credAconex, credBd, registerWritePort);
                worker.OnProgress += (current, total) =>
                {
                    SigmabotSync.Application.Common.Utilities.Wlog($"[Progreso] {current} de {total}", 0);
                };
                worker.OnStatus += (status) =>
                {
                    SigmabotSync.Application.Common.Utilities.Wlog($"[Estado] {status}", 0);
                };

                await worker.RunAsync();
                etapasEjecutadas.Add("FileUploadWithMetadata");
            }
        }

        /// <summary>
        /// Lee el archivo de configuración (settings.json) y valida que exista una DatabaseConnectionString.
        /// En caso de error, muestra el mensaje y espera una tecla. Devuelve null si no es posible continuar.
        /// </summary>
        private static string ObtenerConnectionStringDesdeSettings()
        {
            var settingsService = new SettingsService();
            var settings = settingsService.Load();

            if (string.IsNullOrWhiteSpace(settings?.DatabaseConnectionString))
            {
                SigmabotSync.Application.Common.Utilities.Wlog("ERROR: DatabaseConnectionString no está configurado en settings.json", 0);
                SigmabotSync.Application.Common.Utilities.Wlog("Configura la conexión a la base de datos donde están las tablas Credenciales, Trabajos y TrabajosConfiguracion.", 0);
                return null;
            }

            return ConnectionStringHelper.AsegurarTrustServerCertificate(settings.DatabaseConnectionString.Trim());
        }

        /// <summary>
        /// Obtiene la configuración del trabajo desde la base de datos y valida los datos mínimos requeridos.
        /// Devuelve null si no es posible continuar.
        /// </summary>
        private static TrabajoConfiguracion ObtenerYValidarConfiguracionTrabajo(int idTrabajo, string connectionString)
        {
            var trabajosService = new TrabajosService(connectionString);
            TrabajoConfiguracion trabajoConfig = trabajosService.GetConfiguracionByIdTrabajo(idTrabajo);

            if (trabajoConfig == null)
            {
                SigmabotSync.Application.Common.Utilities.Wlog("ERROR: No hay configuración en TrabajosConfiguracion para IdTrabajo=" + idTrabajo + " o el trabajo no está en estado 'Activo' en la tabla Trabajos. Configure IdProyecto y el resto de parámetros en esas tablas.", 0);
                return null;
            }
            if (!trabajoConfig.CredencialAconexId.HasValue)
            {
                SigmabotSync.Application.Common.Utilities.Wlog("ERROR: Falta CredencialAconex en TrabajosConfiguracion (Id de la credencial en tabla Credenciales).", 0);
                return null;
            }
            if (!trabajoConfig.CredencialBDId.HasValue)
            {
                SigmabotSync.Application.Common.Utilities.Wlog("ERROR: Falta CredencialBD en TrabajosConfiguracion (Id de la credencial en tabla Credenciales).", 0);
                return null;
            }

            string tipoTrabajo = (trabajoConfig.TipoTrabajo ?? "").Trim();
            if (tipoTrabajo == TipoTrabajoIds.FileUploadWithMetadata)
            {
                string tablaMetadata = FileUploadWithMetadataDefaults.ResolverTablaMetadata(trabajoConfig.TablaMetadata);
                string tablaPaths = FileUploadWithMetadataDefaults.ResolverTablaPaths(trabajoConfig.TablaPaths);
                if (!string.Equals(tablaMetadata, FileUploadWithMetadataDefaults.TablaMetadata, StringComparison.OrdinalIgnoreCase)
                    || !string.Equals(tablaPaths, FileUploadWithMetadataDefaults.TablaPaths, StringComparison.OrdinalIgnoreCase))
                {
                    SigmabotSync.Application.Common.Utilities.Wlog(
                        "ERROR: FileUploadWithMetadata requiere TablaMetadata=" + FileUploadWithMetadataDefaults.TablaMetadata
                        + " y TablaPaths=" + FileUploadWithMetadataDefaults.TablaPaths + ".", 0);
                    return null;
                }
            }

            return trabajoConfig;
        }

        /// <summary>
        /// Obtiene las credenciales de Aconex y de BD asociadas a la configuración del trabajo y valida que existan.
        /// Devuelve false si no es posible continuar.
        /// </summary>
        private static bool ObtenerYValidarCredenciales(
            TrabajoConfiguracion trabajoConfig,
            string connectionString,
            out Credencial credAconex,
            out Credencial credBd)
        {
            credAconex = null;
            credBd = null;

            var credService = new CredencialesService(connectionString);
            credAconex = credService.GetById(trabajoConfig.CredencialAconexId.Value);
            credBd = credService.GetById(trabajoConfig.CredencialBDId.Value);

            if (credAconex == null)
            {
                SigmabotSync.Application.Common.Utilities.Wlog("ERROR: No se encontró Credencial Id=" + trabajoConfig.CredencialAconexId + " en la tabla Credenciales (CredencialAconex).", 0);
                return false;
            }
            if (credBd == null)
            {
                SigmabotSync.Application.Common.Utilities.Wlog("ERROR: No se encontró Credencial Id=" + trabajoConfig.CredencialBDId + " en la tabla Credenciales (CredencialBD).", 0);
                return false;
            }

            return true;
        }
    }
}
