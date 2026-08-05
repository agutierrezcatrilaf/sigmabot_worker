using System;
using System.Collections.Generic;
using SigmabotSync.Domain.Entities;

namespace SigmabotSync.Domain.Configuration
{
    /// <summary>
    /// Valida campos obligatorios de credencial según <c>Tipo</c> (Aconex vs BD), alineado con datos reales de la tabla Credenciales.
    /// </summary>
    public static class CredencialRequisitosValidator
    {
        /// <summary>Devuelve mensajes de error vacíos si la credencial cumple los mínimos para su tipo.</summary>
        public static IReadOnlyList<string> ValidarCamposObligatorios(Credencial c)
        {
            var errores = new List<string>();
            if (c == null)
            {
                errores.Add("La credencial no puede ser nula.");
                return errores;
            }

            var tipo = (c.Tipo ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(c.Nombre))
                errores.Add("Nombre es obligatorio.");

            if (string.IsNullOrEmpty(tipo))
            {
                errores.Add("Tipo es obligatorio (Aconex o BD).");
                return errores;
            }

            if (tipo.Equals(CredencialTipoIds.Aconex, StringComparison.OrdinalIgnoreCase))
                ValidarAconex(c, errores);
            else if (tipo.Equals(CredencialTipoIds.BD, StringComparison.OrdinalIgnoreCase))
                ValidarBd(c, errores);
            else
                errores.Add("Tipo no reconocido: use \"" + CredencialTipoIds.Aconex + "\" o \"" + CredencialTipoIds.BD + "\".");

            return errores;
        }

        private static void ValidarAconex(Credencial c, List<string> errores)
        {
            if (string.IsNullOrWhiteSpace(c.Aconex_Instancia))
                errores.Add("Aconex: instancia (host) es obligatoria.");
            if (string.IsNullOrWhiteSpace(c.Aconex_Usuario))
                errores.Add("Aconex: usuario es obligatorio.");
            if (string.IsNullOrWhiteSpace(c.Aconex_Clave))
                errores.Add("Aconex: clave es obligatoria.");
            if (string.IsNullOrWhiteSpace(c.Aconex_IntegrationId))
                errores.Add("Aconex: Integration Id es obligatorio.");
            if (string.IsNullOrWhiteSpace(c.Aconex_OrganizationId))
                errores.Add("Aconex: Organization Id es obligatorio.");
            if (string.IsNullOrWhiteSpace(c.Aconex_UserId))
                errores.Add("Aconex: User Id es obligatorio.");
        }

        private static void ValidarBd(Credencial c, List<string> errores)
        {
            if (string.IsNullOrWhiteSpace(c.BD_Servidor))
                errores.Add("BD: servidor es obligatorio.");
            if (string.IsNullOrWhiteSpace(c.BD_TipoConexion))
                errores.Add("BD: tipo de conexión es obligatorio (ej. SQL).");
            if (string.IsNullOrWhiteSpace(c.BD_Usuario))
                errores.Add("BD: usuario es obligatorio.");
            if (string.IsNullOrWhiteSpace(c.BD_Clave))
                errores.Add("BD: clave es obligatoria.");
            if (string.IsNullOrWhiteSpace(c.BD_BaseDatos))
                errores.Add("BD: nombre de base de datos es obligatorio.");
        }
    }
}
