using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using SigmabotSync.Domain.Models.Synchronization;
using SigmabotSync.Domain.Ports;

namespace SigmabotSync.Infrastructure.Services
{
    public sealed class AconexDocumentCatalogService : IAconexDocumentCatalogPort
    {
        private readonly string _connectionString;

        public AconexDocumentCatalogService(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public Task<AconexDocumentCatalog> LoadCatalogAsync(CancellationToken cancellationToken = default) =>
            LoadCatalogAsync(0, null, null, cancellationToken);

        public async Task<AconexDocumentCatalog> LoadCatalogAsync(
            int idTrabajo,
            string acxProjectIdOrigen,
            string acxProjectIdDestino,
            CancellationToken cancellationToken = default)
        {
            var tipos = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var estatus = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var equivDiscipline = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var equivTipoDoc = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var equivCwa = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            using (var cn = new SqlConnection(_connectionString))
            {
                await cn.OpenAsync(cancellationToken).ConfigureAwait(false);
                await LoadMapAsync(cn, "SELECT [Nombre], [idTipo] FROM [TiposDocumentos]", tipos, cancellationToken).ConfigureAwait(false);
                await LoadMapAsync(cn, "SELECT [Nombre], [idEstatus] FROM [EstatusDocumentos]", estatus, cancellationToken).ConfigureAwait(false);

                if (idTrabajo > 0)
                {
                    await LoadEquivalenciasAsync(
                        cn, idTrabajo, acxProjectIdOrigen, acxProjectIdDestino,
                        "Discipline", equivDiscipline, cancellationToken).ConfigureAwait(false);
                    await LoadEquivalenciasAsync(
                        cn, idTrabajo, acxProjectIdOrigen, acxProjectIdDestino,
                        "TipoDocumento", equivTipoDoc, cancellationToken).ConfigureAwait(false);
                    await LoadEquivalenciasAsync(
                        cn, idTrabajo, acxProjectIdOrigen, acxProjectIdDestino,
                        "Cwa", equivCwa, cancellationToken).ConfigureAwait(false);
                }
            }

            if (tipos.Count == 0 && estatus.Count == 0 && equivDiscipline.Count == 0 && equivTipoDoc.Count == 0 && equivCwa.Count == 0)
                return AconexDocumentCatalog.Empty;

            return new AconexDocumentCatalog(tipos, estatus, equivDiscipline, equivTipoDoc, equivCwa);
        }

        private static async Task LoadEquivalenciasAsync(
            SqlConnection cn,
            int idTrabajo,
            string origen,
            string destino,
            string tipo,
            Dictionary<string, string> valorDestinoTarget,
            CancellationToken cancellationToken)
        {
            const string sql = @"
                SELECT ValorOrigen, ValorDestino
                FROM TransmittalSyncEquivalencia
                WHERE IdTrabajo = @IdTrabajo
                  AND Tipo = @Tipo
                  AND Activo = 1
                  AND (@Origen IS NULL OR ACXProjectIdOrigen = @Origen)
                  AND (@Destino IS NULL OR ACXProjectIdDestino = @Destino)";

            try
            {
                using (var cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.AddWithValue("@IdTrabajo", idTrabajo);
                    cmd.Parameters.AddWithValue("@Tipo", tipo);
                    cmd.Parameters.AddWithValue("@Origen", (object)origen ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Destino", (object)destino ?? DBNull.Value);

                    using (var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
                    {
                        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                        {
                            string vo = reader[0] == DBNull.Value ? null : reader[0].ToString()?.Trim();
                            string vd = reader[1] == DBNull.Value ? null : reader[1].ToString()?.Trim();
                            if (string.IsNullOrEmpty(vo) || string.IsNullOrEmpty(vd))
                                continue;
                            if (!valorDestinoTarget.ContainsKey(vo))
                                valorDestinoTarget[vo] = vd;
                        }
                    }
                }
            }
            catch (SqlException)
            {
                // Tabla opcional hasta ejecutar CreateTable_TransmittalSyncEquivalencia.sql
            }
        }

        private static async Task LoadMapAsync(
            SqlConnection cn,
            string sql,
            Dictionary<string, string> target,
            CancellationToken cancellationToken)
        {
            try
            {
                using (var cmd = new SqlCommand(sql, cn))
                using (var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
                {
                    while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                    {
                        string nombre = reader[0] == DBNull.Value ? null : reader[0].ToString()?.Trim();
                        string id = reader[1] == DBNull.Value ? null : reader[1].ToString()?.Trim();
                        if (string.IsNullOrEmpty(nombre) || string.IsNullOrEmpty(id))
                            continue;
                        if (!target.ContainsKey(nombre))
                            target[nombre] = id;
                    }
                }
            }
            catch (SqlException)
            {
                // Tabla opcional (misma tolerancia que FileUploadWithMetadata).
            }
        }
    }
}
