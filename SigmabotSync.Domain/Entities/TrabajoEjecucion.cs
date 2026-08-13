using System;

namespace SigmabotSync.Domain.Entities
{
    /// <summary>
    /// Registro de historial de una ejecución de un trabajo (tabla TrabajosEjecucion).
    /// Un insert por cada ejecución con detalle, error y etapas ejecutadas.
    /// </summary>
    public class TrabajoEjecucion
    {
        public int Id { get; set; }
        public int IdTrabajo { get; set; }
        public DateTime FechaHoraInicio { get; set; }
        /// <summary>NULL mientras la ejecución está en curso; se actualiza al finalizar.</summary>
        public DateTime? FechaHoraFin { get; set; }
        public bool Exito { get; set; }
        public string MensajeError { get; set; }
        /// <summary>Etapas ejecutadas, separadas por coma (ej. "FileExtraction,DocumentExtraction").</summary>
        public string EtapasEjecutadas { get; set; }
        public string DetalleEjecucion { get; set; }
        /// <summary>Origen de la ejecución: "Manual" o "Scheduler".</summary>
        public string TipoEjecucion { get; set; }
        /// <summary>Ruta del archivo job-{idTrabajo}-ejec-{id}.log de esta ejecución.</summary>
        public string RutaLog { get; set; }
    }
}
