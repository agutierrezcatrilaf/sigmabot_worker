using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using SigmabotSync.Domain.Ports;

namespace SigmabotSync.Infrastructure.Services
{
    /// <summary>Persistencia de mails procesados y mapeo DocumentNo+Revision → DocumentId local.</summary>
    public sealed class TransmittalSyncStateService : ITransmittalSyncStatePort
    {
        private readonly string _connectionString;

        public TransmittalSyncStateService(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public async Task<bool> IsMailProcessedAsync(
            int idTrabajo,
            string projectId,
            string mailId,
            CancellationToken cancellationToken = default)
        {
            const string sql = @"
                SELECT COUNT(1)
                FROM TransmittalSyncProcesados
                WHERE IdTrabajo = @IdTrabajo AND ACXProjectId = @ProjectId AND MailId = @MailId";

            using (var cn = new SqlConnection(_connectionString))
            {
                await cn.OpenAsync(cancellationToken).ConfigureAwait(false);
                using (var cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.AddWithValue("@IdTrabajo", idTrabajo);
                    cmd.Parameters.AddWithValue("@ProjectId", projectId ?? "");
                    cmd.Parameters.AddWithValue("@MailId", mailId ?? "");
                    object scalar = await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                    return scalar != null && Convert.ToInt32(scalar) > 0;
                }
            }
        }

        public async Task MarkMailProcessedAsync(
            int idTrabajo,
            string projectId,
            string mailId,
            CancellationToken cancellationToken = default)
        {
            const string sql = @"
                IF NOT EXISTS (
                    SELECT 1 FROM TransmittalSyncProcesados
                    WHERE IdTrabajo = @IdTrabajo AND ACXProjectId = @ProjectId AND MailId = @MailId)
                BEGIN
                    INSERT INTO TransmittalSyncProcesados (IdTrabajo, ACXProjectId, MailId, ProcessedAt)
                    VALUES (@IdTrabajo, @ProjectId, @MailId, SYSUTCDATETIME())
                END";

            using (var cn = new SqlConnection(_connectionString))
            {
                await cn.OpenAsync(cancellationToken).ConfigureAwait(false);
                using (var cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.AddWithValue("@IdTrabajo", idTrabajo);
                    cmd.Parameters.AddWithValue("@ProjectId", projectId ?? "");
                    cmd.Parameters.AddWithValue("@MailId", mailId ?? "");
                    await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                }
            }
        }

        public async Task<string> GetLocalDocumentIdAsync(
            int idTrabajo,
            string projectId,
            string documentNo,
            string revision,
            CancellationToken cancellationToken = default)
        {
            const string sql = @"
                SELECT TOP 1 LocalDocumentId
                FROM TransmittalSyncMapeo
                WHERE IdTrabajo = @IdTrabajo
                  AND ACXProjectId = @ProjectId
                  AND DocumentNo = @DocumentNo
                  AND Revision = @Revision";

            using (var cn = new SqlConnection(_connectionString))
            {
                await cn.OpenAsync(cancellationToken).ConfigureAwait(false);
                using (var cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.AddWithValue("@IdTrabajo", idTrabajo);
                    cmd.Parameters.AddWithValue("@ProjectId", projectId ?? "");
                    cmd.Parameters.AddWithValue("@DocumentNo", documentNo ?? "");
                    cmd.Parameters.AddWithValue("@Revision", revision ?? "");
                    object scalar = await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                    return scalar == null || scalar == DBNull.Value ? null : (scalar as string)?.Trim();
                }
            }
        }

        public async Task SaveLocalDocumentMappingAsync(
            int idTrabajo,
            string projectId,
            string documentNo,
            string revision,
            string localDocumentId,
            CancellationToken cancellationToken = default)
        {
            const string sql = @"
                IF EXISTS (
                    SELECT 1 FROM TransmittalSyncMapeo
                    WHERE IdTrabajo = @IdTrabajo AND ACXProjectId = @ProjectId
                      AND DocumentNo = @DocumentNo AND Revision = @Revision)
                BEGIN
                    UPDATE TransmittalSyncMapeo
                    SET LocalDocumentId = @LocalDocumentId, UpdatedAt = SYSUTCDATETIME()
                    WHERE IdTrabajo = @IdTrabajo AND ACXProjectId = @ProjectId
                      AND DocumentNo = @DocumentNo AND Revision = @Revision
                END
                ELSE
                BEGIN
                    INSERT INTO TransmittalSyncMapeo (IdTrabajo, ACXProjectId, DocumentNo, Revision, LocalDocumentId, UpdatedAt)
                    VALUES (@IdTrabajo, @ProjectId, @DocumentNo, @Revision, @LocalDocumentId, SYSUTCDATETIME())
                END";

            using (var cn = new SqlConnection(_connectionString))
            {
                await cn.OpenAsync(cancellationToken).ConfigureAwait(false);
                using (var cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.AddWithValue("@IdTrabajo", idTrabajo);
                    cmd.Parameters.AddWithValue("@ProjectId", projectId ?? "");
                    cmd.Parameters.AddWithValue("@DocumentNo", documentNo ?? "");
                    cmd.Parameters.AddWithValue("@Revision", revision ?? "");
                    cmd.Parameters.AddWithValue("@LocalDocumentId", localDocumentId ?? "");
                    await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                }
            }
        }

        public async Task<bool> IsSourceDocumentSyncedAsync(
            int idTrabajo,
            string sourceProjectId,
            string sourceDocumentNo,
            string sourceRevision,
            string sourceVersionNumber,
            string sourceDocumentId,
            CancellationToken cancellationToken = default)
        {
            string versionKey = sourceVersionNumber?.Trim() ?? "";
            string docId = sourceDocumentId?.Trim() ?? "";

            string sql;
            if (!string.IsNullOrWhiteSpace(versionKey))
            {
                sql = @"
                    SELECT COUNT(1)
                    FROM TransmittalSyncDocumentProcesados
                    WHERE IdTrabajo = @IdTrabajo
                      AND SourceProjectId = @SourceProjectId
                      AND SourceDocumentNo = @SourceDocumentNo
                      AND SourceRevision = @SourceRevision
                      AND SourceVersionNumber = @SourceVersionNumber";
            }
            else if (!string.IsNullOrWhiteSpace(docId))
            {
                sql = @"
                    SELECT COUNT(1)
                    FROM TransmittalSyncDocumentProcesados
                    WHERE IdTrabajo = @IdTrabajo
                      AND SourceProjectId = @SourceProjectId
                      AND SourceDocumentId = @SourceDocumentId";
            }
            else
            {
                return false;
            }

            using (var cn = new SqlConnection(_connectionString))
            {
                await cn.OpenAsync(cancellationToken).ConfigureAwait(false);
                using (var cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.AddWithValue("@IdTrabajo", idTrabajo);
                    cmd.Parameters.AddWithValue("@SourceProjectId", sourceProjectId ?? "");
                    cmd.Parameters.AddWithValue("@SourceDocumentNo", sourceDocumentNo ?? "");
                    cmd.Parameters.AddWithValue("@SourceRevision", sourceRevision ?? "");
                    if (!string.IsNullOrWhiteSpace(versionKey))
                        cmd.Parameters.AddWithValue("@SourceVersionNumber", versionKey);
                    else
                        cmd.Parameters.AddWithValue("@SourceDocumentId", docId);

                    object scalar = await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                    return scalar != null && Convert.ToInt32(scalar) > 0;
                }
            }
        }

        public async Task MarkSourceDocumentSyncedAsync(
            int idTrabajo,
            string sourceProjectId,
            string sourceDocumentNo,
            string sourceRevision,
            string sourceVersionNumber,
            string sourceDocumentId,
            string destProjectId,
            string destDocumentId,
            string destDocumentNo,
            string destRevision,
            string mailId,
            CancellationToken cancellationToken = default)
        {
            const string sql = @"
                IF NOT EXISTS (
                    SELECT 1 FROM TransmittalSyncDocumentProcesados
                    WHERE IdTrabajo = @IdTrabajo
                      AND SourceProjectId = @SourceProjectId
                      AND SourceDocumentNo = @SourceDocumentNo
                      AND SourceRevision = @SourceRevision
                      AND SourceVersionNumber = @SourceVersionNumber)
                BEGIN
                    INSERT INTO TransmittalSyncDocumentProcesados (
                        IdTrabajo, SourceProjectId, SourceDocumentNo, SourceRevision, SourceVersionNumber,
                        SourceDocumentId, DestProjectId, DestDocumentId, DestDocumentNo, DestRevision, MailId, ProcessedAt)
                    VALUES (
                        @IdTrabajo, @SourceProjectId, @SourceDocumentNo, @SourceRevision, @SourceVersionNumber,
                        @SourceDocumentId, @DestProjectId, @DestDocumentId, @DestDocumentNo, @DestRevision, @MailId, SYSUTCDATETIME())
                END";

            using (var cn = new SqlConnection(_connectionString))
            {
                await cn.OpenAsync(cancellationToken).ConfigureAwait(false);
                using (var cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.AddWithValue("@IdTrabajo", idTrabajo);
                    cmd.Parameters.AddWithValue("@SourceProjectId", sourceProjectId ?? "");
                    cmd.Parameters.AddWithValue("@SourceDocumentNo", sourceDocumentNo ?? "");
                    cmd.Parameters.AddWithValue("@SourceRevision", sourceRevision ?? "");
                    cmd.Parameters.AddWithValue("@SourceVersionNumber", sourceVersionNumber?.Trim() ?? "");
                    cmd.Parameters.AddWithValue("@SourceDocumentId", (object)sourceDocumentId?.Trim() ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@DestProjectId", destProjectId ?? "");
                    cmd.Parameters.AddWithValue("@DestDocumentId", destDocumentId ?? "");
                    cmd.Parameters.AddWithValue("@DestDocumentNo", (object)destDocumentNo?.Trim() ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@DestRevision", (object)destRevision?.Trim() ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@MailId", (object)mailId?.Trim() ?? DBNull.Value);
                    await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }
}
