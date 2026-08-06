using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;

namespace SigmabotSync.Infrastructure.Services.ConfigurationEditor
{
    public sealed class ProjectSyncCampoDestinoFila
    {
        public string AcxProjectIdOrigen { get; set; }
        public string AcxProjectIdDestino { get; set; }
        public string CampoDestino { get; set; }
        public string TipoFuente { get; set; }
        public string FuenteValor { get; set; }
        public bool EsObligatorio { get; set; }
        public string ValorDefault { get; set; }
        public string Catalogo { get; set; }
        public int Orden { get; set; }
        public bool Activo { get; set; } = true;
    }

    public sealed class ProjectSyncEquivalenciaFila
    {
        public string AcxProjectIdOrigen { get; set; }
        public string AcxProjectIdDestino { get; set; }
        public string Tipo { get; set; }
        public string ValorOrigen { get; set; }
        public string ValorDestino { get; set; }
        public string CodigoDestino { get; set; }
        public bool Activo { get; set; } = true;
    }

    /// <summary>Editor de matriz destino y equivalencias ProjectSync para el configurador.</summary>
    public sealed class ProjectSyncConfigEditorService
    {
        private readonly string _connectionString;

        public ProjectSyncConfigEditorService(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public IReadOnlyList<ProjectSyncCampoDestinoFila> ListarCamposDestino(
            int idTrabajo,
            string origen,
            string destino)
        {
            const string sql = @"
                SELECT ACXProjectIdOrigen, ACXProjectIdDestino, CampoDestino, TipoFuente, FuenteValor,
                       EsObligatorio, ValorDefault, Catalogo, Orden, Activo
                FROM TransmittalSyncCampoDestino
                WHERE IdTrabajo = @IdTrabajo
                  AND ACXProjectIdOrigen = @Origen
                  AND ACXProjectIdDestino = @Destino
                ORDER BY Orden, CampoDestino";

            var list = new List<ProjectSyncCampoDestinoFila>();
            using (var cn = new SqlConnection(_connectionString))
            {
                cn.Open();
                using (var cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.AddWithValue("@IdTrabajo", idTrabajo);
                    cmd.Parameters.AddWithValue("@Origen", origen ?? "");
                    cmd.Parameters.AddWithValue("@Destino", destino ?? "");
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new ProjectSyncCampoDestinoFila
                            {
                                AcxProjectIdOrigen = ReadString(reader, 0),
                                AcxProjectIdDestino = ReadString(reader, 1),
                                CampoDestino = ReadString(reader, 2),
                                TipoFuente = ReadString(reader, 3),
                                FuenteValor = ReadString(reader, 4),
                                EsObligatorio = !reader.IsDBNull(5) && Convert.ToBoolean(reader.GetValue(5)),
                                ValorDefault = ReadString(reader, 6),
                                Catalogo = ReadString(reader, 7),
                                Orden = reader.IsDBNull(8) ? 0 : Convert.ToInt32(reader.GetValue(8)),
                                Activo = reader.IsDBNull(9) || Convert.ToBoolean(reader.GetValue(9))
                            });
                        }
                    }
                }
            }

