using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SigmabotSync.Domain.Models.Extraction
{
    public class MemProject
    {
        public string ProjectId { get; set; }
        public string Activo { get; set; }
        public string Nombre { get; set; }
        public string FechaInicial { get; set; }
    }

    public class MemProyFlujo
    {
        public string Numero { get; set; }
        public string Nombre { get; set; }
        public string Estado { get; set; }
        public string Plantilla { get; set; }
        public string NombreEmisor { get; set; }
        public string ACXIdEmisor { get; set; }
        public string OrganizacionEmisora { get; set; }
        public string MotivodeEmision { get; set; }
    }

    public class MemProyPaso
    {
        public string IdFlujodeTrabajo { get; set; }
        public string NumeroFlujodeTrabajo { get; set; }
        public string Nombre { get; set; }
        public string Estado { get; set; }
        public string Resultado { get; set; }
        public string Comentarios { get; set; }
        public DateTime FechaFinalizacion { get; set; }
        public DateTime FechaLimite { get; set; }
        public DateTime FechaInicio { get; set; }
        public int Duracion { get; set; }
        public DateTime FechaLimiteOriginal { get; set; }
        public int DiasAtraso { get; set; }
        public string RevisorOrganizacion { get; set; }
        public string RevisorNombre { get; set; }
        public string RevisorACXId { get; set; }
    }
}
