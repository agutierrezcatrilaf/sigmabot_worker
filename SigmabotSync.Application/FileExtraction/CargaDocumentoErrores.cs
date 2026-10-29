using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using SigmabotSync.Application.Common;

namespace SigmabotSync.Application.FileExtraction
{
    internal enum CategoriaErrorCarga
    {
        ArchivoNoEncontrado,
        ErrorAlLeerArchivo,
        DatosRechazados,
        DocumentoYaExiste,
        AconexNoDisponible,
        AccesoRechazado,
        Otro
    }

    internal sealed class ErrorCargaDocumento
    {
        public ErrorCargaDocumento(string archivo, CategoriaErrorCarga categoria, string motivo)
        {
            Archivo = string.IsNullOrWhiteSpace(archivo) ? "sin nombre" : archivo.Trim();
            Categoria = categoria;
            Motivo = string.IsNullOrWhiteSpace(motivo) ? "error no identificado" : motivo.Trim();
        }

        public string Archivo { get; }
        public CategoriaErrorCarga Categoria { get; }
        public string Motivo { get; }
    }

    internal sealed class CargaDocumentoException : Exception
    {
        public CargaDocumentoException(CategoriaErrorCarga categoria, string motivoCliente, string detalle)
            : base(string.IsNullOrWhiteSpace(detalle) ? motivoCliente : detalle)
        {
            Categoria = categoria;
            MotivoCliente = string.IsNullOrWhiteSpace(motivoCliente) ? "error no identificado" : motivoCliente.Trim();
        }

        public CategoriaErrorCarga Categoria { get; }
        public string MotivoCliente { get; }
    }

    internal static class CargaDocumentoErrores
    {
        private const int MaxArchivosPorGrupo = 30;

        public static ErrorCargaDocumento ArchivoNoEncontrado(string archivo, string ruta)
        {
            return new ErrorCargaDocumento(archivo, CategoriaErrorCarga.ArchivoNoEncontrado, DescribirArchivoNoEncontrado(ruta));
        }

        private static readonly Dictionary<string, bool> CarpetaDisponible = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        private static string DescribirArchivoNoEncontrado(string ruta)
        {
            if (string.IsNullOrWhiteSpace(ruta))
                return "no tiene ruta de archivo";

            string path = ruta.Trim();
            string carpeta;
            try
            {
                carpeta = Path.GetDirectoryName(path);
            }
            catch (Exception)
            {
                return "la ruta del archivo no es válida";
            }

            if (string.IsNullOrWhiteSpace(carpeta))
                return "la ruta del archivo no es válida";

            bool disponible;
            if (!CarpetaDisponible.TryGetValue(carpeta, out disponible))
            {
                try
                {
                    disponible = Directory.Exists(carpeta);
                }
                catch (Exception)
                {
                    disponible = false;
                }
                CarpetaDisponible[carpeta] = disponible;
            }

            bool esCompartida = path.StartsWith(@"\\", StringComparison.Ordinal);
            if (!disponible && esCompartida)
                return "no se puede abrir la carpeta compartida " + carpeta;
            if (!disponible)
                return "no existe la carpeta " + carpeta;
            return "el archivo no está en la carpeta " + carpeta;
        }

        public static string Etiqueta(string numeroDocumento, string id, string archivo)
        {
            string archivoTxt = string.IsNullOrWhiteSpace(archivo) ? "sin archivo" : archivo.Trim();
            if (!string.IsNullOrWhiteSpace(numeroDocumento))
                return "N° " + numeroDocumento.Trim() + " | " + archivoTxt;
            string idTxt = string.IsNullOrWhiteSpace(id) ? "sin id" : id.Trim();
            return "Id " + idTxt + " | " + archivoTxt;
        }

