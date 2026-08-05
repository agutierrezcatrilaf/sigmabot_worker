using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using Microsoft.Data.SqlClient;
using SigmabotSync.Domain.Entities;

namespace SigmabotSync.Infrastructure.Services.ConfigurationEditor
{
    /// <summary>CRUD de <c>TrabajosProgramacion</c> para la herramienta de configuración.</summary>
    public class TrabajosProgramacionEditorService
    {
        private readonly string _connectionString;

        public TrabajosProgramacionEditorService(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public IReadOnlyList<TrabajoProgramacion> ListarPorIdTrabajo(int idTrabajo)
        {
            const string sql = @"
                SELECT Id, IdTrabajo, DiaSemana, Hora, Activo
                FROM [dbo].[TrabajosProgramacion]
                WHERE IdTrabajo = @IdTrabajo
                ORDER BY DiaSemana, Hora";

            var lista = new List<TrabajoProgramacion>();
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
                            lista.Add(new TrabajoProgramacion
                            {
                                Id = Convert.ToInt32(rdr.GetValue(0), CultureInfo.InvariantCulture),
                                IdTrabajo = Convert.ToInt32(rdr.GetValue(1), CultureInfo.InvariantCulture),
                                DiaSemana = Convert.ToInt32(rdr.GetValue(2), CultureInfo.InvariantCulture),
                                Hora = LeerHoraSql(rdr, 3),
                                Activo = Convert.ToBoolean(rdr.GetValue(4), CultureInfo.InvariantCulture)
                            });
                        }
                    }
                }
            }
            return lista;
        }

        private static TimeSpan LeerHoraSql(SqlDataReader rdr, int ordinal)
        {
            if (rdr.IsDBNull(ordinal))
                return TimeSpan.Zero;
            var v = rdr.GetValue(ordinal);
            if (v is TimeSpan ts)
                return ts;
            if (v is DateTime dt)
                return dt.TimeOfDay;
            var s = Convert.ToString(v, CultureInfo.InvariantCulture);
            if (string.IsNullOrWhiteSpace(s))
                return TimeSpan.Zero;
            if (TimeSpan.TryParse(s, CultureInfo.InvariantCulture, out var parsed))
                return parsed;
            return TimeSpan.Zero;
        }

        public int Insertar(TrabajoProgramacion fila)
        {
            if (fila.IdTrabajo <= 0)
                throw new ArgumentException("IdTrabajo inválido.", nameof(fila));
            if (fila.DiaSemana < 0 || fila.DiaSemana > 6)
                throw new ArgumentException("DiaSemana debe estar entre 0 (domingo) y 6 (sábado).", nameof(fila));

            const string sql = @"
                INSERT INTO [dbo].[TrabajosProgramacion] (IdTrabajo, DiaSemana, Hora, Activo)
                OUTPUT INSERTED.Id
                VALUES (@IdTrabajo, @DiaSemana, @Hora, @Activo)";

            using (var cn = new SqlConnection(_connectionString))
            {
                cn.Open();
                using (var cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.AddWithValue("@IdTrabajo", fila.IdTrabajo);
                    cmd.Parameters.AddWithValue("@DiaSemana", fila.DiaSemana);
                    cmd.Parameters.Add("@Hora", SqlDbType.Time).Value = fila.Hora;
                    cmd.Parameters.AddWithValue("@Activo", fila.Activo);
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }

        public void Actualizar(TrabajoProgramacion fila)
        {
            if (fila.Id <= 0)
                throw new ArgumentException("Id inválido.", nameof(fila));
            if (fila.DiaSemana < 0 || fila.DiaSemana > 6)
                throw new ArgumentException("DiaSemana debe estar entre 0 y 6.", nameof(fila));

            const string sql = @"
                UPDATE [dbo].[TrabajosProgramacion]
                SET IdTrabajo = @IdTrabajo, DiaSemana = @DiaSemana, Hora = @Hora, Activo = @Activo
                WHERE Id = @Id";

            using (var cn = new SqlConnection(_connectionString))
            {
                cn.Open();
                using (var cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.AddWithValue("@Id", fila.Id);
                    cmd.Parameters.AddWithValue("@IdTrabajo", fila.IdTrabajo);
                    cmd.Parameters.AddWithValue("@DiaSemana", fila.DiaSemana);
                    cmd.Parameters.Add("@Hora", SqlDbType.Time).Value = fila.Hora;
                    cmd.Parameters.AddWithValue("@Activo", fila.Activo);
                    if (cmd.ExecuteNonQuery() == 0)
                        throw new InvalidOperationException("No existe programación id=" + fila.Id);
                }
            }
        }

        public void Eliminar(int id)
        {
            const string sql = "DELETE FROM [dbo].[TrabajosProgramacion] WHERE Id = @Id";
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
    }
}
