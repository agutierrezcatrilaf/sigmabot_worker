using System;

namespace SigmabotSync.Domain.Entities
{
    /// <summary>
    /// Registro de la tabla Trabajos. Definición (Nombre, Tipo, Estado) y resumen de la última ejecución
    /// (rellenado por la consola). La programación horaria está en TrabajosProgramacion.
    /// </summary>
    public class Trabajo
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Tipo { get; set; }
        public string Estado { get; set; }
        /// <summary>Fecha y hora de la última ejecución (datetime).</summary>
        public DateTime? FechaUltimaEjecucion { get; set; }
        /// <summary>Resultado de la última ejecución: "Exitoso", "Error", etc.</summary>
        public string ResultadoUltimaEjecucion { get; set; }
        /// <summary>Detalle del error de la última ejecución (null si fue exitosa).</summary>
        public string UltCorrEjecucion { get; set; }
    }
}
