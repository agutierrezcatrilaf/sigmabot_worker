using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SigmabotSync.Domain.Models.Extraction
{
    public class Rootobject
    {
        [JsonProperty("searchResults")] public List<Searchresult> searchResults { get; set; }
        [JsonProperty("totalResultsCount")] public int totalResultsCount { get; set; }
        [JsonProperty("totalResultsOnCurrentPage")] public int totalResultsOnCurrentPage { get; set; }
        [JsonProperty("totalNumberOfPages")] public int totalNumberOfPages { get; set; }
        [JsonProperty("currentPageNumber")] public int currentPageNumber { get; set; }
        [JsonProperty("singlePageSize")] public int singlePageSize { get; set; }
        /// <summary>Campos dinámicos en la raíz del JSON.</summary>
        [JsonExtensionData]
        public IDictionary<string, JToken> ExtensionData { get; set; }
    }

    /// <summary>Resultado de búsqueda de documento. Solo campos por defecto (documentNumber, title, revision); el resto va en projectFields o ExtensionData.</summary>
    public class Searchresult
    {
        [JsonProperty("id")] public long Id { get; set; }
        [JsonProperty("documentNumber")] public string DocumentNumber { get; set; }
        [JsonProperty("title")] public string Title { get; set; }
        [JsonProperty("revision")] public string Revision { get; set; }
        [JsonProperty("trackingid")] public long TrackingId { get; set; }
        /// <summary>Campos custom del proyecto (ej. Cma_singleSelect, Discipline_singleSelect). Cada item tiene name y value.</summary>
        [JsonProperty("projectFields")] public List<ProjectFieldItem> ProjectFields { get; set; }
        /// <summary>Propiedades dinámicas que vienen en el JSON y no tienen propiedad en esta clase. Permite deserializar campos extra sin fallar.</summary>
        [JsonExtensionData]
        public IDictionary<string, JToken> ExtensionData { get; set; }

        /// <summary>Obtiene un valor dinámico por clave (p. ej. "filename", "versionNumber"). Busca en ExtensionData.</summary>
        public string GetDynamicValue(string key)
        {
            if (string.IsNullOrEmpty(key) || ExtensionData == null) return null;
            JToken token;
            if (ExtensionData.TryGetValue(key, out token) && token != null)
            {
                if (token is JValue jv) return jv.Value?.ToString();
                return token.ToString();
            }
            return null;
        }
    }

    /// <summary>Campo custom del proyecto Aconex (name = nombre del campo, value = valor).</summary>
    public class ProjectFieldItem
    {
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("value")] public string Value { get; set; }
        [JsonExtensionData]
        public IDictionary<string, JToken> ExtensionData { get; set; }
    }
}
