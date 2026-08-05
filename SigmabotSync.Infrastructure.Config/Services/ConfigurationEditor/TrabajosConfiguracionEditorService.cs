using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Data.SqlClient;

namespace SigmabotSync.Infrastructure.Services.ConfigurationEditor
{
    /// <summary>
    /// Filas de <c>TrabajosConfiguracion</c> para edición administrativa.
    /// </summary>
    public sealed class TrabajoConfiguracionFila
    {
        public int Id { get; set; }
        public int IdTrabajo { get; set; }
        public string Nombre { get; set; }
        public string ValorTexto { get; set; }
    }

    /// <summary>
    /// CRUD de la tabla TrabajosConfiguracion (herramienta de configuración).
    /// </summary>
    public class TrabajosConfiguracionEditorService
    {
        private readonly string _connectionString;

        public TrabajosConfiguracionEditorService(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public IReadOnlyList<TrabajoConfiguracionFila> ListarPorIdTrabajo(int idTrabajo)
        {
            const string sql = @"
                SELECT id, idTrabajo, Nombre, ValorTexto
                FROM TrabajosConfiguracion
                WHERE idTrabajo = @IdTrabajo
                ORDER BY Nombre";

            var lista = new List<TrabajoConfiguracionFila>();
            using (var cn = new SqlConnection(_connectionString))
            {
                cn.Open();
                using (var cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.AddWithValue("@IdTrabajo", idTrabajo);
                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            lista.Add(new TrabajoConfiguracionFila
                            {
                                Id = ReadInt32Safe(rdr, 0),
                                IdTrabajo = ReadInt32Safe(rdr, 1),
                                Nombre = ReadStringNullable(rdr, 2),
                                ValorTexto = ReadStringNullable(rdr, 3)
                            });
                        }
                    }
                }
            }

            return lista;
        }

        private static int ReadInt32Safe(SqlDataReader rdr, int ordinal)
        {
            if (rdr.IsDBNull(ordinal))
                return 0;
            return Convert.ToInt32(rdr.GetValue(ordinal), CultureInfo.InvariantCulture);
        }

        private static string ReadStringNullable(SqlDataReader rdr, int ordinal)
        {
            if (rdr.IsDBNull(ordinal))
                return null;
            var s = Convert.ToString(rdr.GetValue(ordinal), CultureInfo.InvariantCulture);
            return string.IsNullOrWhiteSpace(s) ? null : s.Trim();
        }

        /// <summary>Inserta una fila clave-valor. Columna <c>tipo</c> en BD queda en "Texto" por defecto (no usada por la consola).</summary>
        public int Insertar(int idTrabajo, string nombre, string valorTexto, string tipoValor = "Texto")
        {
            if (string.IsNullOrWhiteSpace(nombre))
                throw new ArgumentException("Nombre requerido.", nameof(nombre));

            const string sql = @"
                INSERT INTO TrabajosConfiguracion (idTrabajo, Nombre, ValorTexto, ValorFechaHora, ValorNumero, tipo)
                OUTPUT INSERTED.id
                VALUES (@IdTrabajo, @Nombre, @ValorTexto, NULL, NULL, @Tipo)";

            using (var cn = new SqlConnection(_connectionString))
            {
                cn.Open();
                using (var cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.AddWithValue("@IdTrabajo", idTrabajo);
                    cmd.Parameters.AddWithValue("@Nombre", nombre.Trim());
                    cmd.Parameters.AddWithValue("@ValorTexto", (object)valorTexto ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Tipo", string.IsNullOrWhiteSpace(tipoValor) ? "Texto" : tipoValor.Trim());
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }

        public void ActualizarFila(TrabajoConfiguracionFila fila)
        {
            if (fila.Id <= 0)
                throw new ArgumentException("Id inválido.", nameof(fila));

            const string sql = @"
                UPDATE TrabajosConfiguracion SET
                    Nombre = @Nombre,
                    ValorTexto = @ValorTexto
                WHERE id = @Id";

            using (var cn = new SqlConnection(_connectionString))
            {
                cn.Open();
                using (var cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.AddWithValue("@Id", fila.Id);
                    cmd.Parameters.AddWithValue("@Nombre", (object)fila.Nombre ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ValorTexto", (object)fila.ValorTexto ?? DBNull.Value);
                    if (cmd.ExecuteNonQuery() == 0)
                        throw new InvalidOperationException("No existe fila id=" + fila.Id);
                }
            }
        }

        public void Eliminar(int id)
        {
            const string sql = "DELETE FROM TrabajosConfiguracion WHERE id = @Id";
            using (var cn = new SqlConnection(_connectionString))
            {
                cn.Open();
                using (var cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>Si existe (idTrabajo + Nombre), actualiza ValorTexto; si no, inserta.</summary>
        public void UpsertValorTexto(int idTrabajo, string nombre, string valorTexto, string tipoValor = "Texto")
        {
            if (string.IsNullOrWhiteSpace(nombre))
                throw new ArgumentException("Nombre requerido.", nameof(nombre));

            const string sqlMerge = @"
                MERGE TrabajosConfiguracion AS t
                USING (SELECT @IdTrabajo AS idTrabajo, @Nombre AS Nombre) AS s
                ON t.idTrabajo = s.idTrabajo AND t.Nombre = s.Nombre
                WHEN MATCHED THEN
                    UPDATE SET ValorTexto = @ValorTexto
                WHEN NOT MATCHED THEN
                    INSERT (idTrabajo, Nombre, ValorTexto, ValorFechaHora, ValorNumero, tipo)
                    VALUES (@IdTrabajo, @Nombre, @ValorTexto, NULL, NULL, @Tipo);";

            using (var cn = new SqlConnection(_connectionString))
            {
                cn.Open();
                using (var cmd = new SqlCommand(sqlMerge, cn))
                {
                    cmd.Parameters.AddWithValue("@IdTrabajo", idTrabajo);
                    cmd.Parameters.AddWithValue("@Nombre", nombre.Trim());
                    cmd.Parameters.AddWithValue("@ValorTexto", (object)valorTexto ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Tipo", string.IsNullOrWhiteSpace(tipoValor) ? "Texto" : tipoValor.Trim());
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
