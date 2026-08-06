using System;
using System.Collections.Generic;
using System.Linq;

namespace SigmabotSync.Domain.Configuration
{
    /// <summary>
    /// Catálogo de campos de <c>TrabajosConfiguracion</c> por tipo de trabajo.
    /// </summary>
    public static class TrabajoTipoConfigFieldCatalog
    {
        private static readonly IReadOnlyList<TrabajoConfiguracionCampoDefinicion> Campos = new[]
        {
            new TrabajoConfiguracionCampoDefinicion(
                TrabajosConfiguracionKeyNames.IdProyecto,
                "Id proyecto Aconex (lado 1)",
                tiposDondeVisible: new[] { TipoTrabajoIds.FileExtraction, TipoTrabajoIds.FullExtraction, TipoTrabajoIds.FileUploadWithMetadata, TipoTrabajoIds.ProjectSync },
                tiposDondeObligatorio: new[] { TipoTrabajoIds.FileExtraction, TipoTrabajoIds.FullExtraction, TipoTrabajoIds.FileUploadWithMetadata, TipoTrabajoIds.ProjectSync },
                ayuda: "ProjectId en Aconex (lado 1). En ProjectSync se lee su sentbox y se sincroniza en el lado 2."),

            new TrabajoConfiguracionCampoDefinicion(
                TrabajosConfiguracionKeyNames.IdProyecto2,
                "Id proyecto Aconex (lado 2)",
                tiposDondeVisible: new[] { TipoTrabajoIds.ProjectSync },
                tiposDondeObligatorio: Array.Empty<string>(),
                ayuda: "Segundo proyecto del par. Se lee su sentbox y se sincroniza en el lado 1 (y viceversa)."),

            new TrabajoConfiguracionCampoDefinicion(
                TrabajosConfiguracionKeyNames.Proyecto,
                "Nombre proyecto 1 (logs)",
                tiposDondeVisible: new[] { TipoTrabajoIds.FileExtraction, TipoTrabajoIds.FullExtraction, TipoTrabajoIds.FileUploadWithMetadata, TipoTrabajoIds.ProjectSync },
                tiposDondeObligatorio: Array.Empty<string>(),
                ayuda: "Opcional; mejora logs."),

            new TrabajoConfiguracionCampoDefinicion(
                TrabajosConfiguracionKeyNames.Proyecto2,
                "Nombre proyecto 2 (logs)",
                tiposDondeVisible: new[] { TipoTrabajoIds.ProjectSync },
                tiposDondeObligatorio: Array.Empty<string>(),
                ayuda: "Etiqueta opcional para el segundo proyecto del par."),

            new TrabajoConfiguracionCampoDefinicion(
                TrabajosConfiguracionKeyNames.DiasLookbackTransmittal,
                "Días lookback transmitals",
                tiposDondeVisible: new[] { TipoTrabajoIds.ProjectSync },
                tiposDondeObligatorio: Array.Empty<string>(),
                ayuda: "Días hacia atrás para buscar transmitals en inbox del origen. Default 30."),

            new TrabajoConfiguracionCampoDefinicion(
                TrabajosConfiguracionKeyNames.IdEstatusDocumentoDestino,
                "Estatus documento destino (idEstatus, Codelco)",
                tiposDondeVisible: new[] { TipoTrabajoIds.ProjectSync },
                tiposDondeObligatorio: Array.Empty<string>(),
                ayuda: "idEstatus al supersede en Codelco (ej. 1207959768). En matriz destino Codelco, fila statusid: Tipo fuente = " +
                        ProjectSyncCampoDestinoTipoFuente.ParametroIdEstatusDestino + "."),

            new TrabajoConfiguracionCampoDefinicion(
                TrabajosConfiguracionKeyNames.SubjectFiltroTransmittalVuelta,
                "Filtro subject transmittal (vuelta)",
                tiposDondeVisible: new[] { TipoTrabajoIds.ProjectSync },
                tiposDondeObligatorio: Array.Empty<string>(),
                ayuda: "Solo en vuelta SALFA→Codelco (sentbox lado 2). Procesa transmitals cuyo Subject contenga este texto (ej. Final). Vacío = sin filtro."),

            new TrabajoConfiguracionCampoDefinicion(
                TrabajosConfiguracionKeyNames.CredencialAconex,
                "Id credencial Aconex",
                tiposDondeVisible: new[] { TipoTrabajoIds.FileExtraction, TipoTrabajoIds.FullExtraction, TipoTrabajoIds.FileUploadWithMetadata, TipoTrabajoIds.ProjectSync },
                tiposDondeObligatorio: new[] { TipoTrabajoIds.FileExtraction, TipoTrabajoIds.FullExtraction, TipoTrabajoIds.FileUploadWithMetadata, TipoTrabajoIds.ProjectSync },
                ayuda: "Id en tabla Credenciales con Tipo = Aconex."),

            new TrabajoConfiguracionCampoDefinicion(
                TrabajosConfiguracionKeyNames.CredencialBD,
                "Id credencial BD",
                tiposDondeVisible: new[] { TipoTrabajoIds.FileExtraction, TipoTrabajoIds.FullExtraction, TipoTrabajoIds.FileUploadWithMetadata, TipoTrabajoIds.ProjectSync },
                tiposDondeObligatorio: new[] { TipoTrabajoIds.FileExtraction, TipoTrabajoIds.FullExtraction, TipoTrabajoIds.FileUploadWithMetadata, TipoTrabajoIds.ProjectSync },
                ayuda: "Id en tabla Credenciales con Tipo = BD (estado de sync y mapeos)."),

            new TrabajoConfiguracionCampoDefinicion(
                TrabajosConfiguracionKeyNames.CamposConsulta,
                "Campos consulta API (CSV)",
                tiposDondeVisible: new[] { TipoTrabajoIds.FileExtraction, TipoTrabajoIds.FullExtraction },
                tiposDondeObligatorio: new[] { TipoTrabajoIds.FileExtraction, TipoTrabajoIds.FullExtraction },
                ayuda: "Lista separada por comas; mismo orden que CamposResponse y CamposBD."),

            new TrabajoConfiguracionCampoDefinicion(
                TrabajosConfiguracionKeyNames.CamposResponse,
                "Campos respuesta JSON (CSV)",
                tiposDondeVisible: new[] { TipoTrabajoIds.FileExtraction, TipoTrabajoIds.FullExtraction },
                tiposDondeObligatorio: new[] { TipoTrabajoIds.FileExtraction, TipoTrabajoIds.FullExtraction },
                ayuda: "Propiedades JSON alineadas por índice con CamposConsulta."),

            new TrabajoConfiguracionCampoDefinicion(
                TrabajosConfiguracionKeyNames.CamposBD,
                "Campos columnas BD (CSV)",
                tiposDondeVisible: new[] { TipoTrabajoIds.FileExtraction, TipoTrabajoIds.FullExtraction },
                tiposDondeObligatorio: new[] { TipoTrabajoIds.FileExtraction, TipoTrabajoIds.FullExtraction },
                ayuda: "Columnas en BD alineadas por índice con CamposConsulta."),

            new TrabajoConfiguracionCampoDefinicion(
                TrabajosConfiguracionKeyNames.BasePath,
                "Ruta base archivos",
                tiposDondeVisible: new[] { TipoTrabajoIds.FileExtraction },
                tiposDondeObligatorio: new[] { TipoTrabajoIds.FileExtraction },
                ayuda: "UNC o carpeta local donde FileExtraction descarga los archivos."),

            new TrabajoConfiguracionCampoDefinicion(
                TrabajosConfiguracionKeyNames.TablaMetadata,
                "Tabla metadata (BD)",
                tiposDondeVisible: new[] { TipoTrabajoIds.FileUploadWithMetadata },
                tiposDondeObligatorio: new[] { TipoTrabajoIds.FileUploadWithMetadata },
                ayuda: "Tabla de metadata en la credencial BD. Estándar: " + FileUploadWithMetadataDefaults.TablaMetadata + "."),

            new TrabajoConfiguracionCampoDefinicion(
                TrabajosConfiguracionKeyNames.TablaPaths,
                "Tabla rutas archivos (BD)",
                tiposDondeVisible: new[] { TipoTrabajoIds.FileUploadWithMetadata },
                tiposDondeObligatorio: new[] { TipoTrabajoIds.FileUploadWithMetadata },
                ayuda: "Tabla con PathFisico enlazada por DocumentoId. Estándar: " + FileUploadWithMetadataDefaults.TablaPaths + "."),
        };

        public static IReadOnlyList<TrabajoConfiguracionCampoDefinicion> ObtenerTodos()
        {
            return Campos;
        }

        public static IEnumerable<TrabajoConfiguracionCampoDefinicion> ObtenerObligatoriosPara(string tipoTrabajo)
        {
            foreach (var c in Campos)
            {
                if (c.EsObligatorioPara(tipoTrabajo))
                    yield return c;
            }
        }

        /// <summary>Campos que debe mostrar el formulario guiado para el tipo de trabajo indicado.</summary>
        public static IEnumerable<TrabajoConfiguracionCampoDefinicion> ObtenerCamposParaFormulario(string tipoTrabajo)
        {
            foreach (var c in Campos)
            {
                if (c.EsVisiblePara(tipoTrabajo))
                    yield return c;
            }
        }

        /// <summary>Indica si existe al menos un campo de catálogo para ese tipo (p. ej. ProjectSync no tiene plantilla).</summary>
        public static bool TipoSoportaFormularioGuiado(string tipoTrabajo)
        {
            return ObtenerCamposParaFormulario(tipoTrabajo ?? string.Empty).Any();
        }
    }
}