        public static CargaDocumentoException DesdeRespuestaHttp(int statusCode, string responseText)
        {
            if (Contiene(responseText, "FIELD_VALUE_ALREADY_EXISTS"))
            {
                return new CargaDocumentoException(
                    CategoriaErrorCarga.DocumentoYaExiste,
                    "el documento ya existe en Aconex",
                    responseText);
            }

            if (TryCampoObligatorio(responseText, out string obligatorio))
            {
                return new CargaDocumentoException(
                    CategoriaErrorCarga.DatosRechazados,
                    "falta el campo obligatorio " + obligatorio,
                    responseText);
            }

            if (TryValorInvalido(responseText, out string invalido))
            {
                return new CargaDocumentoException(
                    CategoriaErrorCarga.DatosRechazados,
                    invalido,
                    responseText);
            }

            if (statusCode == 401 || statusCode == 403)
            {
                return new CargaDocumentoException(
                    CategoriaErrorCarga.AccesoRechazado,
                    "Aconex rechazó el acceso",
                    responseText);
            }

            if (statusCode == 408 || statusCode == 429 || statusCode >= 500)
            {
                return new CargaDocumentoException(
                    CategoriaErrorCarga.AconexNoDisponible,
                    "Aconex no está disponible",
                    responseText);
            }

            if (statusCode == 400)
            {
                return new CargaDocumentoException(
                    CategoriaErrorCarga.DatosRechazados,
                    "Aconex rechazó los datos del documento",
                    responseText);
            }

            return new CargaDocumentoException(
                CategoriaErrorCarga.Otro,
                "Aconex no aceptó el documento",
                responseText);
        }

        public static ErrorCargaDocumento Clasificar(string archivo, Exception ex)
        {
            Exception actual = ex;
            while (actual != null)
            {
                if (actual is CargaDocumentoException carga)
                    return new ErrorCargaDocumento(archivo, carga.Categoria, carga.MotivoCliente);
                if (actual is UnauthorizedAccessException || actual is IOException)
                    return new ErrorCargaDocumento(archivo, CategoriaErrorCarga.ErrorAlLeerArchivo, "no se pudo leer el archivo");
                if (actual is InvalidOperationException && !string.IsNullOrWhiteSpace(actual.Message))
                {
                    ErrorCargaDocumento dato = DesdeMensajeLocal(archivo, actual.Message);
                    if (dato != null)
                        return dato;
                }
                actual = actual.InnerException;
            }

            if (ex is TimeoutException || ex is OperationCanceledException)
                return new ErrorCargaDocumento(archivo, CategoriaErrorCarga.AconexNoDisponible, "Aconex no respondió a tiempo");

            if (ex is HttpRequestException)
                return new ErrorCargaDocumento(archivo, CategoriaErrorCarga.AconexNoDisponible, "no se pudo conectar con Aconex");

            string texto = ex?.ToString() ?? "";
            if (Contiene(texto, "FIELD_VALUE_ALREADY_EXISTS"))
                return new ErrorCargaDocumento(archivo, CategoriaErrorCarga.DocumentoYaExiste, "el documento ya existe en Aconex");

            if (TryCampoObligatorio(texto, out string obligatorio))
                return new ErrorCargaDocumento(archivo, CategoriaErrorCarga.DatosRechazados, "falta el campo obligatorio " + obligatorio);

            if (TryValorInvalido(texto, out string invalido))
                return new ErrorCargaDocumento(archivo, CategoriaErrorCarga.DatosRechazados, invalido);

            if (IndicaAccesoRechazado(texto))
                return new ErrorCargaDocumento(archivo, CategoriaErrorCarga.AccesoRechazado, "Aconex rechazó el acceso");

            if (IndicaAconexCaido(texto))
                return new ErrorCargaDocumento(archivo, CategoriaErrorCarga.AconexNoDisponible, "Aconex no está disponible");

            return new ErrorCargaDocumento(archivo, CategoriaErrorCarga.Otro, "ocurrió un error al enviar el archivo");
        }

