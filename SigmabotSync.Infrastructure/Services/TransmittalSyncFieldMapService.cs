using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using SigmabotSync.Domain.Entities;
using SigmabotSync.Domain.Models.Synchronization;
using SigmabotSync.Domain.Ports;

namespace SigmabotSync.Infrastructure.Services
{
    public sealed class TransmittalSyncFieldMapService : ITransmittalSyncFieldMapPort
    {
        private readonly string _connectionString;

        public TransmittalSyncFieldMapService(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public async Task<IReadOnlyList<TransmittalSyncCampoMapeoItem>> GetMappingsAsync(
            int idTrabajo,
            string acxProjectIdOrigen,
            string acxProjectIdDestino,
            CancellationToken cancellationToken = default)
        {
            string origen = acxProjectIdOrigen?.Trim() ?? "";
            string destino = acxProjectIdDestino?.Trim() ?? "";

            using (var cn = new SqlConnection(_connectionString))
            {
                await cn.OpenAsync(cancellationToken).ConfigureAwait(false);

                if (await TableExistsAsync(cn, "TransmittalSyncCampoProyecto", cancellationToken).ConfigureAwait(false))
                {
                    var rows = await LoadCampoProyectoAsync(cn, idTrabajo, origen, destino, cancellationToken).ConfigureAwait(false);
                    if (rows.Count > 0)
                        return rows;
                }

                if (await TableExistsAsync(cn, "TransmittalSyncCampoMapeo", cancellationToken).ConfigureAwait(false))
                {
                    return await LoadLegacyCampoMapeoAsync(cn, idTrabajo, origen, destino, cancellationToken).ConfigureAwait(false);
                }
            }

            return Array.Empty<TransmittalSyncCampoMapeoItem>();
        }

        private static async Task<bool> TableExistsAsync(SqlConnection cn, string tableName, CancellationToken cancellationToken)
        {
            const string sql = "SELECT 1 FROM sys.tables WHERE name = @Name";
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.AddWithValue("@Name", tableName);
                object result = await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                return result != null;
            }
        }

        private static async Task<bool> ColumnExistsAsync(SqlConnection cn, string tableName, string columnName, CancellationToken cancellationToken)
        {
            const string sql = @"
                SELECT 1 FROM sys.columns
                WHERE object_id = OBJECT_ID(@Table) AND name = @Column";
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.AddWithValue("@Table", tableName);
                cmd.Parameters.AddWithValue("@Column", columnName);
                object result = await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                return result != null;
            }
        }