            return list;
        }

        public IReadOnlyList<ProjectSyncEquivalenciaFila> ListarEquivalencias(
            int idTrabajo,
            string origen,
            string destino)
        {
            const string sql = @"
                SELECT ACXProjectIdOrigen, ACXProjectIdDestino, Tipo, ValorOrigen, ValorDestino, CodigoDestino, Activo
                FROM TransmittalSyncEquivalencia
                WHERE IdTrabajo = @IdTrabajo
                  AND ACXProjectIdOrigen = @Origen
                  AND ACXProjectIdDestino = @Destino
                ORDER BY Tipo, ValorOrigen";

            var list = new List<ProjectSyncEquivalenciaFila>();
            using (var cn = new SqlConnection(_connectionString))
            {
                cn.Open();
                using (var cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.AddWithValue("@IdTrabajo", idTrabajo);
                    cmd.Parameters.AddWithValue("@Origen", origen ?? "");
                    cmd.Parameters.AddWithValue("@Destino", destino ?? "");
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new ProjectSyncEquivalenciaFila
                            {
                                AcxProjectIdOrigen = ReadString(reader, 0),
                                AcxProjectIdDestino = ReadString(reader, 1),
                                Tipo = ReadString(reader, 2),
                                ValorOrigen = ReadString(reader, 3),
                                ValorDestino = ReadString(reader, 4),
                                CodigoDestino = ReadString(reader, 5),
                                Activo = reader.IsDBNull(6) || Convert.ToBoolean(reader.GetValue(6))
                            });
                        }
                    }
                }
            }

            return list;
        }

        public void ReemplazarCamposDestino(
            int idTrabajo,
            string origen,
            string destino,
            IReadOnlyList<ProjectSyncCampoDestinoFila> filas)
        {
            using (var cn = new SqlConnection(_connectionString))
            {
                cn.Open();
                using (var tx = cn.BeginTransaction())
                {
                    using (var del = new SqlCommand(@"
                        DELETE FROM TransmittalSyncCampoDestino
                        WHERE IdTrabajo = @IdTrabajo
                          AND ACXProjectIdOrigen = @Origen
                          AND ACXProjectIdDestino = @Destino", cn, tx))
                    {
                        del.Parameters.AddWithValue("@IdTrabajo", idTrabajo);
                        del.Parameters.AddWithValue("@Origen", origen ?? "");
                        del.Parameters.AddWithValue("@Destino", destino ?? "");
                        del.ExecuteNonQuery();
                    }

                    if (filas != null)
                    {
                        foreach (var fila in filas)
                        {
                            if (fila == null || string.IsNullOrWhiteSpace(fila.CampoDestino))
                                continue;
                            if (string.IsNullOrWhiteSpace(fila.TipoFuente))
                                continue;

                            using (var ins = new SqlCommand(@"
                                INSERT INTO TransmittalSyncCampoDestino
                                    (IdTrabajo, ACXProjectIdOrigen, ACXProjectIdDestino, CampoDestino, TipoFuente,
                                     FuenteValor, EsObligatorio, ValorDefault, Catalogo, Orden, Activo)
                                VALUES
                                    (@IdTrabajo, @Origen, @Destino, @CampoDestino, @TipoFuente,
                                     @FuenteValor, @EsObligatorio, @ValorDefault, @Catalogo, @Orden, @Activo)", cn, tx))
                            {
                                AddCommon(ins, idTrabajo, origen, destino);
                                ins.Parameters.AddWithValue("@CampoDestino", fila.CampoDestino.Trim());
                                ins.Parameters.AddWithValue("@TipoFuente", fila.TipoFuente.Trim());
                                ins.Parameters.AddWithValue("@FuenteValor", DbValue(fila.FuenteValor));
                                ins.Parameters.AddWithValue("@EsObligatorio", fila.EsObligatorio);
                                ins.Parameters.AddWithValue("@ValorDefault", DbValue(fila.ValorDefault));
                                ins.Parameters.AddWithValue("@Catalogo", DbValue(fila.Catalogo));
                                ins.Parameters.AddWithValue("@Orden", fila.Orden);
                                ins.Parameters.AddWithValue("@Activo", fila.Activo);
                                ins.ExecuteNonQuery();
                            }
                        }
                    }

                    tx.Commit();
                }
            }
        }

        public void ReemplazarEquivalencias(
            int idTrabajo,
            string origen,
            string destino,
            IReadOnlyList<ProjectSyncEquivalenciaFila> filas)
        {
            using (var cn = new SqlConnection(_connectionString))
            {
                cn.Open();
                using (var tx = cn.BeginTransaction())
                {
                    using (var del = new SqlCommand(@"
                        DELETE FROM TransmittalSyncEquivalencia
                        WHERE IdTrabajo = @IdTrabajo
                          AND ACXProjectIdOrigen = @Origen
                          AND ACXProjectIdDestino = @Destino", cn, tx))
                    {
                        del.Parameters.AddWithValue("@IdTrabajo", idTrabajo);
                        del.Parameters.AddWithValue("@Origen", origen ?? "");
                        del.Parameters.AddWithValue("@Destino", destino ?? "");
                        del.ExecuteNonQuery();
                    }

                    if (filas != null)
                    {
                        foreach (var fila in filas)
                        {
                            if (fila == null ||
                                string.IsNullOrWhiteSpace(fila.Tipo) ||
                                string.IsNullOrWhiteSpace(fila.ValorOrigen) ||
                                string.IsNullOrWhiteSpace(fila.ValorDestino) ||
                                string.IsNullOrWhiteSpace(fila.CodigoDestino))
                            {
                                continue;
                            }

                            using (var ins = new SqlCommand(@"
                                INSERT INTO TransmittalSyncEquivalencia
                                    (IdTrabajo, ACXProjectIdOrigen, ACXProjectIdDestino, Tipo,
                                     ValorOrigen, ValorDestino, CodigoDestino, Activo)
                                VALUES
                                    (@IdTrabajo, @Origen, @Destino, @Tipo,
                                     @ValorOrigen, @ValorDestino, @CodigoDestino, @Activo)", cn, tx))
                            {
                                AddCommon(ins, idTrabajo, origen, destino);
                                ins.Parameters.AddWithValue("@Tipo", fila.Tipo.Trim());
                                ins.Parameters.AddWithValue("@ValorOrigen", fila.ValorOrigen.Trim());
                                ins.Parameters.AddWithValue("@ValorDestino", fila.ValorDestino.Trim());
                                ins.Parameters.AddWithValue("@CodigoDestino", fila.CodigoDestino.Trim());
                                ins.Parameters.AddWithValue("@Activo", fila.Activo);
                                ins.ExecuteNonQuery();
                            }
                        }
                    }

                    tx.Commit();
                }
            }
        }

        private static void AddCommon(SqlCommand cmd, int idTrabajo, string origen, string destino)
        {
            cmd.Parameters.AddWithValue("@IdTrabajo", idTrabajo);
            cmd.Parameters.AddWithValue("@Origen", origen ?? "");
            cmd.Parameters.AddWithValue("@Destino", destino ?? "");
        }

        private static object DbValue(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();
        }

        private static string ReadString(SqlDataReader reader, int ordinal)
        {
            return reader.IsDBNull(ordinal) ? null : reader.GetValue(ordinal)?.ToString()?.Trim();
        }
    }
}
