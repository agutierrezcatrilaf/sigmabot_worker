using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using SigmabotSync.Domain.Entities;

namespace SigmabotSync.Infrastructure.Services
{
    /// <summary>
    /// Operaciones sobre la tabla TrabajosEjecucion: registro de inicio de ejecución (FechaHoraFin NULL),
    /// actualización al finalizar, y consulta de ejecución en curso para evitar duplicados.
    /// </summary>
    public class TrabajosEjecucionService
    {
        private readonly string _connectionString;

        /// <summary>Tiempo máximo que se considera "en curso" una ejecución sin FechaHoraFin; pasado este tiempo se considera abandonada y se permite una nueva ejecución (p. ej. scheduler cada 10 min).</summary>
        public static readonly TimeSpan TiempoMaximoEnCursoPorDefecto = TimeSpan.FromMinutes(60);

        public TrabajosEjecucionService(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        /// <summary>
        /// Indica si existe una ejecución en curso para el trabajo (registro con FechaHoraFin NULL y FechaHoraInicio dentro del margen).
        /// Sirve para no lanzar una segunda instancia del mismo trabajo.
        /// </summary>
        /// <param name="idTrabajo">Id del trabajo.</param>
        /// <param name="tiempoMaximoEnCurso">Ejecuciones con inicio anterior a (ahora - tiempoMaximoEnCurso) se consideran abandonadas. Si null, usa TiempoMaximoEnCursoPorDefecto (24h).</param>
        public bool ExisteEjecucionEnCurso(int idTrabajo, TimeSpan? tiempoMaximoEnCurso = null)
        {
            var margen = tiempoMaximoEnCurso ?? TiempoMaximoEnCursoPorDefecto;
            var desde = DateTime.Now - margen;

            const string sql = @"
                SELECT 1 FROM [dbo].[TrabajosEjecucion] WITH (NOLOCK)
                WHERE IdTrabajo = @IdTrabajo
                  AND FechaHoraFin IS NULL
                  AND FechaHoraInicio >= @Desde";

            using (var cn = new SqlConnection(_connectionString))
            {
                cn.Open();
                using (var cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.AddWithValue("@IdTrabajo", idTrabajo);
                    cmd.Parameters.AddWithValue("@Desde", desde);
                    using (var rdr = cmd.ExecuteReader())
                        return rdr.HasRows;
                }
            }
        }

        /// <summary>
        /// Devuelve los IdTrabajo que tienen al menos una ejecución en curso (FechaHoraFin NULL, inicio dentro del margen).
        /// Útil para informar en log cuando no hay pendientes pero sí trabajos ejecutándose.
        /// </summary>
        public IReadOnlyList<int> ObtenerIdsTrabajosEnCurso(TimeSpan? tiempoMaximoEnCurso = null)
        {
            var margen = tiempoMaximoEnCurso ?? TiempoMaximoEnCursoPorDefecto;
            var desde = DateTime.Now - margen;

            const string sql = @"
                SELECT DISTINCT IdTrabajo FROM [dbo].[TrabajosEjecucion] WITH (NOLOCK)
                WHERE FechaHoraFin IS NULL
                  AND FechaHoraInicio >= @Desde
                ORDER BY IdTrabajo";

            var lista = new List<int>();
            using (var cn = new SqlConnection(_connectionString))
            {
                cn.Open();
                using (var cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.AddWithValue("@Desde", desde);
                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                            lista.Add(rdr.GetInt32(0));
                    }
                }
            }
            return lista;
        }

        /// <summary>
        /// Registra el inicio de una ejecución (FechaHoraFin NULL). Debe actualizarse con ActualizarFin al terminar.
        /// Requiere que la columna FechaHoraFin permita NULL en la tabla TrabajosEjecucion.
        /// </summary>
        /// <returns>Id del registro insertado (para pasarlo a ActualizarFin).</returns>
        public int InsertarInicio(int idTrabajo, DateTime fechaHoraInicio, string tipoEjecucion = "Scheduler")
        {
            const string sql = @"
                INSERT INTO [dbo].[TrabajosEjecucion] (IdTrabajo, FechaHoraInicio, FechaHoraFin, Exito, MensajeError, EtapasEjecutadas, DetalleEjecucion, TipoEjecucion)
                OUTPUT INSERTED.Id
                VALUES (@IdTrabajo, @FechaHoraInicio, NULL, 0, NULL, NULL, NULL, @TipoEjecucion)";

            using (var cn = new SqlConnection(_connectionString))
            {
                cn.Open();
                using (var cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.AddWithValue("@IdTrabajo", idTrabajo);
                    cmd.Parameters.AddWithValue("@FechaHoraInicio", fechaHoraInicio);
                    cmd.Parameters.AddWithValue("@TipoEjecucion", (object)tipoEjecucion ?? DBNull.Value);
                    return (int)cmd.ExecuteScalar();
                }
            }
        }

