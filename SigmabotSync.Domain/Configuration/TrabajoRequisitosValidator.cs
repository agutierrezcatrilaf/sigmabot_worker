using System;
using System.Collections.Generic;
using System.Linq;
using SigmabotSync.Domain.Entities;

namespace SigmabotSync.Domain.Configuration
{
    /// <summary>Validación mínima de la tabla Trabajos para el configurador.</summary>
    public static class TrabajoRequisitosValidator
    {
        public static IReadOnlyList<string> Validar(Trabajo t)
        {
            return Validar(t, codigosTipoActivos: null);
        }

        /// <param name="codigosTipoActivos">
        /// Códigos activos desde tabla TiposTrabajo. Si se indica, reemplaza la lista fija en código.
        /// </param>
        public static IReadOnlyList<string> Validar(Trabajo t, IReadOnlyCollection<string> codigosTipoActivos)
        {
            var errores = new List<string>();
            if (t == null)
            {
                errores.Add("El trabajo no puede ser nulo.");
                return errores;
            }

            if (string.IsNullOrWhiteSpace(t.Nombre))
                errores.Add("Nombre es obligatorio.");

            if (string.IsNullOrWhiteSpace(t.Tipo))
                errores.Add("Tipo es obligatorio.");
            else if (codigosTipoActivos != null && codigosTipoActivos.Count > 0)
            {
                if (!codigosTipoActivos.Any(c => c.Equals(t.Tipo.Trim(), StringComparison.OrdinalIgnoreCase)))
                    errores.Add("Tipo debe ser un código activo del catálogo TiposTrabajo.");
            }
            else if (!EsTipoConocido(t.Tipo))
                errores.Add("Tipo debe ser uno de: " + TipoTrabajoIds.FileExtraction + ", " + TipoTrabajoIds.ProjectSync + ", "
                    + TipoTrabajoIds.FullExtraction + ", " + TipoTrabajoIds.FileUploadWithMetadata + ".");

            if (string.IsNullOrWhiteSpace(t.Estado))
                errores.Add("Estado es obligatorio.");
            else if (!EsEstadoPermitido(t.Estado))
                errores.Add("Estado debe ser " + TrabajoEstadoIds.Activo + ", " + TrabajoEstadoIds.Desactivado + " o " + TrabajoEstadoIds.Pendiente + ".");

            return errores;
        }

        private static bool EsEstadoPermitido(string estado)
        {
            var x = estado.Trim();
            return x.Equals(TrabajoEstadoIds.Activo, StringComparison.OrdinalIgnoreCase)
                || x.Equals(TrabajoEstadoIds.Desactivado, StringComparison.OrdinalIgnoreCase)
                || x.Equals(TrabajoEstadoIds.Pendiente, StringComparison.OrdinalIgnoreCase);
        }

        private static bool EsTipoConocido(string tipo)
        {
            var x = tipo.Trim();
            return x.Equals(TipoTrabajoIds.FileExtraction, StringComparison.OrdinalIgnoreCase)
                || x.Equals(TipoTrabajoIds.ProjectSync, StringComparison.OrdinalIgnoreCase)
                || x.Equals(TipoTrabajoIds.FullExtraction, StringComparison.OrdinalIgnoreCase)
                || x.Equals(TipoTrabajoIds.FileUploadWithMetadata, StringComparison.OrdinalIgnoreCase);
        }
    }
}
