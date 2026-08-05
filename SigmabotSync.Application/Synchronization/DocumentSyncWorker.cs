using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SigmabotSync.Application.Common;
using SigmabotSync.Domain.Ports;

namespace SigmabotSync.Application.Synchronization
{
    public class DocumentSyncWorker
    {
        private readonly DocumentService _documentService;

        public event Action<int, int> OnProgress;
        public event Action<string> OnStatus;

        public DocumentSyncWorker(DocumentService documentService)
        {
            _documentService = documentService ?? throw new ArgumentNullException(nameof(documentService));
        }

        /// <summary>
        /// Boundary aleatorio por petición (opcional). Para el mismo valor que el curl (<c>myboundary</c>), usar <see cref="AconexRegisterMultipart.ExampleBoundary"/>.
        /// </summary>
        public static string CreateMultipartBoundary() => AconexRegisterMultipart.CreateBoundary();

        /// <summary>
        /// Construye el cuerpo multipart/mixed (XML + X-Filename + base64), igual que Register Document.
        /// </summary>
        public static string BuildRegisterMultipartBody(string xmlDocument, string fileName, string fileBase64, string boundary) =>
            AconexRegisterMultipart.BuildRegisterBody(xmlDocument, fileName, fileBase64, boundary);

        /// <summary>
        /// Envía Supersede a Aconex (POST multipart/mixed). El llamador construye <paramref name="multipartBody"/> y <paramref name="boundary"/> como en Register Document.
        /// </summary>
        public Task<AconexRawHttpResponse> SupersedeDocumentAsync(
            string baseUrl,
            string projectId,
            string documentId,
            string authorizationHeaderBase64,
            string integrationIdOrNull,
            string multipartBody,
            string boundary,
            CancellationToken cancellationToken = default)
        {
            return _documentService.SupersedeDocumentAsync(
                baseUrl,
                projectId,
                documentId,
                authorizationHeaderBase64,
                integrationIdOrNull,
                multipartBody,
                boundary,
                cancellationToken);
        }

        public async Task RunAsync(string projectId, DateTime since)
        {
            OnStatus?.Invoke("Buscando documentos modificados...");

            // 1) Obtener documentos modificados
            var changedDocs = await _documentService.GetChangedDocumentsAsync(projectId, since);

            int total = changedDocs.Count;
            OnStatus?.Invoke($"Documentos modificados encontrados: {total}");

            if (total == 0)
            {
                OnStatus?.Invoke("No hay documentos por sincronizar.");
                return;
            }

            int current = 0;

            // 2) Iterar documentos para procesarlos

            foreach (var doc in changedDocs)
            {
                current++;
                OnProgress?.Invoke(current, total);
                OnStatus?.Invoke($"Procesando documento {current} de {total} (ID={doc.Id})...");

                var metadata = await _documentService.GetDocumentMetadataAsync(projectId, doc.Id);

                // Siguiente paso: string boundary = CreateMultipartBoundary();
                // string body = BuildRegisterMultipartBody(xml, fileName, fileBase64, boundary);
                // await SupersedeDocumentAsync(baseUrl, projectId, doc.Id, auth, integrationId, body, boundary);
            }


            OnStatus?.Invoke("Sincronización finalizada.");
        }
    }

}
