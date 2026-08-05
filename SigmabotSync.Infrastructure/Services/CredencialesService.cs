using System;
using System.Data;
using Microsoft.Data.SqlClient;
using SigmabotSync.Domain.Entities;
using SigmabotSync.Infrastructure.Data;

namespace SigmabotSync.Infrastructure.Services
{
    /// <summary>
    /// Lee credenciales desde la tabla Credenciales de la base de datos.
    /// Tipo "Aconex" = credenciales para API Aconex; Tipo "BD" = credenciales para la BD de metadata de documentos.
    /// </summary>
    public class CredencialesService
    {
        private readonly string _connectionString;

        public CredencialesService(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        /// <summary>
        /// Obtiene la credencial por Id (tabla Credenciales). Devuelve null si no existe.
        /// </summary>
        public Credencial GetById(int id)
        {
            const string sql = @"
                SELECT Id, Nombre, Tipo,
                    Aconex_Instancia, Aconex_Usuario, Aconex_Clave, Aconex_IntegrationId, Aconex_OrganizationId, Aconex_UserId,
                    BD_Servidor, BD_TipoConexion, BD_Usuario, BD_Clave, BD_BaseDatos
                FROM Credenciales
                WHERE Id = @Id";

            using (var cn = new SqlConnection(_connectionString))
            {
                cn.Open();
                using (var cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    using (var adapter = new SqlDataAdapter(cmd))
                    {
                        var dt = new DataTable();
                        adapter.Fill(dt);
                        if (dt.Rows.Count == 0)
                            return null;
                        return dt.Rows[0].MapTo<Credencial>();
                    }
                }
            }
        }

        /// <summary>
        /// Obtiene la credencial por tipo (ej. "Aconex" o "BD"). Devuelve null si no existe.
        /// </summary>
        public Credencial GetByTipo(string tipo)
        {
            if (string.IsNullOrWhiteSpace(tipo))
                return null;

            // Comparación sin depender de mayúsculas ni espacios al inicio/fin
            const string sql = @"
                SELECT Id, Nombre, Tipo,
                    Aconex_Instancia, Aconex_Usuario, Aconex_Clave, Aconex_IntegrationId, Aconex_OrganizationId, Aconex_UserId,
                    BD_Servidor, BD_TipoConexion, BD_Usuario, BD_Clave, BD_BaseDatos
                FROM Credenciales
                WHERE LTRIM(RTRIM(Tipo)) = @Tipo";

            using (var cn = new SqlConnection(_connectionString))
            {
                cn.Open();
                using (var cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.AddWithValue("@Tipo", tipo.Trim());
                    using (var adapter = new SqlDataAdapter(cmd))
                    {
                        var dt = new DataTable();
                        adapter.Fill(dt);
                        if (dt.Rows.Count == 0)
                            return null;
                        return dt.Rows[0].MapTo<Credencial>();
                    }
                }
            }
        }
    }
}
