namespace SigmabotSync.Domain.Entities
{
    /// <summary>
    /// Registro de la tabla TiposTrabajo. <see cref="Codigo"/> es el valor persistido en <c>Trabajos.Tipo</c>.
    /// </summary>
    public class TipoTrabajo
    {
        public string Codigo { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public int Orden { get; set; }
        public bool Activo { get; set; }
    }
}
