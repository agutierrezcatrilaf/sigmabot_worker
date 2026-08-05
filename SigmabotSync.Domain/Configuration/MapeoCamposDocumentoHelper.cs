using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SigmabotSync.Domain.Configuration
{
    /// <summary>Convierte y valida CamposConsulta / CamposResponse / CamposBD (CSV alineados por índice).</summary>
    public static class MapeoCamposDocumentoHelper
    {
        public static bool TipoUsaMapeoGuiado(string tipoTrabajo)
        {
            return TrabajoTipoConfigFieldCatalog.ObtenerCamposParaFormulario(tipoTrabajo ?? string.Empty)
                .Any(d => EsClaveMapeo(d.Clave));
        }

        public static bool EsClaveMapeo(string clave)
        {
            if (string.IsNullOrWhiteSpace(clave))
                return false;
            var k = clave.Trim();
            return k.Equals(TrabajosConfiguracionKeyNames.CamposConsulta, StringComparison.OrdinalIgnoreCase)
                || k.Equals(TrabajosConfiguracionKeyNames.CamposResponse, StringComparison.OrdinalIgnoreCase)
                || k.Equals(TrabajosConfiguracionKeyNames.CamposBD, StringComparison.OrdinalIgnoreCase);
        }

        public static List<MapeoCampoFila> FilasDesdeValores(IDictionary<string, string> valorPorNombre)
        {
            if (valorPorNombre == null)
                return new List<MapeoCampoFila>();

            valorPorNombre.TryGetValue(TrabajosConfiguracionKeyNames.CamposConsulta, out var consulta);
            valorPorNombre.TryGetValue(TrabajosConfiguracionKeyNames.CamposResponse, out var response);
            valorPorNombre.TryGetValue(TrabajosConfiguracionKeyNames.CamposBD, out var bd);

            return FilasDesdeCsv(consulta, response, bd);
        }

        public static List<MapeoCampoFila> FilasDesdeCsv(string consulta, string response, string bd)
        {
            var apis = ParseCsv(consulta);
            var jsons = ParseCsv(response);
            var bds = ParseCsv(bd);
            int n = apis.Count;
            if (jsons.Count > n) n = jsons.Count;
            if (bds.Count > n) n = bds.Count;

            var filas = new List<MapeoCampoFila>(n);
            for (int i = 0; i < n; i++)
            {
                filas.Add(new MapeoCampoFila
                {
                    Api = i < apis.Count ? apis[i] : string.Empty,
                    Json = i < jsons.Count ? jsons[i] : string.Empty,
                    Bd = i < bds.Count ? bds[i] : string.Empty
                });
            }
            return filas;
        }

        public static void AplicarFilasAValores(IReadOnlyList<MapeoCampoFila> filas, IDictionary<string, string> valorPorNombre)
        {
            if (valorPorNombre == null)
                return;

            var lista = filas ?? Array.Empty<MapeoCampoFila>();
            valorPorNombre[TrabajosConfiguracionKeyNames.CamposConsulta] = ToCsv(lista.Select(f => f?.Api ?? string.Empty));
            valorPorNombre[TrabajosConfiguracionKeyNames.CamposResponse] = ToCsv(lista.Select(f => f?.Json ?? string.Empty));
            valorPorNombre[TrabajosConfiguracionKeyNames.CamposBD] = ToCsv(lista.Select(f => f?.Bd ?? string.Empty));
        }

        public static IReadOnlyList<string> ValidarFilas(IReadOnlyList<MapeoCampoFila> filas, bool obligatorio)
        {
            var errores = new List<string>();
            var lista = filas ?? Array.Empty<MapeoCampoFila>();

            if (obligatorio && lista.Count == 0)
            {
                errores.Add("El mapeo de campos debe tener al menos una fila.");
                return errores;
            }

            for (int i = 0; i < lista.Count; i++)
            {
                var f = lista[i];
                var n = i + 1;
                var api = (f?.Api ?? string.Empty).Trim();
                var json = (f?.Json ?? string.Empty).Trim();
                var bd = (f?.Bd ?? string.Empty).Trim();

                if (string.IsNullOrEmpty(api) && string.IsNullOrEmpty(json) && string.IsNullOrEmpty(bd))
                {
                    errores.Add("Fila " + n + ": está vacía; elimínela o complete al menos un valor.");
                    continue;
                }
                if (string.IsNullOrEmpty(api))
                    errores.Add("Fila " + n + ": falta Campo API (consulta).");
                if (string.IsNullOrEmpty(json))
                    errores.Add("Fila " + n + ": falta Propiedad JSON (respuesta).");
                if (string.IsNullOrEmpty(bd))
                    errores.Add("Fila " + n + ": falta Columna BD.");
            }

            var apis = lista.Select(f => (f?.Api ?? string.Empty).Trim()).Where(s => s.Length > 0).ToList();
            var dupApi = apis.GroupBy(s => s, StringComparer.OrdinalIgnoreCase).FirstOrDefault(g => g.Count() > 1);
            if (dupApi != null)
                errores.Add("Campo API duplicado: \"" + dupApi.Key + "\".");

            var bds = lista.Select(f => (f?.Bd ?? string.Empty).Trim()).Where(s => s.Length > 0).ToList();
            var dupBd = bds.GroupBy(s => s, StringComparer.OrdinalIgnoreCase).FirstOrDefault(g => g.Count() > 1);
            if (dupBd != null)
                errores.Add("Columna BD duplicada: \"" + dupBd.Key + "\".");

            return errores;
        }

        private static List<string> ParseCsv(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return new List<string>();
            return raw.Split(',')
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .ToList();
        }

        private static string ToCsv(IEnumerable<string> items)
        {
            var list = items?
                .Select(s => (s ?? string.Empty).Trim())
                .Where(s => s.Length > 0)
                .ToList() ?? new List<string>();
            if (list.Count == 0)
                return string.Empty;
            return string.Join(",", list);
        }
    }
}
