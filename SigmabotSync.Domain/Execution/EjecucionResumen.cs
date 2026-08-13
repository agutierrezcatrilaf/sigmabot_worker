using System.Collections.Generic;

namespace SigmabotSync.Domain.Execution
{
    /// <summary>Resumen JSON guardado en TrabajosEjecucion.DetalleEjecucion al terminar con éxito.</summary>
    public sealed class EjecucionResumen
    {
        public string Tipo { get; set; }
        public int DuracionSeg { get; set; }
        public List<string> Etapas { get; set; }
        public List<ProjectSyncDireccionResumen> Direcciones { get; set; }
        public FileExtractionResumen FileExtraction { get; set; }
        public DocumentExtractionResumen DocumentExtraction { get; set; }
        public FileUploadResumen FileUpload { get; set; }
        public FullExtractionResumen FullExtraction { get; set; }
    }

    public sealed class ProjectSyncDireccionResumen
    {
        public string Origen { get; set; }
        public string Destino { get; set; }
        /// <summary>Transmitals tras filtro subject (solo «Final» en vuelta SALFA→Codelco).</summary>
        public int Mails { get; set; }
        /// <summary>Total listados en Aconex antes del filtro subject (si aplica).</summary>
        public int MailsListados { get; set; }
        public int Procesados { get; set; }
        /// <summary>Transmitals Final ya sincronizados antes (estado en BD).</summary>
        public int OmitidosYaProcesados { get; set; }
        /// <summary>Transmitals listados que no pasan filtro subject (no son «Final»).</summary>
        public int DescartadosSubject { get; set; }
        public int Marcadores { get; set; }
        public int Archivos { get; set; }
        public int Errores { get; set; }
    }

    public sealed class FileExtractionResumen
    {
        public long TotalProcesados { get; set; }
        public int Guardados { get; set; }
        public int Omitidos { get; set; }
        public int OmitidosSinDocumento { get; set; }
        public int OmitidosYaExistian { get; set; }
        public int Errores { get; set; }
    }

    public sealed class DocumentExtractionResumen
    {
        public long DocumentosAconex { get; set; }
        public long DocumentosDescargados { get; set; }
    }

    public sealed class FileUploadResumen
    {
        public int Enviados { get; set; }
        /// <summary>Filas con Procesado=1; no se intentó envío (omitidos por lógica, no por error).</summary>
        public int OmitidosYaProcesados { get; set; }
        public int Marcados { get; set; }
        /// <summary>Intentos de envío fallidos (Aconex/archivo). No son omitidos.</summary>
        public int Errores { get; set; }
    }

    public sealed class FullExtractionResumen
    {
        public long DocumentosAconex { get; set; }
        public long DocumentosDescargados { get; set; }
        public long CorreosRecibidosAconex { get; set; }
        public long CorreosRecibidosProcesados { get; set; }
        public long CorreosRecibidosDescartados { get; set; }
        public long CorreosEnviadosAconex { get; set; }
        public long CorreosEnviadosProcesados { get; set; }
        public long CorreosEnviadosDescartados { get; set; }
        public long FlujosAconex { get; set; }
        public long FlujosDescargados { get; set; }
        public long PasosFlujosAconex { get; set; }
        public long PasosFlujosDescargados { get; set; }
        /// <summary>Errores absorbidos en Documentos (no detienen la ejecución).</summary>
        public int ErroresDocumentos { get; set; }
        /// <summary>Errores absorbidos en Correos.</summary>
        public int ErroresCorreos { get; set; }
        /// <summary>Errores absorbidos en Flujos.</summary>
        public int ErroresFlujos { get; set; }
        /// <summary>Total ErroresDocumentos + ErroresCorreos + ErroresFlujos.</summary>
        public int Errores { get; set; }
    }
}
