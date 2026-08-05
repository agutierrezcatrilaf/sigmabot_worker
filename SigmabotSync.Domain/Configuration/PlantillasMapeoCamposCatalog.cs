using System;
using System.Collections.Generic;
using System.Linq;

namespace SigmabotSync.Domain.Configuration
{
    /// <summary>Plantillas predefinidas solo para CamposConsulta / CamposResponse / CamposBD.</summary>
    public static class PlantillasMapeoCamposCatalog
    {
        private static readonly IReadOnlyList<PlantillaMapeoCampos> Plantillas = CrearPlantillas();

        public static IReadOnlyList<PlantillaMapeoResumen> ListarParaTipo(string tipoTrabajo)
        {
            var tipo = (tipoTrabajo ?? string.Empty).Trim();
            return Plantillas
                .Where(p => p.AplicaATipo(tipo))
                .Select(p => new PlantillaMapeoResumen { Id = p.Id, Nombre = p.Nombre })
                .ToList();
        }

        public static PlantillaMapeoCampos Obtener(string plantillaId, string tipoTrabajo)
        {
            var id = (plantillaId ?? string.Empty).Trim();
            var tipo = (tipoTrabajo ?? string.Empty).Trim();
            var p = Plantillas.FirstOrDefault(x =>
                x.Id.Equals(id, StringComparison.OrdinalIgnoreCase) && x.AplicaATipo(tipo));
            if (p == null)
                throw new InvalidOperationException("Plantilla no encontrada para el tipo de trabajo indicado.");
            return p;
        }

        private static IReadOnlyList<PlantillaMapeoCampos> CrearPlantillas()
        {
            // 28 campos (incl. current → iscurrent en BD), alineado con mapeo Salfa en producción
            var estandarConsulta =
                "docno,revision,title,doctype,confidential,revisiondate,registered,milestonedate,plannedsubmissiondate,author,reviewstatus,reviewsource,comments,versionnumber,statusid,current,Cma_singleSelect,Cwa_singleSelect,Cwp_singleSelect,Description_multiLineText,Discipline_singleSelect,Ewp_singleSelect,EstatusBim_singleSelect,NDeDocumento2_singleLineText,NDeDocumento3_singleLineText,Pwp_singleSelect,Proceso_singleSelect,TipoDeDocumento_singleSelect";
            var estandarResponse =
                "documentNumber,revision,title,documentType,confidential,revisionDate,dateModified,milestoneDate,plannedSubmissionDate,author,reviewStatus,reviewSource,comments,versionNumber,documentStatus,current,Cma_singleSelect,Cwa_singleSelect,Cwp_singleSelect,Description_multiLineText,Discipline_singleSelect,Ewp_singleSelect,EstatusBim_singleSelect,NDeDocumento2_singleLineText,NDeDocumento3_singleLineText,Pwp_singleSelect,Proceso_singleSelect,TipoDeDocumento_singleSelect";
            var estandarBd =
                "docno,revision,title,doctype,confidential,revisiondate,registered,milestonedate,plannedsubmissiondate,author,reviewstatus,reviewsource,comments,versionnumber,statusid,iscurrent,Cma_singleSelect,Cwa_singleSelect,Cwp_singleSelect,Description_multiLineText,Discipline_singleSelect,Ewp_singleSelect,EstatusBim_singleSelect,NDeDocumento2_singleLineText,NDeDocumento3_singleLineText,Pwp_singleSelect,Proceso_singleSelect,TipoDeDocumento_singleSelect";
            var estandarFilas = MapeoCamposDocumentoHelper.FilasDesdeCsv(estandarConsulta, estandarResponse, estandarBd);

            var minimaFilas = MapeoCamposDocumentoHelper.FilasDesdeCsv(
                "docno,revision,title",
                "documentNumber,revision,title",
                "docno,revision,title");

            var tiposExtraccion = new[]
            {
                TipoTrabajoIds.FileExtraction,
                TipoTrabajoIds.FullExtraction
            };

            return new[]
            {
                new PlantillaMapeoCampos(
                    "estandar",
                    "Extracción — estándar Salfa",
                    tiposExtraccion,
                    estandarFilas),
                new PlantillaMapeoCampos(
                    "minima",
                    "Extracción — mínima (prueba)",
                    tiposExtraccion,
                    minimaFilas)
            };
        }
    }

    public sealed class PlantillaMapeoResumen
    {
        public string Id { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
    }

    public sealed class PlantillaMapeoCampos
    {
        public PlantillaMapeoCampos(string id, string nombre, string[] tiposTrabajo, IReadOnlyList<MapeoCampoFila> filas)
        {
            Id = id;
            Nombre = nombre;
            TiposTrabajo = tiposTrabajo ?? Array.Empty<string>();
            Filas = filas ?? Array.Empty<MapeoCampoFila>();
        }

        public string Id { get; }
        public string Nombre { get; }
        public string[] TiposTrabajo { get; }
        public IReadOnlyList<MapeoCampoFila> Filas { get; }

        public bool AplicaATipo(string tipoTrabajo)
        {
            var t = (tipoTrabajo ?? string.Empty).Trim();
            return TiposTrabajo.Any(x => x.Equals(t, StringComparison.OrdinalIgnoreCase));
        }
    }
}
