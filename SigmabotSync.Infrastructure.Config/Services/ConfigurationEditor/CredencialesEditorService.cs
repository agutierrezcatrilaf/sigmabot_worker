using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using SigmabotSync.Domain.Entities;
using SigmabotSync.Infrastructure.Data;

namespace SigmabotSync.Infrastructure.Services.ConfigurationEditor
{
    /// <summary>
    /// Alta, baja y modificación de la tabla Credenciales (herramienta de configuración).
    /// </summary>
    public class CredencialesEditorService
    {
        private readonly string _connectionString;

        public CredencialesEditorService(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public IReadOnlyList<Credencial> ListarTodas()
        {
            const string sql = @"
                SELECT Id, Nombre, Tipo,
                    Aconex_Instancia, Aconex_Usuario, Aconex_Clave, Aconex_IntegrationId, Aconex_OrganizationId, Aconex_UserId,
                    BD_Servidor, BD_TipoConexion, BD_Usuario, BD_Clave, BD_BaseDatos
                FROM Credenciales
                ORDER BY Id";

            var lista = new List<Credencial>();
            using (var cn = new SqlConnection(_connectionString))
            {
                cn.Open();
                using (var cmd = new SqlCommand(sql, cn))
                using (var adapter = new SqlDataAdapter(cmd))
                {
                    var dt = new DataTable();
                    adapter.Fill(dt);
                    foreach (DataRow row in dt.Rows)
                        lista.Add(row.MapTo<Credencial>());
                }
            }

            return lista;
        }

        public int Insertar(Credencial c)
        {
            const string sql = @"
                INSERT INTO Credenciales (
                    Nombre, Tipo,
                    Aconex_Instancia, Aconex_Usuario, Aconex_Clave, Aconex_IntegrationId, Aconex_OrganizationId, Aconex_UserId,
                    BD_Servidor, BD_TipoConexion, BD_Usuario, BD_Clave, BD_BaseDatos)
                OUTPUT INSERTED.Id
                VALUES (
                    @Nombre, @Tipo,
                    @Aconex_Instancia, @Aconex_Usuario, @Aconex_Clave, @Aconex_IntegrationId, @Aconex_OrganizationId, @Aconex_UserId,
                    @BD_Servidor, @BD_TipoConexion, @BD_Usuario, @BD_Clave, @BD_BaseDatos)";

            using (var cn = new SqlConnection(_connectionString))
            {
                cn.Open();
                using (var cmd = new SqlCommand(sql, cn))
                {
                    AddParameters(cmd, c, includeId: false);
                    var id = cmd.ExecuteScalar();
                    return Convert.ToInt32(id);
                }
            }
        }

        public void Actualizar(Credencial c)
        {
            if (c.Id <= 0)
                throw new ArgumentException("Id inválido para actualizar.", nameof(c));

            const string sql = @"
                UPDATE Credenciales SET
                    Nombre = @Nombre,
                    Tipo = @Tipo,
                    Aconex_Instancia = @Aconex_Instancia,
                    Aconex_Usuario = @Aconex_Usuario,
                    Aconex_Clave = @Aconex_Clave,
                    Aconex_IntegrationId = @Aconex_IntegrationId,
                    Aconex_OrganizationId = @Aconex_OrganizationId,
                    Aconex_UserId = @Aconex_UserId,
                    BD_Servidor = @BD_Servidor,
                    BD_TipoConexion = @BD_TipoConexion,
                    BD_Usuario = @BD_Usuario,
                    BD_Clave = @BD_Clave,
                    BD_BaseDatos = @BD_BaseDatos
                WHERE Id = @Id";

            using (var cn = new SqlConnection(_connectionString))
            {
                cn.Open();
                using (var cmd = new SqlCommand(sql, cn))
                {
                    AddParameters(cmd, c, includeId: true);
                    int n = cmd.ExecuteNonQuery();
                    if (n == 0)
                        throw new InvalidOperationException("No existe Credencial Id=" + c.Id);
                }
            }
        }

        public void Eliminar(int id)
        {
            const string sql = "DELETE FROM Credenciales WHERE Id = @Id";
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

        private static void AddParameters(SqlCommand cmd, Credencial c, bool includeId)
        {
            if (includeId)
                cmd.Parameters.AddWithValue("@Id", c.Id);
            cmd.Parameters.AddWithValue("@Nombre", (object)c.Nombre ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Tipo", c.Tipo ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Aconex_Instancia", (object)c.Aconex_Instancia ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Aconex_Usuario", (object)c.Aconex_Usuario ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Aconex_Clave", (object)c.Aconex_Clave ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Aconex_IntegrationId", (object)c.Aconex_IntegrationId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Aconex_OrganizationId", (object)c.Aconex_OrganizationId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Aconex_UserId", (object)c.Aconex_UserId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@BD_Servidor", (object)c.BD_Servidor ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@BD_TipoConexion", (object)c.BD_TipoConexion ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@BD_Usuario", (object)c.BD_Usuario ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@BD_Clave", (object)c.BD_Clave ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@BD_BaseDatos", (object)c.BD_BaseDatos ?? DBNull.Value);
        }
    }
}