        public static void EscribirResumen(
            Action<string, int> log,
            int total,
            int enviados,
            int yaProcesados,
            IReadOnlyList<ErrorCargaDocumento> errores)
        {
            SyncLog.Info(log, $"Se revisaron {total} documentos.");
            SyncLog.Info(log, $"Enviados en esta ejecución: {enviados}.");
            SyncLog.Info(log, $"Ya estaban procesados: {yaProcesados}.");
            SyncLog.Info(log, $"Con error: {errores.Count}.");

            if (errores.Count == 0)
            {
                SyncLog.Info(log, "Resultado: carga completada. Enviados: " + enviados + ". Errores: 0.");
                return;
            }

            SyncLog.Info(log, "");
            foreach (IGrouping<CategoriaErrorCarga, ErrorCargaDocumento> categoria in errores
                .GroupBy(error => error.Categoria)
                .OrderBy(grupo => (int)grupo.Key))
            {
                var motivos = categoria
                    .GroupBy(error => error.Motivo, StringComparer.OrdinalIgnoreCase)
                    .OrderByDescending(grupo => grupo.Count())
                    .ToList();

                if (motivos.Count == 1)
                {
                    string titulo = Titulo(categoria.Key);
                    if (categoria.Key == CategoriaErrorCarga.DatosRechazados
                        || categoria.Key == CategoriaErrorCarga.Otro
                        || categoria.Key == CategoriaErrorCarga.ArchivoNoEncontrado
                        || categoria.Key == CategoriaErrorCarga.ErrorAlLeerArchivo)
                        titulo = titulo + ". " + Capitalizar(motivos[0].Key);
                    EscribirGrupo(log, titulo, motivos[0].Select(error => error.Archivo).ToList());
                    continue;
                }

                SyncLog.Info(log, Titulo(categoria.Key));
                foreach (var motivo in motivos)
                    EscribirGrupo(log, "  " + Capitalizar(motivo.Key), motivo.Select(error => error.Archivo).ToList(), "    ");
            }

            SyncLog.Info(log, "");
            SyncLog.Info(log, "Resultado: carga completada con errores. Enviados: " + enviados + ". Errores: " + errores.Count + ".");
        }

        private static void EscribirGrupo(Action<string, int> log, string titulo, IReadOnlyList<string> archivos, string sangria = "")
        {
            SyncLog.Info(log, $"{titulo} ({archivos.Count})");
            int visibles = Math.Min(archivos.Count, MaxArchivosPorGrupo);
            for (int i = 0; i < visibles; i++)
                SyncLog.Info(log, sangria + "- " + archivos[i]);
            if (archivos.Count > visibles)
                SyncLog.Info(log, sangria + "- y " + (archivos.Count - visibles) + " más");
        }

        private static string Titulo(CategoriaErrorCarga categoria)
        {
            switch (categoria)
            {
                case CategoriaErrorCarga.ArchivoNoEncontrado:
                    return "Archivos no encontrados";
                case CategoriaErrorCarga.ErrorAlLeerArchivo:
                    return "Archivos que no se pudieron leer";
                case CategoriaErrorCarga.DatosRechazados:
                    return "Datos rechazados por Aconex";
                case CategoriaErrorCarga.DocumentoYaExiste:
                    return "Documentos que ya existen en Aconex";
                case CategoriaErrorCarga.AconexNoDisponible:
                    return "Aconex no disponible";
                case CategoriaErrorCarga.AccesoRechazado:
                    return "Aconex rechazó el acceso";
                default:
                    return "Otros errores";
            }
        }

        private static string Capitalizar(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return texto;
            return char.ToUpperInvariant(texto[0]) + texto.Substring(1);
        }

