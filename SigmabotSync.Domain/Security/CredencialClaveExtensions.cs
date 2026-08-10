using SigmabotSync.Domain.Entities;

namespace SigmabotSync.Domain.Security
{
    public static class CredencialClaveExtensions
    {
        public static void UnprotectClaves(this Credencial credencial, ICredencialClaveProtector protector)
        {
            if (credencial == null || protector == null || !protector.IsEnabled)
                return;

            credencial.Aconex_Clave = protector.Unprotect(credencial.Aconex_Clave);
            credencial.BD_Clave = protector.Unprotect(credencial.BD_Clave);
        }

        public static void ProtectClaves(this Credencial credencial, ICredencialClaveProtector protector)
        {
            if (credencial == null || protector == null || !protector.IsEnabled)
                return;

            credencial.Aconex_Clave = protector.Protect(credencial.Aconex_Clave);
            credencial.BD_Clave = protector.Protect(credencial.BD_Clave);
        }
    }
}
