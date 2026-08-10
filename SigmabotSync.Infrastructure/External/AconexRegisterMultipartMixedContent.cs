using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SigmabotSync.Infrastructure.External
{
    /// <summary>
    /// multipart/mixed con XML + archivo en base64, escrito por streaming (sin string monolítico).
    /// </summary>
    internal sealed class AconexRegisterMultipartMixedContent : HttpContent
    {
        private readonly string _boundary;
        private readonly string _xmlDocument;
        private readonly string _fileName;
        private readonly string _filePath;

        public AconexRegisterMultipartMixedContent(string boundary, string xmlDocument, string fileName, string filePath)
        {
            if (string.IsNullOrEmpty(boundary))
                throw new ArgumentException("boundary requerido.", nameof(boundary));
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("filePath requerido.", nameof(filePath));

            _boundary = boundary;
            _xmlDocument = xmlDocument ?? "";
            _fileName = string.IsNullOrWhiteSpace(fileName) ? "document" : fileName;
            _filePath = filePath;

            Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("multipart/mixed");
            Headers.ContentType.Parameters.Add(
                new System.Net.Http.Headers.NameValueHeaderValue("boundary", "\"" + boundary + "\""));
        }

        protected override bool TryComputeLength(out long length)
        {
            length = -1;
            return false;
        }

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            await WriteMultipartAsync(stream, CancellationToken.None).ConfigureAwait(false);
        }

        private async Task WriteMultipartAsync(Stream stream, CancellationToken cancellationToken)
        {
            var encoding = Encoding.UTF8;
            var header = new StringBuilder();
            header.Append("--").Append(_boundary).Append("\r\n\r\n");
            header.Append(_xmlDocument).Append("\r\n");
            header.Append("--").Append(_boundary).Append("\r\n");
            header.Append("X-Filename: ").Append(_fileName).Append("\r\n\r\n");
            await stream.WriteAsync(encoding.GetBytes(header.ToString()), cancellationToken).ConfigureAwait(false);

            await MultipartBase64FileWriter.WriteFileAsBase64Async(stream, _filePath, cancellationToken).ConfigureAwait(false);

            var footer = "\r\n\r\n--" + _boundary + "--";
            await stream.WriteAsync(encoding.GetBytes(footer), cancellationToken).ConfigureAwait(false);
        }
    }
}