        private static ErrorCargaDocumento DesdeMensajeLocal(string archivo, string mensaje)
        {
            if (TryCampoObligatorio(mensaje, out string obligatorio))
                return new ErrorCargaDocumento(archivo, CategoriaErrorCarga.DatosRechazados, "falta el campo obligatorio " + obligatorio);

            if (TryValorInvalido(mensaje, out string invalido))
                return new ErrorCargaDocumento(archivo, CategoriaErrorCarga.DatosRechazados, invalido);

            const string estado = "No se encontró estado para '";
            const string tipo = "No se encontró tipo de documento para '";
            if (Contiene(mensaje, estado))
                return new ErrorCargaDocumento(archivo, CategoriaErrorCarga.DatosRechazados, "el estado " + ExtraerEntreComillas(mensaje) + " no existe");
            if (Contiene(mensaje, tipo))
                return new ErrorCargaDocumento(archivo, CategoriaErrorCarga.DatosRechazados, "el tipo de documento " + ExtraerEntreComillas(mensaje) + " no existe");
            if (Contiene(mensaje, "no es un DocumentStatusId"))
                return new ErrorCargaDocumento(archivo, CategoriaErrorCarga.DatosRechazados, "el estado no es válido para este proyecto");
            if (Contiene(mensaje, "no es un DocumentTypeId"))
                return new ErrorCargaDocumento(archivo, CategoriaErrorCarga.DatosRechazados, "el tipo de documento no es válido para este proyecto");

            return null;
        }

        private static string ExtraerEntreComillas(string texto)
        {
            int abre = texto.IndexOf('\'');
            if (abre < 0)
                return "";
            int cierra = texto.IndexOf('\'', abre + 1);
            if (cierra <= abre)
                return "";
            return texto.Substring(abre + 1, cierra - abre - 1).Trim();
        }

        private static bool TryCampoObligatorio(string text, out string campo)
        {
            if (TryCampoTrasMarcador(text, "Mandatory field ", out campo))
                return true;
            if (TryCampoTrasMarcador(text, "sin valor: ", out campo))
                return true;
            campo = null;
            return false;
        }

        private static bool TryCampoTrasMarcador(string text, string marker, out string campo)
        {
            campo = null;
            int idx = text?.IndexOf(marker, StringComparison.OrdinalIgnoreCase) ?? -1;
            if (idx < 0)
                return false;

            int start = idx + marker.Length;
            int end = start;
            while (end < text.Length && text[end] != '<' && text[end] != '.' && text[end] != '\r' && text[end] != '\n' && text[end] != ' ')
                end++;

            campo = LimpiarCampo(text.Substring(start, end - start));
            return !string.IsNullOrWhiteSpace(campo);
        }

        private static bool TryValorInvalido(string text, out string motivo)
        {
            motivo = null;
            const string marker = "not valid for field ";
            int idx = text?.IndexOf(marker, StringComparison.OrdinalIgnoreCase) ?? -1;
            if (idx < 0)
                return false;

            int start = idx + marker.Length;
            int end = start;
            while (end < text.Length)
            {
                char c = text[end];
                if (c == '<' || c == '"' || c == '\'' || c == '\r' || c == '\n' || c == '.')
                    break;
                end++;
            }

            string campo = LimpiarCampo(text.Substring(start, end - start));
            if (string.IsNullOrWhiteSpace(campo))
                return false;

            motivo = "el valor del campo " + campo + " no es válido";
            return true;
        }

        private static string LimpiarCampo(string field)
        {
            if (string.IsNullOrWhiteSpace(field))
                return "";
            const string suffix = "_singleSelect";
            string limpio = field.Trim();
            if (limpio.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                limpio = limpio.Substring(0, limpio.Length - suffix.Length);
            return limpio.Trim();
        }

        private static bool IndicaAccesoRechazado(string text)
        {
            return Contiene(text, "Unauthorized")
                || Contiene(text, "Forbidden")
                || Contiene(text, " 401")
                || Contiene(text, " 403")
                || Contiene(text, ": 401")
                || Contiene(text, ": 403");
        }

        private static bool IndicaAconexCaido(string text)
        {
            return Contiene(text, "InternalServerError")
                || Contiene(text, "BadGateway")
                || Contiene(text, "ServiceUnavailable")
                || Contiene(text, "GatewayTimeout")
                || Contiene(text, "RequestTimeout")
                || Contiene(text, "TooManyRequests")
                || Contiene(text, ": 500")
                || Contiene(text, ": 502")
                || Contiene(text, ": 503")
                || Contiene(text, ": 504")
                || Contiene(text, ": 408")
                || Contiene(text, ": 429");
        }

        private static bool Contiene(string text, string marker)
        {
            return !string.IsNullOrEmpty(text)
                && text.IndexOf(marker, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