        private static async Task<List<TransmittalSyncCampoMapeoItem>> LoadCampoProyectoAsync(
            SqlConnection cn,
            int idTrabajo,
            string origen,
            string destino,
            CancellationToken cancellationToken)
        {
            bool hasOrigen = await ColumnExistsAsync(cn, "TransmittalSyncCampoProyecto", "ACXProjectIdOrigen", cancellationToken).ConfigureAwait(false);
            bool hasCatalogo = await ColumnExistsAsync(cn, "TransmittalSyncCampoProyecto", "Catalogo", cancellationToken).ConfigureAwait(false);
            bool hasLegacyPicklist = await ColumnExistsAsync(cn, "TransmittalSyncCampoProyecto", "ResolverPicklist", cancellationToken).ConfigureAwait(false);

            string catalogoCol = hasCatalogo ? "Catalogo" : "NULL AS Catalogo";
            string picklistCol = hasLegacyPicklist ? "ResolverPicklist" : "0 AS ResolverPicklist";

            string sql = hasOrigen
                ? $@"
                SELECT Campo, CampoOrigen, EsObligatorio, ValorDefault, {catalogoCol}, {picklistCol}, Orden
                FROM TransmittalSyncCampoProyecto
                WHERE IdTrabajo = @IdTrabajo
                  AND ACXProjectIdOrigen = @Origen
                  AND ACXProjectIdDestino = @Destino
                ORDER BY Orden, Campo"
                : $@"
                SELECT Campo, CampoOrigen, EsObligatorio, ValorDefault, {catalogoCol}, {picklistCol}, Orden
                FROM TransmittalSyncCampoProyecto
                WHERE IdTrabajo = @IdTrabajo AND ACXProjectId = @Destino
                ORDER BY Orden, Campo";

            var list = new List<TransmittalSyncCampoMapeoItem>();
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.AddWithValue("@IdTrabajo", idTrabajo);
                cmd.Parameters.AddWithValue("@Destino", destino);
                if (hasOrigen)
                    cmd.Parameters.AddWithValue("@Origen", origen);

                using (var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
                {
                    while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                    {
                        string campo = reader["Campo"] as string ?? "";
                        string campoOrigen = reader["CampoOrigen"] as string;
                        string catalogo = reader["Catalogo"] as string;
                        bool legacyPicklist = reader["ResolverPicklist"] != DBNull.Value && Convert.ToBoolean(reader["ResolverPicklist"]);

                        list.Add(new TransmittalSyncCampoMapeoItem
                        {
                            CampoDestino = campo,
                            CampoOrigen = string.IsNullOrWhiteSpace(campoOrigen) ? campo : campoOrigen.Trim(),
                            EsObligatorio = reader["EsObligatorio"] != DBNull.Value && Convert.ToBoolean(reader["EsObligatorio"]),
                            ValorDefault = reader["ValorDefault"] as string,
                            Catalogo = ResolveCatalogo(catalogo, legacyPicklist, campo),
                            Orden = reader["Orden"] != DBNull.Value ? Convert.ToInt32(reader["Orden"]) : 0
                        });
                    }
                }
            }

            return list;
        }

        private static string ResolveCatalogo(string catalogo, bool legacyPicklist, string campoDestino)
        {
            if (!string.IsNullOrWhiteSpace(catalogo))
                return catalogo.Trim();

            if (!legacyPicklist)
                return null;

            if (string.Equals(campoDestino, "DocumentStatusId", StringComparison.OrdinalIgnoreCase))
                return AconexDocumentCatalogNames.EstatusDocumentos;

            if (string.Equals(campoDestino, "DocumentTypeId", StringComparison.OrdinalIgnoreCase))
                return AconexDocumentCatalogNames.TiposDocumentos;

            return null;
        }

        private static async Task<IReadOnlyList<TransmittalSyncCampoMapeoItem>> LoadLegacyCampoMapeoAsync(
            SqlConnection cn,
            int idTrabajo,
            string origen,
            string destino,
            CancellationToken cancellationToken)
        {
            const string sql = @"
                SELECT CampoOrigen, CampoDestino, ValorDefault, ResolverPicklist, Orden
                FROM TransmittalSyncCampoMapeo
                WHERE IdTrabajo = @IdTrabajo
                  AND ACXProjectIdOrigen = @Origen
                  AND ACXProjectIdDestino = @Destino
                ORDER BY Orden, CampoDestino";

            var list = new List<TransmittalSyncCampoMapeoItem>();
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.AddWithValue("@IdTrabajo", idTrabajo);
                cmd.Parameters.AddWithValue("@Origen", origen);
                cmd.Parameters.AddWithValue("@Destino", destino);
                using (var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
                {
                    while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                    {
                        string campoDestino = reader["CampoDestino"] as string ?? "";
                        bool legacyPicklist = reader["ResolverPicklist"] != DBNull.Value && Convert.ToBoolean(reader["ResolverPicklist"]);
                        list.Add(new TransmittalSyncCampoMapeoItem
                        {
                            CampoOrigen = reader["CampoOrigen"] as string ?? "",
                            CampoDestino = campoDestino,
                            ValorDefault = reader["ValorDefault"] as string,
                            Catalogo = ResolveCatalogo(null, legacyPicklist, campoDestino),
                            Orden = reader["Orden"] != DBNull.Value ? Convert.ToInt32(reader["Orden"]) : 0
                        });
                    }
                }
            }

            return list;
        }
    }
}
