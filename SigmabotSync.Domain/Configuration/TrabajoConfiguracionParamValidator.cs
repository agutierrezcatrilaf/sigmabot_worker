using System;
using System.Collections.Generic;
using System.Linq;

namespace SigmabotSync.Domain.Configuration
{
    /// <summary>
    /// Comprueba que existan valores no vacíos para las claves obligatorias de <c>TrabajosConfiguracion</c> según <c>Trabajos.Tipo</c>.
    /// </summary>
    public static class TrabajoConfiguracionParamValidator
    {
        /// <param name="tipoTrabajo">Valor de <c>Trabajos.Tipo</c>.</param>
        /// <param name="valorPorNombre">Clave Nombre (trim) → ValorTexto.</param>
        public static IReadOnlyList<string> ValidarObligatoriosPorTipo(string tipoTrabajo, IDictionary<string, string> valorPorNombre)
        {
            var errores = new List<string>();
            if (valorPorNombre == null)
            {
                errores.Add("No hay parámetros cargados.");
                return errores;
            }

            foreach (var def in TrabajoTipoConfigFieldCatalog.ObtenerObligatoriosPara(tipoTrabajo ?? string.Empty))
            {
                if (MapeoCamposDocumentoHelper.EsClaveMapeo(def.Clave))
                    continue;
                if (!valorPorNombre.TryGetValue(def.Clave.Trim(), out var v) || string.IsNullOrWhiteSpace(v))
                    errores.Add("Falta o está vacío: " + def.Etiqueta + " (" + def.Clave + ").");
            }

            if (string.Equals(tipoTrabajo?.Trim(), TipoTrabajoIds.FileUploadWithMetadata, StringComparison.OrdinalIgnoreCase))
            {
                valorPorNombre.TryGetValue(TrabajosConfiguracionKeyNames.TablaMetadata, out var meta);
                valorPorNombre.TryGetValue(TrabajosConfiguracionKeyNames.TablaPaths, out var paths);
                string metaR = FileUploadWithMetadataDefaults.ResolverTablaMetadata(meta);
                string pathsR = FileUploadWithMetadataDefaults.ResolverTablaPaths(paths);
                if (!string.Equals(metaR, FileUploadWithMetadataDefaults.TablaMetadata, StringComparison.OrdinalIgnoreCase))
                    errores.Add("Tabla metadata debe ser " + FileUploadWithMetadataDefaults.TablaMetadata + ".");
                if (!string.Equals(pathsR, FileUploadWithMetadataDefaults.TablaPaths, StringComparison.OrdinalIgnoreCase))
                    errores.Add("Tabla rutas debe ser " + FileUploadWithMetadataDefaults.TablaPaths + ".");
            }

            if (MapeoCamposDocumentoHelper.TipoUsaMapeoGuiado(tipoTrabajo ?? string.Empty))
            {
                var filas = MapeoCamposDocumentoHelper.FilasDesdeValores(valorPorNombre);
                var mapeoObligatorio = TrabajoTipoConfigFieldCatalog.ObtenerObligatoriosPara(tipoTrabajo ?? string.Empty)
                    .Any(d => MapeoCamposDocumentoHelper.EsClaveMapeo(d.Clave));
                foreach (var msg in MapeoCamposDocumentoHelper.ValidarFilas(filas, mapeoObligatorio))
                    errores.Add(msg);
            }

            return errores;
        }
    }
}
