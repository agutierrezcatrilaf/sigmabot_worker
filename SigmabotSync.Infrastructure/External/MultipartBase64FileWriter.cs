using System;
using System.Buffers;
using System.Buffers.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SigmabotSync.Infrastructure.External
{
    /// <summary>Escribe el contenido de un archivo como base64 UTF-8 en un stream de salida, por bloques.</summary>
    internal static class MultipartBase64FileWriter
    {
        private const int SourceBufferSize = 57 * 1024;

        public static async Task WriteFileAsBase64Async(Stream output, string filePath, CancellationToken cancellationToken)
        {
            if (output == null)
                throw new ArgumentNullException(nameof(output));
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("filePath requerido.", nameof(filePath));

            using (var input = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                81920,
                FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                var buffer = new byte[SourceBufferSize];
                int leftoverCount = 0;

                while (true)
                {
                    int bytesRead = await input.ReadAsync(buffer.AsMemory(leftoverCount), cancellationToken).ConfigureAwait(false);
                    int totalCount = leftoverCount + bytesRead;
                    if (totalCount == 0)
                        break;

                    int byteCount = totalCount / 3 * 3;
                    if (byteCount > 0)
                    {
                        await WriteBase64ChunkAsync(output, buffer, 0, byteCount, cancellationToken).ConfigureAwait(false);
                    }

                    leftoverCount = totalCount - byteCount;
                    if (leftoverCount > 0)
                        Buffer.BlockCopy(buffer, byteCount, buffer, 0, leftoverCount);

                    if (bytesRead == 0)
                    {
                        if (leftoverCount > 0)
                            await WriteBase64ChunkAsync(output, buffer, 0, leftoverCount, cancellationToken).ConfigureAwait(false);
                        break;
                    }
                }
            }
        }

        private static async Task WriteBase64ChunkAsync(
            Stream output,
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken)
        {
            int maxEncoded = Base64.GetMaxEncodedToUtf8Length(count);
            byte[] encoded = ArrayPool<byte>.Shared.Rent(maxEncoded);
            try
            {
                Base64.EncodeToUtf8(buffer.AsSpan(offset, count), encoded, out _, out int written);
                await output.WriteAsync(encoded.AsMemory(0, written), cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(encoded);
            }
        }
    }
}
