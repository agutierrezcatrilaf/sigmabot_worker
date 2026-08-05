using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using SigmabotSync.Domain.Entities;
using SigmabotSync.Infrastructure.Data;

namespace SigmabotSync.Infrastructure.Services.ConfigurationEditor
{
    /// <summary>Lectura del catálogo TiposTrabajo (nombres visibles para configuración).</summary>
    public class TiposTrabajoEditorService
    {
        private readonly string _connectionString;

        public TiposTrabajoEditorService(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public IReadOnlyList<TipoTrabajo> ListarActivos()
        {
            return Listar(soloActivos: true);
        }

        public IReadOnlyList<TipoTrabajo> ListarTodos()
        {
            return Listar(soloActivos: false);
        }

        private IReadOnlyList<TipoTrabajo> Listar(bool soloActivos)
        {
            var sql = @"
                SELECT Codigo, Nombre, Descripcion, Orden, Activo
                FROM TiposTrabajo" + (soloActivos ? " WHERE Activo = 1" : string.Empty) + @"
                ORDER BY Orden, Nombre";

            var lista = new List<TipoTrabajo>();
            using (var cn = new SqlConnection(_connectionString))
            {
                cn.Open();
                using (var cmd = new SqlCommand(sql, cn))
                using (var adapter = new SqlDataAdapter(cmd))
                {
                    var dt = new DataTable();
                    adapter.Fill(dt);
                    foreach (DataRow row in dt.Rows)
                        lista.Add(Map(row));
                }
            }

            return lista;
        }

        private static TipoTrabajo Map(DataRow row)
        {
            return new TipoTrabajo
            {
                Codigo = (row["Codigo"] as string)?.Trim(),
                Nombre = (row["Nombre"] as string)?.Trim(),
                Descripcion = row["Descripcion"] as string,
                Orden = row["Orden"] != DBNull.Value ? Convert.ToInt32(row["Orden"]) : 0,
                Activo = row["Activo"] != DBNull.Value && Convert.ToBoolean(row["Activo"])
            };
        }
    }
}