        /// <summary>
        /// Actualiza el registro de ejecución creado con InsertarInicio al finalizar (FechaHoraFin y resultado).
        /// </summary>
        public void ActualizarFin(
            int idEjecucion,
            DateTime fechaHoraFin,
            bool exito,
            string mensajeError,
            IReadOnlyList<string> etapasEjecutadas,
            string detalleEjecucion = null)
        {
            var etapas = etapasEjecutadas != null && etapasEjecutadas.Count > 0
                ? string.Join(",", etapasEjecutadas)
                : null;

            const string sql = @"
                UPDATE [dbo].[TrabajosEjecucion]
                SET FechaHoraFin = @FechaHoraFin, Exito = @Exito, MensajeError = @MensajeError,
                    EtapasEjecutadas = @EtapasEjecutadas, DetalleEjecucion = @DetalleEjecucion
                WHERE Id = @Id";

            using (var cn = new SqlConnection(_connectionString))
            {
                cn.Open();
                using (var cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.AddWithValue("@Id", idEjecucion);
                    cmd.Parameters.AddWithValue("@FechaHoraFin", fechaHoraFin);
                    cmd.Parameters.AddWithValue("@Exito", exito);
                    cmd.Parameters.AddWithValue("@MensajeError", (object)mensajeError ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@EtapasEjecutadas", (object)etapas ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@DetalleEjecucion", (object)detalleEjecucion ?? DBNull.Value);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Historial de ejecuciones de un trabajo, más recientes primero.
        /// </summary>
        public IReadOnlyList<TrabajoEjecucion> ListarPorIdTrabajo(int idTrabajo, int limit = 50)
        {
            if (limit <= 0)
                limit = 50;
            if (limit > 200)
                limit = 200;

            const string sql = @"
                SELECT Id, IdTrabajo, FechaHoraInicio, FechaHoraFin, Exito, MensajeError,
                       EtapasEjecutadas, DetalleEjecucion, TipoEjecucion
                FROM [dbo].[TrabajosEjecucion] WITH (NOLOCK)
                WHERE IdTrabajo = @IdTrabajo
                ORDER BY FechaHoraInicio DESC
                OFFSET 0 ROWS FETCH NEXT @Limit ROWS ONLY";

            var lista = new List<TrabajoEjecucion>();
            using (var cn = new SqlConnection(_connectionString))
            {
                cn.Open();
                using (var cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.AddWithValue("@IdTrabajo", idTrabajo);
                    cmd.Parameters.AddWithValue("@Limit", limit);
                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                            lista.Add(LeerEjecucion(rdr));
                    }
                }
            }
            return lista;
        }

        private static TrabajoEjecucion LeerEjecucion(SqlDataReader rdr)
        {
            return new TrabajoEjecucion
            {
                Id = rdr.GetInt32(rdr.GetOrdinal("Id")),
                IdTrabajo = rdr.GetInt32(rdr.GetOrdinal("IdTrabajo")),
                FechaHoraInicio = rdr.GetDateTime(rdr.GetOrdinal("FechaHoraInicio")),
                FechaHoraFin = rdr.IsDBNull(rdr.GetOrdinal("FechaHoraFin"))
                    ? (DateTime?)null
                    : rdr.GetDateTime(rdr.GetOrdinal("FechaHoraFin")),
                Exito = rdr.GetBoolean(rdr.GetOrdinal("Exito")),
                MensajeError = rdr.IsDBNull(rdr.GetOrdinal("MensajeError"))
                    ? null
                    : rdr.GetString(rdr.GetOrdinal("MensajeError")),
                EtapasEjecutadas = rdr.IsDBNull(rdr.GetOrdinal("EtapasEjecutadas"))
                    ? null
                    : rdr.GetString(rdr.GetOrdinal("EtapasEjecutadas")),
                DetalleEjecucion = rdr.IsDBNull(rdr.GetOrdinal("DetalleEjecucion"))
                    ? null
                    : rdr.GetString(rdr.GetOrdinal("DetalleEjecucion")),
                TipoEjecucion = rdr.IsDBNull(rdr.GetOrdinal("TipoEjecucion"))
                    ? null
                    : rdr.GetString(rdr.GetOrdinal("TipoEjecucion"))
            };
        }

        /// <summary>
        /// Inserta un registro histórico completo (inicio y fin). Útil para compatibilidad o cargas manuales.
        /// FechaHoraFin puede ser null para registrar solo inicio.
        /// </summary>
        public void Insertar(
            int idTrabajo,
            DateTime fechaHoraInicio,
            DateTime? fechaHoraFin,
            bool exito,
            string mensajeError,
            IReadOnlyList<string> etapasEjecutadas,
            string detalleEjecucion = null,
            string tipoEjecucion = "Scheduler")
        {
            var etapas = etapasEjecutadas != null && etapasEjecutadas.Count > 0
                ? string.Join(",", etapasEjecutadas)
                : null;

            const string sql = @"
                INSERT INTO [dbo].[TrabajosEjecucion] (IdTrabajo, FechaHoraInicio, FechaHoraFin, Exito, MensajeError, EtapasEjecutadas, DetalleEjecucion, TipoEjecucion)
                VALUES (@IdTrabajo, @FechaHoraInicio, @FechaHoraFin, @Exito, @MensajeError, @EtapasEjecutadas, @DetalleEjecucion, @TipoEjecucion)";

            using (var cn = new SqlConnection(_connectionString))
            {
                cn.Open();
                using (var cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.AddWithValue("@IdTrabajo", idTrabajo);
                    cmd.Parameters.AddWithValue("@FechaHoraInicio", fechaHoraInicio);
                    cmd.Parameters.AddWithValue("@FechaHoraFin", (object)fechaHoraFin ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Exito", exito);
                    cmd.Parameters.AddWithValue("@MensajeError", (object)mensajeError ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@EtapasEjecutadas", (object)etapas ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@DetalleEjecucion", (object)detalleEjecucion ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@TipoEjecucion", (object)tipoEjecucion ?? DBNull.Value);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
