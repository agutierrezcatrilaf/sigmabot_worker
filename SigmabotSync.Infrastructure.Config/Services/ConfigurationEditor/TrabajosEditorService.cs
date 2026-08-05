using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using SigmabotSync.Domain.Entities;
using SigmabotSync.Infrastructure.Data;

namespace SigmabotSync.Infrastructure.Services.ConfigurationEditor
{
    /// <summary>
    /// Alta, baja y modificación de la tabla Trabajos (herramienta de configuración).
    /// Solo persiste Nombre, Tipo y Estado; el resumen de ejecución lo actualiza la consola.
    /// </summary>
    public class TrabajosEditorService
    {
        private readonly string _connectionString;

        public TrabajosEditorService(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public IReadOnlyList<Trabajo> ListarTodos()
        {
            const string sql = @"
                SELECT id AS Id, Nombre, Tipo, Estado,
                    FechaUltimaEjecucion, ResultadoUltimaEjecucion, UltCorrEjecucion
                FROM Trabajos
                ORDER BY id";

            var lista = new List<Trabajo>();
            using (var cn = new SqlConnection(_connectionString))
            {
                cn.Open();
                using (var cmd = new SqlCommand(sql, cn))
                using (var adapter = new SqlDataAdapter(cmd))
                {
                    var dt = new DataTable();
                    adapter.Fill(dt);
                    foreach (DataRow row in dt.Rows)
                        lista.Add(row.MapTo<Trabajo>());
                }
            }

            return lista;
        }

        /// <summary>Inserta un trabajo. Devuelve el id generado (IDENTITY).</summary>
        public int Insertar(Trabajo t)
        {
            const string sql = @"
                INSERT INTO Trabajos (Nombre, Tipo, Estado)
                OUTPUT INSERTED.id
                VALUES (@Nombre, @Tipo, @Estado)";

            using (var cn = new SqlConnection(_connectionString))
            {
                cn.Open();
                using (var cmd = new SqlCommand(sql, cn))
                {
                    AddEditableParameters(cmd, t);
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }

        public void Actualizar(Trabajo t)
        {
            if (t.Id <= 0)
                throw new ArgumentException("Id inválido.", nameof(t));

            const string sql = @"
                UPDATE Trabajos SET
                    Nombre = @Nombre,
                    Tipo = @Tipo,
                    Estado = @Estado
                WHERE id = @Id";

            using (var cn = new SqlConnection(_connectionString))
            {
                cn.Open();
                using (var cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.AddWithValue("@Id", t.Id);
                    AddEditableParameters(cmd, t);
                    if (cmd.ExecuteNonQuery() == 0)
                        throw new InvalidOperationException("No existe Trabajo id=" + t.Id);
                }
            }
        }

        private static void AddEditableParameters(SqlCommand cmd, Trabajo t)
        {
            cmd.Parameters.AddWithValue("@Nombre", (object)t.Nombre ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Tipo", (object)t.Tipo ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Estado", (object)t.Estado ?? DBNull.Value);
        }

        /// <summary>Elimina un trabajo por id. Puede fallar si existen filas en TrabajosProgramacion u otras FK.</summary>
        public void Eliminar(int id)
        {
            if (id <= 0)
                throw new ArgumentOutOfRangeException(nameof(id));

            const string sql = "DELETE FROM Trabajos WHERE id = @Id";
            using (var cn = new SqlConnection(_connectionString))
            {
                cn.Open();
                using (var cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    int n = cmd.ExecuteNonQuery();
                    if (n == 0)
                        throw new InvalidOperationException("No existe Trabajo id=" + id);
                }
            }
        }
    }
}
