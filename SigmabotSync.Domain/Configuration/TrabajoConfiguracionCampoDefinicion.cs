using System;
using System.Collections.Generic;
using System.Linq;

namespace SigmabotSync.Domain.Configuration
{
    /// <summary>
    /// Describe un parámetro almacenado en <c>TrabajosConfiguracion</c> para el configurador y validaciones.
    /// </summary>
    public sealed class TrabajoConfiguracionCampoDefinicion
    {
        private readonly HashSet<string> _tiposVisibles;
        private readonly HashSet<string> _tiposObligatorios;

        /// <param name="tiposDondeVisible">Valores de <c>Trabajos.Tipo</c> en los que el campo se muestra en el formulario.</param>
        /// <param name="tiposDondeObligatorio">Subconjunto de esos tipos donde el valor no puede quedar vacío.</param>
        public TrabajoConfiguracionCampoDefinicion(
            string clave,
            string etiqueta,
            IEnumerable<string> tiposDondeVisible,
            IEnumerable<string> tiposDondeObligatorio,
            string ayuda = null)
        {
            Clave = clave ?? throw new ArgumentNullException(nameof(clave));
            Etiqueta = etiqueta ?? throw new ArgumentNullException(nameof(etiqueta));
            Ayuda = ayuda;
            _tiposVisibles = new HashSet<string>(
                (tiposDondeVisible ?? Array.Empty<string>()).Select(NormalizarTipo),
                StringComparer.OrdinalIgnoreCase);
            _tiposObligatorios = new HashSet<string>(
                (tiposDondeObligatorio ?? Array.Empty<string>()).Select(NormalizarTipo),
                StringComparer.OrdinalIgnoreCase);
        }

        private static string NormalizarTipo(string t) => (t ?? string.Empty).Trim();

        /// <summary>Nombre de fila en TrabajosConfiguracion (columna Nombre).</summary>
        public string Clave { get; }

        public string Etiqueta { get; }

        public string Ayuda { get; }

        public bool EsVisiblePara(string tipoTrabajo)
        {
            var t = NormalizarTipo(tipoTrabajo);
            return t.Length > 0 && _tiposVisibles.Contains(t);
        }

        public bool EsObligatorioPara(string tipoTrabajo)
        {
            var t = NormalizarTipo(tipoTrabajo);
            return t.Length > 0 && _tiposObligatorios.Contains(t);
        }
    }
}
