using System;

namespace SigmabotSync.Domain.Entities
{
    /// <summary>
    /// Programación de ejecución de un trabajo: día de la semana y hora.
    /// Tabla TrabajosProgramacion, asociada a Trabajos.
    /// </summary>
    public class TrabajoProgramacion
    {
        public int Id { get; set; }
        public int IdTrabajo { get; set; }
        /// <summary>0=Domingo, 1=Lunes, ..., 6=Sábado (DayOfWeek).</summary>
        public int DiaSemana { get; set; }
        /// <summary>Hora programada (solo hora:minuto:segundo).</summary>
        public TimeSpan Hora { get; set; }
        public bool Activo { get; set; }
    }
}
