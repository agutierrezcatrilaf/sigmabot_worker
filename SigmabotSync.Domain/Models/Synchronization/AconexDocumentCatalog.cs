using System;
using System.Collections.Generic;

namespace SigmabotSync.Domain.Models.Synchronization
{
    /// <summary>Catálogos TiposDocumentos / EstatusDocumentos + equivalencias project fields.</summary>
    public sealed class AconexDocumentCatalog
    {
        private static readonly IReadOnlyDictionary<string, string> EmptyMap =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public static AconexDocumentCatalog Empty { get; } = new AconexDocumentCatalog(
            EmptyMap, EmptyMap, EmptyMap, EmptyMap, EmptyMap);

        public AconexDocumentCatalog(
            IReadOnlyDictionary<string, string> idTipoPorNombre,
            IReadOnlyDictionary<string, string> idEstatusPorNombre,
            IReadOnlyDictionary<string, string> equivalenciaDiscipline = null,
            IReadOnlyDictionary<string, string> equivalenciaTipoDocumento = null,
            IReadOnlyDictionary<string, string> equivalenciaCwa = null)
        {
            IdTipoPorNombre = idTipoPorNombre ?? EmptyMap;
            IdEstatusPorNombre = idEstatusPorNombre ?? EmptyMap;
            EquivalenciaDiscipline = equivalenciaDiscipline ?? EmptyMap;
            EquivalenciaTipoDocumento = equivalenciaTipoDocumento ?? EmptyMap;
            EquivalenciaCwa = equivalenciaCwa ?? EmptyMap;
        }

        public IReadOnlyDictionary<string, string> IdTipoPorNombre { get; }
        public IReadOnlyDictionary<string, string> IdEstatusPorNombre { get; }
        /// <summary>ValorOrigen → ValorDestino (texto picklist SALFA).</summary>
        public IReadOnlyDictionary<string, string> EquivalenciaDiscipline { get; }
        /// <summary>ValorOrigen → ValorDestino (texto picklist SALFA).</summary>
        public IReadOnlyDictionary<string, string> EquivalenciaTipoDocumento { get; }
        /// <summary>Localizador/WBS Codelco (ValorOrigen) → CWA SALFA (ValorDestino).</summary>
        public IReadOnlyDictionary<string, string> EquivalenciaCwa { get; }

        public string ResolveByCatalog(string catalogo, string nameOrId)
        {
            if (string.IsNullOrWhiteSpace(catalogo) || string.IsNullOrWhiteSpace(nameOrId))
                return null;

            string trimmed = nameOrId.Trim();
            string table = catalogo.Trim();

            if (string.Equals(table, AconexDocumentCatalogNames.EquivalenciaDiscipline, StringComparison.OrdinalIgnoreCase))
                return ResolveByName(EquivalenciaDiscipline, trimmed);
            if (string.Equals(table, AconexDocumentCatalogNames.EquivalenciaTipoDocumento, StringComparison.OrdinalIgnoreCase))
                return ResolveEquivalenciaTipoDocumento(EquivalenciaTipoDocumento, trimmed);
            if (string.Equals(table, AconexDocumentCatalogNames.EquivalenciaCwa, StringComparison.OrdinalIgnoreCase))
                return ResolveByName(EquivalenciaCwa, trimmed) ?? ResolveEquivalenciaCwaByWbsCode(trimmed);

            if (LooksLikeAconexId(trimmed))
                return trimmed;

            if (string.Equals(table, AconexDocumentCatalogNames.EstatusDocumentos, StringComparison.OrdinalIgnoreCase))
                return ResolveByName(IdEstatusPorNombre, trimmed);
            if (string.Equals(table, AconexDocumentCatalogNames.TiposDocumentos, StringComparison.OrdinalIgnoreCase))
                return ResolveByName(IdTipoPorNombre, trimmed);

            return null;
        }

        private static string ResolveByName(IReadOnlyDictionary<string, string> map, string name)
        {
            if (map == null || string.IsNullOrWhiteSpace(name))
                return null;

            if (map.TryGetValue(name.Trim(), out string id) && !string.IsNullOrWhiteSpace(id))
                return id.Trim();

            foreach (var kv in map)
            {
                if (string.Equals(kv.Key, name, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(kv.Value))
                    return kv.Value.Trim();
            }

            return null;
        }

        /// <summary>
        /// Equivalencia tipo documento: primero match exacto (legacy); si no, prefijo por código al inicio del texto Aconex.
        /// Los códigos en homologación son únicos (sin solapamiento); gana el prefijo más largo.
        /// </summary>
        private static string ResolveEquivalenciaTipoDocumento(IReadOnlyDictionary<string, string> map, string sourceValue)
        {
            return ResolveEquivalenciaTipoDocumentoPrefixMap(map, sourceValue);
        }

        private static string ResolveEquivalenciaTipoDocumentoPrefixMap(
            IReadOnlyDictionary<string, string> map,
            string sourceValue)
        {
            if (map == null || string.IsNullOrWhiteSpace(sourceValue))
                return null;

            string exact = ResolveByName(map, sourceValue);
            if (!string.IsNullOrWhiteSpace(exact))
                return exact;

            string trimmedSource = sourceValue.Trim();
            string bestKey = null;
            string bestValue = null;
            foreach (var kv in map)
            {
                if (string.IsNullOrWhiteSpace(kv.Key) || string.IsNullOrWhiteSpace(kv.Value))
                    continue;

                string key = kv.Key.Trim();
                if (!trimmedSource.StartsWith(key, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (bestKey == null || key.Length > bestKey.Length)
                {
                    bestKey = key;
                    bestValue = kv.Value.Trim();
                }
            }

            return bestValue;
        }

        public string ResolveEquivalenciaCwaByWbsCode(string wbsCode)
        {
            return ResolveEquivalenciaCwaByWbsCode(EquivalenciaCwa, wbsCode);
        }

        private static string ResolveEquivalenciaCwaByWbsCode(IReadOnlyDictionary<string, string> map, string wbsCode)
        {
            if (map == null || string.IsNullOrWhiteSpace(wbsCode))
                return null;

            string code = wbsCode.Trim();
            string bestKey = null;
            string bestValue = null;
            foreach (var kv in map)
            {
                if (string.IsNullOrWhiteSpace(kv.Key) || string.IsNullOrWhiteSpace(kv.Value))
                    continue;

                string key = kv.Key.Trim();
                if (!key.StartsWith(code, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (bestKey == null || key.Length > bestKey.Length)
                {
                    bestKey = key;
                    bestValue = kv.Value.Trim();
                }
            }

            return bestValue;
        }

        private static bool LooksLikeAconexId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;
            foreach (char c in value)
            {
                if (!char.IsDigit(c))
                    return false;
            }
            return value.Length >= 8;
        }
    }
}
