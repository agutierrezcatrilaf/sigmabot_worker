using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using SigmabotSync.Domain.Configuration;
using SigmabotSync.Domain.Entities;
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

                if (await TableExistsAsync(cn, "TransmittalSyncCampoDestino", cancellationToken).ConfigureAwait(false))
                {
                    var rows = await LoadCampoDestinoAsync(cn, idTrabajo, origen, destino, cancellationToken).ConfigureAwait(false);
                    if (rows.Count > 0)
                        return rows;
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

        private static async Task<List<TransmittalSyncCampoMapeoItem>> LoadCampoDestinoAsync(
            SqlConnection cn,
            int idTrabajo,
            string origen,
            string destino,
            CancellationToken cancellationToken)
        {
            const string sql = @"
                SELECT CampoDestino, TipoFuente, FuenteValor, EsObligatorio, ValorDefault, Catalogo, Orden
                FROM TransmittalSyncCampoDestino
                WHERE IdTrabajo = @IdTrabajo
                  AND ACXProjectIdOrigen = @Origen
                  AND ACXProjectIdDestino = @Destino
                  AND Activo = 1
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
                        string tipoFuente = reader["TipoFuente"] as string ?? "";
                        string fuenteValor = reader["FuenteValor"] as string;
                        bool esObligatorio = reader["EsObligatorio"] != DBNull.Value && Convert.ToBoolean(reader["EsObligatorio"]);
                        string valorDefault = reader["ValorDefault"] as string;
                        string catalogo = reader["Catalogo"] as string;
                        int orden = reader["Orden"] != DBNull.Value ? Convert.ToInt32(reader["Orden"]) : 0;

                        list.Add(ProjectSyncCampoDestinoMapper.ToMapeoItem(
                            campoDestino, tipoFuente, fuenteValor, esObligatorio, valorDefault, catalogo, orden));
                    }
                }
            }

            return list;
        }
    }
}
