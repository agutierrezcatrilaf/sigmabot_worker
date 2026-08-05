using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;

namespace SigmabotSync.Infrastructure.Services.ConfigurationEditor
{
    public sealed class ProjectSyncCampoProyectoFila
    {
        public string AcxProjectIdOrigen { get; set; }
        public string AcxProjectIdDestino { get; set; }
        public string Campo { get; set; }
        public string CampoOrigen { get; set; }
        public bool EsObligatorio { get; set; }
        public string ValorDefault { get; set; }
        public string Catalogo { get; set; }
        public int Orden { get; set; }
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

    /// <summary>Editor de homologacion ProjectSync para el configurador.</summary>
    public sealed class ProjectSyncConfigEditorService
    {
        private readonly string _connectionString;

        public ProjectSyncConfigEditorService(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public IReadOnlyList<ProjectSyncCampoProyectoFila> ListarCampos(
            int idTrabajo,
            string origen,
            string destino)
        {
            const string sql = @"
                SELECT ACXProjectIdOrigen, ACXProjectIdDestino, Campo, CampoOrigen,
                       EsObligatorio, ValorDefault, Catalogo, Orden
                FROM TransmittalSyncCampoProyecto
                WHERE IdTrabajo = @IdTrabajo
                  AND ACXProjectIdOrigen = @Origen
                  AND ACXProjectIdDestino = @Destino
                ORDER BY Orden, Campo";

            var list = new List<ProjectSyncCampoProyectoFila>();
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
                            list.Add(new ProjectSyncCampoProyectoFila
                            {
                                AcxProjectIdOrigen = ReadString(reader, 0),
                                AcxProjectIdDestino = ReadString(reader, 1),
                                Campo = ReadString(reader, 2),
                                CampoOrigen = ReadString(reader, 3),
                                EsObligatorio = !reader.IsDBNull(4) && Convert.ToBoolean(reader.GetValue(4)),
                                ValorDefault = ReadString(reader, 5),
                                Catalogo = ReadString(reader, 6),
                                Orden = reader.IsDBNull(7) ? 0 : Convert.ToInt32(reader.GetValue(7))
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

        public void ReemplazarCampos(
            int idTrabajo,
            string origen,
            string destino,
            IReadOnlyList<ProjectSyncCampoProyectoFila> filas)
        {
            using (var cn = new SqlConnection(_connectionString))
            {
                cn.Open();
                using (var tx = cn.BeginTransaction())
                {
                    using (var del = new SqlCommand(@"
                        DELETE FROM TransmittalSyncCampoProyecto
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
                            if (fila == null || string.IsNullOrWhiteSpace(fila.Campo))
                                continue;

                            using (var ins = new SqlCommand(@"
                                INSERT INTO TransmittalSyncCampoProyecto
                                    (IdTrabajo, ACXProjectIdOrigen, ACXProjectIdDestino, Campo, CampoOrigen,
                                     EsObligatorio, ValorDefault, Catalogo, Orden)
                                VALUES
                                    (@IdTrabajo, @Origen, @Destino, @Campo, @CampoOrigen,
                                     @EsObligatorio, @ValorDefault, @Catalogo, @Orden)", cn, tx))
                            {
                                AddCommon(ins, idTrabajo, origen, destino);
                                ins.Parameters.AddWithValue("@Campo", fila.Campo.Trim());
                                ins.Parameters.AddWithValue("@CampoOrigen", DbValue(fila.CampoOrigen));
                                ins.Parameters.AddWithValue("@EsObligatorio", fila.EsObligatorio);
                                ins.Parameters.AddWithValue("@ValorDefault", DbValue(fila.ValorDefault));
                                ins.Parameters.AddWithValue("@Catalogo", DbValue(fila.Catalogo));
                                ins.Parameters.AddWithValue("@Orden", fila.Orden);
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
