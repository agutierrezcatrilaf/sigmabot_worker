using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SigmabotSync.Application.Common;
using SigmabotSync.Application.FileExtraction;
using SigmabotSync.Domain.Configuration;
using SigmabotSync.Domain.Entities;
using SigmabotSync.Domain.Models;
using SigmabotSync.Domain.Models.Extraction;
using SigmabotSync.Domain.Models.Synchronization;
using SigmabotSync.Domain.Ports;

namespace SigmabotSync.Application.Synchronization
{
    public sealed class TransmittalSyncService
    {
        /// <summary>Bandeja por defecto (lado 1 / Codelco → destino).</summary>
        private const string MailboxInbox = "inbox";
        /// <summary>Bandeja cuando el origen es lado 2 (SALFA): transmitals enviados.</summary>
        private const string MailboxSentbox = "sentbox";
        /// <summary>Transmisión estándar Codelco (inbox).</summary>
        private const int CorrTypeIdCodelco = 23;
        /// <summary>Transmittal de respuesta/archivos SALFA (sentbox).</summary>
        private const int CorrTypeIdSalfa = 400;
        /// <summary>Prueba SALFA→Codelco: máx. adjuntos por pasada (0 = sin límite).</summary>
        private const int SalfaToCodelcoTestMaxDocuments = 0;
        /// <summary>Prueba Codelco→SALFA (ida): máx. adjuntos por pasada (0 = sin límite).</summary>
        private const int CodelcoToSalfaTestMaxDocuments = 0;
        /// <summary>Campo en SALFA con el DocumentNumber de Codelco (clave de unión).</summary>
        private const string CodelcoBridgeField = "CdigoCodelco_singleLineText";

        private readonly IMailTransmittalReadPort _mailRead;
        private readonly IAconexRegisterWritePort _registerWrite;
        private readonly IAconexRegisterDocumentContentPort _documentContent;
        private readonly IAconexRegisterSearchPort _registerSearch;
        private readonly IAconexRegisterMetadataPort _registerMetadata;
        private readonly ITransmittalSyncFieldMapPort _fieldMap;
        private readonly ITransmittalSyncStatePort _state;
        private readonly IAconexDocumentCatalogPort _documentCatalog;

        public TransmittalSyncService(
            IMailTransmittalReadPort mailRead,
            IAconexRegisterWritePort registerWrite,
            IAconexRegisterDocumentContentPort documentContent,
            IAconexRegisterSearchPort registerSearch,
            IAconexRegisterMetadataPort registerMetadata,
            ITransmittalSyncFieldMapPort fieldMap,
            ITransmittalSyncStatePort state,
            IAconexDocumentCatalogPort documentCatalog)
        {
            _mailRead = mailRead ?? throw new ArgumentNullException(nameof(mailRead));
            _registerWrite = registerWrite ?? throw new ArgumentNullException(nameof(registerWrite));
            _documentContent = documentContent ?? throw new ArgumentNullException(nameof(documentContent));
            _registerSearch = registerSearch ?? throw new ArgumentNullException(nameof(registerSearch));
            _registerMetadata = registerMetadata ?? throw new ArgumentNullException(nameof(registerMetadata));
            _fieldMap = fieldMap ?? throw new ArgumentNullException(nameof(fieldMap));
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _documentCatalog = documentCatalog ?? throw new ArgumentNullException(nameof(documentCatalog));
        }

        /// <summary>
        /// Lee transmittals del proyecto origen (<see cref="SourceMailbox"/>) y crea/actualiza documentos en el registro del proyecto destino.
        /// Los archivos se descargan del registro del origen.
        /// </summary>
        public async Task<TransmittalSyncProjectResult> ProcessCrossProjectAsync(
            TransmittalSyncRunRequest request,
            ProyectoSyncItem sourceProject,
            ProyectoSyncItem targetProject,
            Action<string> log,
            CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (sourceProject == null) throw new ArgumentNullException(nameof(sourceProject));
            if (targetProject == null) throw new ArgumentNullException(nameof(targetProject));

            DateTime hastaUtc = DateTime.UtcNow;
            DateTime desdeUtc = hastaUtc.AddDays(-Math.Max(1, request.DiasLookback));

            string fechaInicio = desdeUtc.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
            string fechaFin = hastaUtc.ToString("yyyyMMdd", CultureInfo.InvariantCulture);

            string mailbox = ResolveSourceMailbox(request, sourceProject);
            int corrTypeId = ResolveSourceCorrTypeId(request, sourceProject);

            log?.Invoke(
                $"Buscando transmitals {mailbox} (corrtypeid:{corrTypeId}) origen {sourceProject.Label} ({sourceProject.ProjectId}) " +
                $"→ registro destino {targetProject.Label} ({targetProject.ProjectId}), " +
                $"sentdate:[{fechaInicio} TO {fechaFin}] ({request.DiasLookback} días lookback)...");

            var mails = await _mailRead.ListTransmittalsAsync(
                request.BaseUrl,
                sourceProject.ProjectId,
                request.AuthorizationHeaderBase64,
                desdeUtc,
                hastaUtc,
                mailbox,
                corrTypeId,
                cancellationToken).ConfigureAwait(false);

            int totalListados = mails.Count;
            mails = FilterMailsBySubjectForVuelta(request, sourceProject, mails, log);

            var result = new TransmittalSyncProjectResult
            {
                SourceProjectId = sourceProject.ProjectId,
                TargetProjectId = targetProject.ProjectId,
                Mailbox = mailbox,
                TotalMails = mails.Count,
                SkippedSubjectFilter = Math.Max(0, totalListados - mails.Count)
            };
            if (totalListados != mails.Count)
                log?.Invoke(
                    $"Transmitals en {mailbox}: {totalListados} listados, {mails.Count} tras filtro subject " +
                    $"(omitidos {result.SkippedSubjectFilter}).");
            else
                log?.Invoke($"Transmitals encontrados en {mailbox} origen: {mails.Count}");
            if (mails.Count == 0)
            {
                log?.Invoke(
                    $"Sin resultados. Verifique DiasLookbackTransmittal (actual={request.DiasLookback}) " +
                    $"y que el transmittal esté en {mailbox} del proyecto {sourceProject.ProjectId} " +
                    $"con corrtypeid:{corrTypeId} y sentdate entre {fechaInicio} y {fechaFin}.");
                return result;
            }

            IReadOnlyList<TransmittalSyncCampoMapeoItem> fieldMappings;
            AconexRegisterSchemaSnapshot targetSchema = null;
            AconexDocumentCatalog documentCatalog = AconexDocumentCatalog.Empty;
            try
            {
                fieldMappings = await _fieldMap.GetMappingsAsync(
                    request.IdTrabajo, sourceProject.ProjectId, targetProject.ProjectId, cancellationToken).ConfigureAwait(false);

                if (fieldMappings.Count > 0)
                {
                    documentCatalog = await _documentCatalog.LoadCatalogAsync(
                        request.IdTrabajo, sourceProject.ProjectId, targetProject.ProjectId, cancellationToken).ConfigureAwait(false);
                    targetSchema = await LoadTargetRegisterSchemaAsync(request, targetProject, cancellationToken).ConfigureAwait(false);
                    log?.Invoke(
                        $"Homologación ({sourceProject.ProjectId} → {targetProject.ProjectId}): " +
                        $"{fieldMappings.Count} campos + schema destino ({targetSchema.Fields?.Count ?? 0} campos).");
                    log?.Invoke(
                        $"  Catálogos BD: TiposDocumentos={documentCatalog.IdTipoPorNombre.Count}, " +
                        $"EstatusDocumentos={documentCatalog.IdEstatusPorNombre.Count}, " +
                        $"EquivDiscipline={documentCatalog.EquivalenciaDiscipline.Count}, " +
                        $"EquivTipoDoc={documentCatalog.EquivalenciaTipoDocumento.Count}, " +
                        $"EquivCwa={documentCatalog.EquivalenciaCwa.Count}");
                    foreach (var m in fieldMappings)
                        log?.Invoke($"    {m.CampoOrigen} → {m.CampoDestino}{(m.EsObligatorio ? " [oblig]" : "")}{(string.IsNullOrWhiteSpace(m.Catalogo) ? "" : $" cat={m.Catalogo}")}{(string.IsNullOrWhiteSpace(m.ValorDefault) ? "" : $" default={m.ValorDefault}")}");
                }
                else
                {
                    log?.Invoke("  AVISO: sin filas en TransmittalSyncCampoProyecto; se usará GET register/schema (modo legacy).");
                    targetSchema = await LoadTargetRegisterSchemaAsync(request, targetProject, cancellationToken).ConfigureAwait(false);
                    log?.Invoke($"Schema registro destino ({targetProject.Label}): {targetSchema.Fields?.Count ?? 0} campos.");
                }
            }
            catch (Exception ex)
            {
                result.Errors++;
                log?.Invoke($"ERROR preparando registro destino {targetProject.ProjectId}: {ex.Message}");
                return result;
            }

            int docsProcessedThisPass = 0;

            foreach (var mail in mails)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (string.IsNullOrWhiteSpace(mail.MailId))
                    continue;

                if (await _state.IsMailProcessedAsync(request.IdTrabajo, sourceProject.ProjectId, mail.MailId, cancellationToken).ConfigureAwait(false))
                {
                    result.SkippedAlreadyProcessed++;
                    continue;
                }

                try
                {
                    var detail = await _mailRead.GetTransmittalDetailAsync(
                        request.BaseUrl,
                        sourceProject.ProjectId,
                        mail.MailId,
                        request.AuthorizationHeaderBase64,
                        cancellationToken).ConfigureAwait(false);

                    if (detail.Attachments == null || detail.Attachments.Count == 0)
                    {
                        log?.Invoke($"Mail {mail.MailNo} ({mail.MailId}): sin adjuntos registrados.");
                        await _state.MarkMailProcessedAsync(request.IdTrabajo, sourceProject.ProjectId, mail.MailId, cancellationToken).ConfigureAwait(false);
                        result.ProcessedMails++;
                        continue;
                    }

                    IReadOnlyDictionary<string, string> mailHints = BuildMailHints(mail, detail);
                    int maxDocsThisPass = ResolveMaxDocumentsPerPass(request, sourceProject);
                    if (maxDocsThisPass > 0)
                        log?.Invoke($"  [Prueba ProjectSync] límite {maxDocsThisPass} documento(s) en esta pasada.");

                    bool stopPassAfterLimit = false;
                    foreach (var attachment in detail.Attachments)
                    {
                        if (maxDocsThisPass > 0 && docsProcessedThisPass >= maxDocsThisPass)
                        {
                            log?.Invoke(
                                "  [Prueba ProjectSync] límite alcanzado; mail NO marcado como procesado " +
                                "(puede re-ejecutar para el resto).");
                            stopPassAfterLimit = true;
                            break;
                        }

                        if (attachment.IsPlaceholder)
                        {
                            bool ok = await TryRegisterPlaceholderAsync(
                                request, sourceProject, targetProject, targetSchema, documentCatalog, fieldMappings, attachment, mail.MailId, mailHints, log, cancellationToken).ConfigureAwait(false);
                            if (ok) result.PlaceholdersCreated++;
                        }
                        else
                        {
                            bool ok = await TryApplyFileFromTransmittalAsync(
                                request, sourceProject, targetProject, targetSchema, documentCatalog, fieldMappings, attachment, mail.MailId, mailHints, log, cancellationToken).ConfigureAwait(false);
                            if (ok) result.FilesApplied++;
                        }

                        docsProcessedThisPass++;
                    }

                    if (stopPassAfterLimit)
                        break;

                    await _state.MarkMailProcessedAsync(request.IdTrabajo, sourceProject.ProjectId, mail.MailId, cancellationToken).ConfigureAwait(false);
                    result.ProcessedMails++;
                }
                catch (Exception ex)
                {
                    result.Errors++;
                    log?.Invoke($"ERROR mail {mail.MailId}: {ex.Message}");
                }
            }

            return result;
        }

        private static IReadOnlyDictionary<string, string> BuildMailHints(
            TransmittalMailSummary mail,
            TransmittalMailDetail detail)
        {
            var hints = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            string mailNo = FirstNonEmpty(detail?.MailNo, mail?.MailNo);
            if (!string.IsNullOrWhiteSpace(mailNo))
            {
                hints["MailNo"] = mailNo;
                hints["mailno"] = mailNo;
            }

            string mailId = FirstNonEmpty(detail?.MailId, mail?.MailId);
            if (!string.IsNullOrWhiteSpace(mailId))
                hints["MailId"] = mailId;

            string subject = FirstNonEmpty(detail?.Subject, mail?.Subject);
            if (!string.IsNullOrWhiteSpace(subject))
                hints["Subject"] = subject;

            string reference = mail?.ReferenceNumber?.Trim();
            if (!string.IsNullOrWhiteSpace(reference))
                hints["ReferenceNumber"] = reference;

            return hints;
        }

        private static string FirstNonEmpty(params string[] values)
        {
            if (values == null)
                return null;
            foreach (string v in values)
            {
                if (!string.IsNullOrWhiteSpace(v))
                    return v.Trim();
            }
            return null;
        }

        /// <summary>
        /// Lado 1 (IdProyecto / Codelco): inbox. Lado 2 (IdProyecto2 / SALFA): sentbox.
        /// </summary>
        private static string ResolveSourceMailbox(TransmittalSyncRunRequest request, ProyectoSyncItem source)
        {
            return IsLado2Source(request, source) ? MailboxSentbox : MailboxInbox;
        }

        /// <summary>Lado 1: corrtypeid 23. Lado 2 (SALFA sentbox): corrtypeid 400.</summary>
        private static int ResolveSourceCorrTypeId(TransmittalSyncRunRequest request, ProyectoSyncItem source)
        {
            return IsLado2Source(request, source) ? CorrTypeIdSalfa : CorrTypeIdCodelco;
        }

        private static int ResolveMaxDocumentsPerPass(TransmittalSyncRunRequest request, ProyectoSyncItem source)
        {
            if (IsLado2Source(request, source))
                return SalfaToCodelcoTestMaxDocuments;
            return CodelcoToSalfaTestMaxDocuments;
        }

        private static bool IsLado2Source(TransmittalSyncRunRequest request, ProyectoSyncItem source)
        {
            if (request?.Proyectos == null || request.Proyectos.Count < 2 || source == null)
                return false;
            string lado2 = request.Proyectos[1]?.ProjectId?.Trim();
            return !string.IsNullOrWhiteSpace(lado2) &&
                   string.Equals(source.ProjectId?.Trim(), lado2, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Ida Codelco→SALFA: Aconex asigna DocumentNumber vía &lt;AutoNumber&gt;true&lt;/AutoNumber&gt;.</summary>
        private static bool ShouldUseSalfaAutoDocumentNumber(TransmittalSyncRunRequest request, ProyectoSyncItem sourceProject)
        {
            return !IsLado2Source(request, sourceProject);
        }

        private static string ResolveAssignedDocumentNumber(string responseText, string expectedDocumentNo)
        {
            string assigned = AconexRegisterResponseParser.ParseRegisterDocumentNumber(responseText);
            if (!string.IsNullOrWhiteSpace(assigned))
                return assigned.Trim();
            return string.IsNullOrWhiteSpace(expectedDocumentNo) ? null : expectedDocumentNo.Trim();
        }

        /// <summary>
        /// Vuelta SALFA→Codelco: solo transmitals cuyo Subject contenga
        /// <see cref="TransmittalSyncRunRequest.SubjectFiltroTransmittalVuelta"/>.
        /// </summary>
        private static IReadOnlyList<TransmittalMailSummary> FilterMailsBySubjectForVuelta(
            TransmittalSyncRunRequest request,
            ProyectoSyncItem sourceProject,
            IReadOnlyList<TransmittalMailSummary> mails,
            Action<string> log)
        {
            if (mails == null || mails.Count == 0)
                return mails ?? Array.Empty<TransmittalMailSummary>();

            if (!IsLado2Source(request, sourceProject))
                return mails;

            string filter = request?.SubjectFiltroTransmittalVuelta?.Trim();
            if (string.IsNullOrWhiteSpace(filter))
                return mails;

            var filtered = new List<TransmittalMailSummary>(mails.Count);
            foreach (TransmittalMailSummary mail in mails)
            {
                string subject = mail?.Subject ?? string.Empty;
                if (subject.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    filtered.Add(mail);
                    continue;
                }

                log?.Invoke(
                    $"  Omitido (subject sin «{filter}»): {mail?.MailNo ?? "?"} — {Truncate(subject, 120)}");
            }

            return filtered;
        }

        /// <summary>
        /// Clave de mapeo local / búsqueda: docno Codelco en ida (AutoNumber); docno destino en vuelta.
        /// </summary>
        private static string ResolveDestinationDocumentKey(
            TransmittalSyncRunRequest request,
            ProyectoSyncItem sourceProject,
            AconexDocumentCatalog documentCatalog,
            IReadOnlyList<TransmittalSyncCampoMapeoItem> fieldMappings,
            TransmittalDocumentAttachment attachment,
            IReadOnlyDictionary<string, string> sourceHints)
        {
            if (ShouldUseSalfaAutoDocumentNumber(request, sourceProject))
                return attachment?.DocumentNo?.Trim();
            return ResolveDestinationDocumentNo(documentCatalog, fieldMappings, attachment, sourceHints);
        }

        /// <summary>DocumentNumber en destino para vuelta SALFA→Codelco (match/supersede).</summary>
        private static string ResolveDestinationDocumentNo(
            AconexDocumentCatalog documentCatalog,
            IReadOnlyList<TransmittalSyncCampoMapeoItem> fieldMappings,
            TransmittalDocumentAttachment attachment,
            IReadOnlyDictionary<string, string> sourceHints)
        {
            if (fieldMappings != null)
            {
                foreach (TransmittalSyncCampoMapeoItem map in fieldMappings)
                {
                    if (map == null || string.IsNullOrWhiteSpace(map.CampoDestino))
                        continue;
                    string dest = map.CampoDestino.Trim();
                    if (!string.Equals(dest, "DocumentNumber", StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(dest, "docno", StringComparison.OrdinalIgnoreCase))
                        continue;

                    string fromOrigen = GetHintValue(sourceHints, map.CampoOrigen);
                    if (!string.IsNullOrWhiteSpace(fromOrigen))
                        return fromOrigen.Trim();
                }
            }

            string bridge = GetHintValue(sourceHints, CodelcoBridgeField);
            if (!string.IsNullOrWhiteSpace(bridge))
                return bridge.Trim();

            return attachment?.DocumentNo?.Trim();
        }

        private async Task SaveTargetDocumentMappingsAsync(
            int idTrabajo,
            string targetProjectId,
            string revision,
            string localDocumentId,
            string primaryMappingKey,
            string assignedDestDocNo,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(localDocumentId))
                return;

            string effectiveRevision = revision ?? "";
            var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(primaryMappingKey))
                keys.Add(primaryMappingKey.Trim());
            if (!string.IsNullOrWhiteSpace(assignedDestDocNo))
                keys.Add(assignedDestDocNo.Trim());

            foreach (string key in keys)
            {
                await _state.SaveLocalDocumentMappingAsync(
                    idTrabajo, targetProjectId, key, effectiveRevision, localDocumentId, cancellationToken).ConfigureAwait(false);
            }
        }

        private static string ResolveSupersedeDestinationDocumentNo(
            TargetDocumentLookup existing,
            string mappingKey)
        {
            string fromHints = GetHintValue(existing?.RegisterHints, "DocumentNumber");
            if (!string.IsNullOrWhiteSpace(fromHints))
                return fromHints.Trim();
            return mappingKey;
        }

        private static string GetHintValue(IReadOnlyDictionary<string, string> hints, params string[] keys)
        {
            if (hints == null || keys == null)
                return null;
            foreach (string key in keys)
            {
                if (string.IsNullOrWhiteSpace(key))
                    continue;
                string v = GetHintValue(hints, key);
                if (!string.IsNullOrWhiteSpace(v))
                    return v;
            }
            return null;
        }

        private static string GetHintValue(IReadOnlyDictionary<string, string> hints, string key)
        {
            if (hints == null || string.IsNullOrWhiteSpace(key))
                return null;
            return hints.TryGetValue(key.Trim(), out string v) && !string.IsNullOrWhiteSpace(v) ? v.Trim() : null;
        }

        private async Task<bool> TryRegisterPlaceholderAsync(
            TransmittalSyncRunRequest request,
            ProyectoSyncItem sourceProject,
            ProyectoSyncItem targetProject,
            AconexRegisterSchemaSnapshot targetSchema,
            AconexDocumentCatalog documentCatalog,
            IReadOnlyList<TransmittalSyncCampoMapeoItem> fieldMappings,
            TransmittalDocumentAttachment attachment,
            string mailId,
            IReadOnlyDictionary<string, string> mailHints,
            Action<string> log,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(attachment.DocumentNo))
            {
                log?.Invoke("Marcador omitido: DocumentNo vacío.");
                return false;
            }

            string revision = string.IsNullOrWhiteSpace(attachment.Revision) ? "A" : attachment.Revision.Trim();
            if (await IsSourceAttachmentAlreadySyncedAsync(
                request, sourceProject, attachment, revision, log, cancellationToken).ConfigureAwait(false))
                return false;

            IReadOnlyDictionary<string, string> sourceHints = await FetchSourceDocumentHintsAsync(
                request, sourceProject, attachment, revision, fieldMappings, mailHints, log, cancellationToken,
                skipMetadata: IsLado2Source(request, sourceProject)).ConfigureAwait(false);

            string mappingKey = ResolveDestinationDocumentKey(
                request, sourceProject, documentCatalog, fieldMappings, attachment, sourceHints);
            if (string.IsNullOrWhiteSpace(mappingKey))
            {
                log?.Invoke($"Marcador omitido ({attachment.DocumentNo}): sin clave de documento destino.");
                return false;
            }

            string codelcoBridgeKey = IsLado2Source(request, sourceProject) ? null : attachment.DocumentNo?.Trim();
            TargetDocumentLookup existing = await ResolveTargetDocumentWithHintsAsync(
                request, targetProject, targetSchema, mappingKey, revision, codelcoBridgeKey, log, cancellationToken).ConfigureAwait(false);

            if (existing != null && !string.IsNullOrWhiteSpace(existing.DocumentId))
            {
                string destDocNoForLog = ResolveSupersedeDestinationDocumentNo(existing, mappingKey);
                log?.Invoke(
                    $"Supersede marcador en {targetProject.Label}: {destDocNoForLog} " +
                    $"rev {existing.Revision ?? "?"} → {revision} (id={existing.DocumentId})");
                bool superseded = await SupersedeDocumentAsync(
                    request, sourceProject, targetProject, targetSchema, documentCatalog, fieldMappings,
                    attachment, revision, destDocNoForLog, existing.DocumentId, fileName: null, fileBase64: null, hasFile: false,
                    mailHints, sourceHints, existing.RegisterHints, mailId, log, cancellationToken).ConfigureAwait(false);
                return superseded;
            }

            string xml = await BuildRegisterXmlAsync(
                request, sourceProject, targetProject, targetSchema, documentCatalog, fieldMappings, attachment, revision, false, mailHints, sourceHints, log, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(xml))
                return false;

            string boundary = AconexRegisterMultipart.CreateBoundary();
            string body = AconexRegisterMultipart.BuildRegisterBodyXmlOnly(xml, boundary);

            LogRegisterHttpDebug(request, targetProject, body, boundary, xml, "marcador " + mappingKey, log);

            var response = await _registerWrite.PostRegisterDocumentAsync(
                request.BaseUrl,
                targetProject.ProjectId,
                request.AuthorizationHeaderBase64,
                request.IntegrationId,
                body,
                boundary,
                cancellationToken).ConfigureAwait(false);

            string responseText = response?.Body ?? "";
            if (response == null || !response.IsSuccessStatusCode)
            {
                log?.Invoke($"Register marcador falló ({mappingKey}): {Truncate(responseText, 300)}");
                return false;
            }

            string localDocumentId = AconexRegisterResponseParser.ParseRegisterDocumentId(responseText);
            if (string.IsNullOrWhiteSpace(localDocumentId))
            {
                log?.Invoke($"Register marcador sin DocumentId en respuesta ({mappingKey}).");
                return false;
            }

            string assignedDocNo = ResolveAssignedDocumentNumber(responseText, null);
            await SaveTargetDocumentMappingsAsync(
                request.IdTrabajo, targetProject.ProjectId, revision, localDocumentId, mappingKey, assignedDocNo,
                cancellationToken).ConfigureAwait(false);
            await MarkSourceAttachmentSyncedAsync(
                request, sourceProject, targetProject, attachment, revision, mailId,
                localDocumentId, assignedDocNo ?? mappingKey, cancellationToken).ConfigureAwait(false);

            log?.Invoke(
                $"Marcador creado en destino: Codelco {mappingKey} → Salfa {assignedDocNo ?? "?"} rev {revision} → {localDocumentId}");
            return true;
        }

        private async Task<bool> TryApplyFileFromTransmittalAsync(
            TransmittalSyncRunRequest request,
            ProyectoSyncItem sourceProject,
            ProyectoSyncItem targetProject,
            AconexRegisterSchemaSnapshot targetSchema,
            AconexDocumentCatalog documentCatalog,
            IReadOnlyList<TransmittalSyncCampoMapeoItem> fieldMappings,
            TransmittalDocumentAttachment attachment,
            string mailId,
            IReadOnlyDictionary<string, string> mailHints,
            Action<string> log,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(attachment.DocumentNo))
            {
                log?.Invoke("Archivo omitido: DocumentNo vacío.");
                return false;
            }

            string revision = string.IsNullOrWhiteSpace(attachment.Revision) ? "A" : attachment.Revision.Trim();
            if (await IsSourceAttachmentAlreadySyncedAsync(
                request, sourceProject, attachment, revision, log, cancellationToken).ConfigureAwait(false))
                return false;

            string sourceDocumentId = ResolveSourceDocumentId(attachment);
            if (string.IsNullOrWhiteSpace(sourceDocumentId))
            {
                log?.Invoke($"Archivo omitido ({attachment.DocumentNo}): sin DocumentId/RegisteredAs en transmittal.");
                return false;
            }

            IReadOnlyDictionary<string, string> sourceHints = await FetchSourceDocumentHintsAsync(
                request, sourceProject, attachment, revision, fieldMappings, mailHints, log, cancellationToken,
                skipMetadata: IsLado2Source(request, sourceProject)).ConfigureAwait(false);

            string mappingKey = ResolveDestinationDocumentKey(
                request, sourceProject, documentCatalog, fieldMappings, attachment, sourceHints);
            if (string.IsNullOrWhiteSpace(mappingKey))
            {
                log?.Invoke($"Archivo omitido ({attachment.DocumentNo}): sin clave de documento destino.");
                return false;
            }

            string codelcoBridgeKey = IsLado2Source(request, sourceProject) ? null : attachment.DocumentNo?.Trim();
            TargetDocumentLookup existing = await ResolveTargetDocumentWithHintsAsync(
                request, targetProject, targetSchema, mappingKey, revision, codelcoBridgeKey, log, cancellationToken).ConfigureAwait(false);

            string tempFile = Path.Combine(Path.GetTempPath(), "sigmabot_sync_" + Guid.NewGuid().ToString("N") + Path.GetExtension(attachment.FileName ?? ".bin"));
            try
            {
                var download = await _documentContent.DownloadToFileAsync(
                    request.BaseUrl,
                    sourceProject.ProjectId,
                    sourceDocumentId,
                    tempFile,
                    request.AuthorizationHeaderBase64,
                    cancellationToken).ConfigureAwait(false);

                if (download.Status == AconexRegisterDocumentDownloadStatus.OmittedEmptyDocument)
                {
                    log?.Invoke($"Descarga vacía ({attachment.DocumentNo}): documento sin archivo en registro origen.");
                    return false;
                }

                if (download.Status != AconexRegisterDocumentDownloadStatus.Saved || !File.Exists(tempFile))
                {
                    log?.Invoke($"Descarga falló ({attachment.DocumentNo}): {download.Message ?? download.Status.ToString()}");
                    return false;
                }

                byte[] bytes = File.ReadAllBytes(tempFile);
                string fileBase64 = Convert.ToBase64String(bytes);
                string fileName = string.IsNullOrWhiteSpace(attachment.FileName) ? attachment.DocumentNo + ".bin" : attachment.FileName;

                if (existing == null || string.IsNullOrWhiteSpace(existing.DocumentId))
                {
                    return await RegisterWithFileAsync(
                        request, sourceProject, targetProject, targetSchema, documentCatalog, fieldMappings,
                        attachment, revision, mappingKey, fileName, fileBase64, mailId, mailHints, sourceHints, log, cancellationToken).ConfigureAwait(false);
                }

                string destDocNoForLog = ResolveSupersedeDestinationDocumentNo(existing, mappingKey);
                log?.Invoke(
                    $"Supersede con archivo en {targetProject.Label}: {destDocNoForLog} " +
                    $"rev {existing.Revision ?? "?"} → {revision} (id={existing.DocumentId})");
                return await SupersedeDocumentAsync(
                    request, sourceProject, targetProject, targetSchema, documentCatalog, fieldMappings,
                    attachment, revision, destDocNoForLog, existing.DocumentId, fileName, fileBase64, hasFile: true,
                    mailHints, sourceHints, existing.RegisterHints, mailId, log, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                TryDeleteFile(tempFile);
            }
        }

        private async Task<bool> RegisterWithFileAsync(
            TransmittalSyncRunRequest request,
            ProyectoSyncItem sourceProject,
            ProyectoSyncItem targetProject,
            AconexRegisterSchemaSnapshot targetSchema,
            AconexDocumentCatalog documentCatalog,
            IReadOnlyList<TransmittalSyncCampoMapeoItem> fieldMappings,
            TransmittalDocumentAttachment attachment,
            string revision,
            string destinationDocumentNo,
            string fileName,
            string fileBase64,
            string mailId,
            IReadOnlyDictionary<string, string> mailHints,
            IReadOnlyDictionary<string, string> sourceHints,
            Action<string> log,
            CancellationToken cancellationToken)
        {
            string xml = await BuildRegisterXmlAsync(
                request, sourceProject, targetProject, targetSchema, documentCatalog, fieldMappings, attachment, revision, true, mailHints, sourceHints, log, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(xml))
                return false;

            string boundary = AconexRegisterMultipart.CreateBoundary();
            string body = AconexRegisterMultipart.BuildRegisterBody(xml, fileName, fileBase64, boundary);

            LogRegisterHttpDebug(request, targetProject, body, boundary, xml, "archivo " + destinationDocumentNo, log);

            var response = await _registerWrite.PostRegisterDocumentAsync(
                request.BaseUrl,
                targetProject.ProjectId,
                request.AuthorizationHeaderBase64,
                request.IntegrationId,
                body,
                boundary,
                cancellationToken).ConfigureAwait(false);

            string responseText = response?.Body ?? "";
            if (response == null || !response.IsSuccessStatusCode)
            {
                if (ResponseIndicatesFieldValueAlreadyExists(responseText))
                {
                    log?.Invoke(
                        $"Register indica documento existente ({destinationDocumentNo}); reintento supersede con archivo...");
                    TargetDocumentLookup existing = await LookupTargetDocumentInRegisterAsync(
                        request, targetProject, targetSchema, destinationDocumentNo, revision, log, cancellationToken).ConfigureAwait(false);
                    if (existing != null && !string.IsNullOrWhiteSpace(existing.DocumentId))
                    {
                        string destDocNoForLog = ResolveSupersedeDestinationDocumentNo(existing, destinationDocumentNo);
                        log?.Invoke(
                            $"Supersede con archivo en {targetProject.Label}: {destDocNoForLog} " +
                            $"rev {existing.Revision ?? "?"} → {revision} (id={existing.DocumentId})");
                        return await SupersedeDocumentAsync(
                            request, sourceProject, targetProject, targetSchema, documentCatalog, fieldMappings,
                            attachment, revision, destDocNoForLog, existing.DocumentId, fileName, fileBase64, hasFile: true,
                            mailHints, sourceHints, existing.RegisterHints, mailId, log, cancellationToken).ConfigureAwait(false);
                    }

                    log?.Invoke(
                        $"No se pudo resolver DocumentId destino para supersede ({destinationDocumentNo}) tras FIELD_VALUE_ALREADY_EXISTS.");
                }

                log?.Invoke($"Register con archivo falló en destino ({destinationDocumentNo}): {Truncate(responseText, 300)}");
                return false;
            }

            string localDocumentId = AconexRegisterResponseParser.ParseRegisterDocumentId(responseText);
            string assignedDocNo = ResolveAssignedDocumentNumber(responseText, null);
            if (!string.IsNullOrWhiteSpace(localDocumentId))
            {
                await SaveTargetDocumentMappingsAsync(
                    request.IdTrabajo, targetProject.ProjectId, revision, localDocumentId, destinationDocumentNo, assignedDocNo,
                    cancellationToken).ConfigureAwait(false);
                await MarkSourceAttachmentSyncedAsync(
                    request, sourceProject, targetProject, attachment, revision, mailId,
                    localDocumentId, assignedDocNo ?? destinationDocumentNo, cancellationToken).ConfigureAwait(false);
            }

            log?.Invoke(
                $"Documento registrado en destino: {destinationDocumentNo} → Salfa {assignedDocNo ?? "?"} rev {revision} → {localDocumentId ?? "?"}");
            return true;
        }

        private async Task<bool> SupersedeDocumentAsync(
            TransmittalSyncRunRequest request,
            ProyectoSyncItem sourceProject,
            ProyectoSyncItem targetProject,
            AconexRegisterSchemaSnapshot targetSchema,
            AconexDocumentCatalog documentCatalog,
            IReadOnlyList<TransmittalSyncCampoMapeoItem> fieldMappings,
            TransmittalDocumentAttachment attachment,
            string revision,
            string destinationDocumentNo,
            string localDocumentId,
            string fileName,
            string fileBase64,
            bool hasFile,
            IReadOnlyDictionary<string, string> mailHints,
            IReadOnlyDictionary<string, string> sourceHints,
            IReadOnlyDictionary<string, string> preloadedTargetHints,
            string mailId,
            Action<string> log,
            CancellationToken cancellationToken)
        {
            string xml = await BuildRegisterXmlAsync(
                request, sourceProject, targetProject, targetSchema, documentCatalog, fieldMappings, attachment, revision, hasFile, mailHints, sourceHints, log,
                isSupersede: true, destinationDocumentNo, targetDocumentId: localDocumentId, preloadedTargetHints: preloadedTargetHints,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(xml))
                return false;

            string boundary = AconexRegisterMultipart.CreateBoundary();
            string body = hasFile
                ? AconexRegisterMultipart.BuildRegisterBody(xml, fileName, fileBase64, boundary)
                : AconexRegisterMultipart.BuildRegisterBodyXmlOnly(xml, boundary);

            LogRegisterHttpDebug(
                request, targetProject, body, boundary, xml,
                $"supersede {(hasFile ? "archivo" : "marcador")} {destinationDocumentNo}",
                log, isSupersede: true, localDocumentId: localDocumentId);

            var response = await _registerWrite.PostSupersedeDocumentAsync(
                request.BaseUrl,
                targetProject.ProjectId,
                localDocumentId,
                request.AuthorizationHeaderBase64,
                request.IntegrationId,
                body,
                boundary,
                cancellationToken).ConfigureAwait(false);

            string responseText = response?.Body ?? "";
            if (response == null || !response.IsSuccessStatusCode)
            {
                log?.Invoke(
                    $"Supersede falló en destino ({destinationDocumentNo} → {localDocumentId}" +
                    $"{(hasFile ? "" : ", marcador")}): {Truncate(responseText, 300)}");
                return false;
            }

            string newDocumentId = AconexRegisterResponseParser.ParseRegisterDocumentId(responseText);
            string mappedId = !string.IsNullOrWhiteSpace(newDocumentId) ? newDocumentId : localDocumentId;
            await _state.SaveLocalDocumentMappingAsync(
                request.IdTrabajo, targetProject.ProjectId, destinationDocumentNo, revision, mappedId, cancellationToken).ConfigureAwait(false);
            await MarkSourceAttachmentSyncedAsync(
                request, sourceProject, targetProject, attachment, revision, mailId,
                mappedId, destinationDocumentNo, cancellationToken).ConfigureAwait(false);

            log?.Invoke(
                $"Supersede OK en destino: {destinationDocumentNo} rev {revision} " +
                $"(desde {localDocumentId} → {mappedId}){(hasFile ? " +archivo" : " marcador")}");
            return true;
        }

        /// <summary>
        /// Documento existente en destino (por DocumentNo). Incluye hints del register/search para supersede.
        /// </summary>
        private sealed class TargetDocumentLookup
        {
            public string DocumentId { get; set; }
            public string Revision { get; set; }
            public string VersionNumber { get; set; }
            public IReadOnlyDictionary<string, string> RegisterHints { get; set; }
        }

        private static string ResolveAttachmentVersion(TransmittalDocumentAttachment attachment)
        {
            return string.IsNullOrWhiteSpace(attachment?.VersionNumber) ? null : attachment.VersionNumber.Trim();
        }

        private async Task<bool> IsSourceAttachmentAlreadySyncedAsync(
            TransmittalSyncRunRequest request,
            ProyectoSyncItem sourceProject,
            TransmittalDocumentAttachment attachment,
            string revision,
            Action<string> log,
            CancellationToken cancellationToken)
        {
            string versionNumber = ResolveAttachmentVersion(attachment);
            string sourceDocumentId = ResolveSourceDocumentId(attachment);
            if (string.IsNullOrWhiteSpace(versionNumber) && string.IsNullOrWhiteSpace(sourceDocumentId))
            {
                log?.Invoke(
                    $"  Origen sin versión ni DocumentId ({attachment?.DocumentNo ?? "?"}): no se puede validar idempotencia Opción A.");
                return false;
            }

            bool synced = await _state.IsSourceDocumentSyncedAsync(
                request.IdTrabajo,
                sourceProject.ProjectId,
                attachment.DocumentNo?.Trim() ?? "",
                revision ?? "",
                versionNumber ?? "",
                sourceDocumentId,
                cancellationToken).ConfigureAwait(false);

            if (!synced)
                return false;

            string versionLabel = !string.IsNullOrWhiteSpace(versionNumber)
                ? $"ver={versionNumber}"
                : $"DocumentId={sourceDocumentId}";
            log?.Invoke(
                $"Adjunto omitido: ya sincronizado en origen ({attachment.DocumentNo} rev {revision} {versionLabel}).");
            return true;
        }

        private Task MarkSourceAttachmentSyncedAsync(
            TransmittalSyncRunRequest request,
            ProyectoSyncItem sourceProject,
            ProyectoSyncItem targetProject,
            TransmittalDocumentAttachment attachment,
            string revision,
            string mailId,
            string destDocumentId,
            string destDocumentNo,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(destDocumentId))
                return Task.CompletedTask;

            return _state.MarkSourceDocumentSyncedAsync(
                request.IdTrabajo,
                sourceProject.ProjectId,
                attachment.DocumentNo?.Trim() ?? "",
                revision ?? "",
                ResolveAttachmentVersion(attachment) ?? "",
                ResolveSourceDocumentId(attachment),
                targetProject.ProjectId,
                destDocumentId,
                destDocumentNo,
                revision,
                mailId,
                cancellationToken);
        }

        private static string GetVersionNumberFromSearchResult(Searchresult result)
        {
            return result?.GetDynamicValue("versionNumber") ?? result?.GetDynamicValue("versionnumber");
        }

        private static bool RevisionsEqual(string a, string b)
        {
            bool aWildcard = IsWildcardRevision(a);
            bool bWildcard = IsWildcardRevision(b);

            // Ambos sin revisión definida (vacío, "-", etc.).
            if (aWildcard && bWildcard)
                return true;

            // Revisión concreta (ej. A en SALFA) vs wildcard en destino (ej. "-" en Codelco) → actualizar.
            if (aWildcard || bWildcard)
                return false;

            return string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Busca documento en destino: mapeo local BD → register/search por <see cref="CodelcoBridgeField"/> (ida)
        /// → register/search por docno (vuelta SALFA→Codelco).
        /// </summary>
        private async Task<TargetDocumentLookup> ResolveTargetDocumentWithHintsAsync(
            TransmittalSyncRunRequest request,
            ProyectoSyncItem targetProject,
            AconexRegisterSchemaSnapshot targetSchema,
            string documentNo,
            string revision,
            string codelcoBridgeKey,
            Action<string> log,
            CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(documentNo))
            {
                string localDocumentId = await TryGetLocalTargetDocumentIdAsync(
                    request, targetProject.ProjectId, documentNo.Trim(), revision, cancellationToken).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(localDocumentId))
                {
                    log?.Invoke($"  Mapeo local BD: docno={documentNo.Trim()} rev={revision} → {localDocumentId}");
                    return new TargetDocumentLookup
                    {
                        DocumentId = localDocumentId,
                        Revision = revision,
                        RegisterHints = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    };
                }
            }

            if (!string.IsNullOrWhiteSpace(codelcoBridgeKey))
            {
                TargetDocumentLookup fromBridge = await LookupTargetDocumentByCodelcoBridgeInRegisterAsync(
                    request, targetProject, targetSchema, codelcoBridgeKey, revision, log, cancellationToken).ConfigureAwait(false);
                if (fromBridge != null && !string.IsNullOrWhiteSpace(fromBridge.DocumentId))
                {
                    log?.Invoke(
                        $"  Register destino: encontrado por {CodelcoBridgeField}={codelcoBridgeKey.Trim()} → {fromBridge.DocumentId}");
                    return await FinalizeRecoveredTargetDocumentAsync(
                        request, targetProject, documentNo, revision, fromBridge, log, cancellationToken).ConfigureAwait(false);
                }
            }

            if (!string.IsNullOrWhiteSpace(documentNo))
            {
                TargetDocumentLookup fromRegister = await LookupTargetDocumentInRegisterAsync(
                    request, targetProject, targetSchema, documentNo.Trim(), revision, log, cancellationToken).ConfigureAwait(false);
                if (fromRegister != null && !string.IsNullOrWhiteSpace(fromRegister.DocumentId))
                {
                    log?.Invoke(
                        $"  Register destino: encontrado por docno={documentNo.Trim()} rev={fromRegister.Revision ?? "?"} → {fromRegister.DocumentId}");
                    return await FinalizeRecoveredTargetDocumentAsync(
                        request, targetProject, documentNo, revision, fromRegister, log, cancellationToken).ConfigureAwait(false);
                }
            }

            return null;
        }

        private async Task<string> TryGetLocalTargetDocumentIdAsync(
            TransmittalSyncRunRequest request,
            string targetProjectId,
            string documentNo,
            string revision,
            CancellationToken cancellationToken)
        {
            foreach (string revKey in EnumerateRevisionLookupKeys(revision))
            {
                string localId = await _state.GetLocalDocumentIdAsync(
                    request.IdTrabajo, targetProjectId, documentNo, revKey, cancellationToken).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(localId))
                    return localId;
            }

            return null;
        }

        private static IEnumerable<string> EnumerateRevisionLookupKeys(string revision)
        {
            yield return revision ?? "";
            if (!IsWildcardRevision(revision))
            {
                yield break;
            }

            yield return "-";
            yield return "A";
            yield return "";
        }

        private async Task<TargetDocumentLookup> FinalizeRecoveredTargetDocumentAsync(
            TransmittalSyncRunRequest request,
            ProyectoSyncItem targetProject,
            string predictedDocumentNo,
            string revision,
            TargetDocumentLookup found,
            Action<string> log,
            CancellationToken cancellationToken)
        {
            string effectiveRevision = string.IsNullOrWhiteSpace(found.Revision) ? revision : found.Revision;
            await SaveRecoveredTargetDocumentMappingsAsync(
                request, targetProject.ProjectId, predictedDocumentNo, effectiveRevision, found, cancellationToken).ConfigureAwait(false);

            string actualDocNo = GetHintValue(found.RegisterHints, "DocumentNumber");
            string displayDocNo = !string.IsNullOrWhiteSpace(actualDocNo) ? actualDocNo : predictedDocumentNo;
            log?.Invoke(
                $"Mapeo recuperado en {targetProject.Label}: {displayDocNo} " +
                $"rev destino={found.Revision ?? "?"} (pedida {revision}) → {found.DocumentId}");
            return found;
        }

        private async Task SaveRecoveredTargetDocumentMappingsAsync(
            TransmittalSyncRunRequest request,
            string targetProjectId,
            string predictedDocumentNo,
            string revision,
            TargetDocumentLookup found,
            CancellationToken cancellationToken)
        {
            if (found == null || string.IsNullOrWhiteSpace(found.DocumentId))
                return;

            string effectiveRevision = revision ?? "";
            var docNoKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(predictedDocumentNo))
                docNoKeys.Add(predictedDocumentNo.Trim());

            string actualDocNo = GetHintValue(found.RegisterHints, "DocumentNumber");
            if (!string.IsNullOrWhiteSpace(actualDocNo))
                docNoKeys.Add(actualDocNo.Trim());

            string codelcoKey = GetHintValue(found.RegisterHints, CodelcoBridgeField);
            if (!string.IsNullOrWhiteSpace(codelcoKey))
                docNoKeys.Add(codelcoKey.Trim());

            foreach (string docNoKey in docNoKeys)
            {
                await _state.SaveLocalDocumentMappingAsync(
                    request.IdTrabajo,
                    targetProjectId,
                    docNoKey,
                    effectiveRevision,
                    found.DocumentId,
                    cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task<TargetDocumentLookup> LookupTargetDocumentByDocumentIdInRegisterAsync(
            TransmittalSyncRunRequest request,
            ProyectoSyncItem targetProject,
            string documentId,
            IReadOnlyList<string> returnFields,
            Action<string> log,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(documentId))
                return null;

            log?.Invoke(
                $"  Register destino: documentid={documentId.Trim()}, returnFields ({returnFields.Count}): " +
                string.Join(", ", returnFields));

            var searchResult = await SearchTargetRegisterByQueryAsync(
                request,
                targetProject.ProjectId,
                "documentid:" + documentId.Trim(),
                returnFields,
                cancellationToken).ConfigureAwait(false);

            if (searchResult != null && (!searchResult.IsHttpSuccess || searchResult.HasAconexError))
            {
                string aconexMsg = !string.IsNullOrWhiteSpace(searchResult.AconexErrorDescription)
                    ? $"{searchResult.AconexErrorCode}: {searchResult.AconexErrorDescription}"
                    : Truncate(searchResult.ResponseBody, 400);
                log?.Invoke($"  Register destino: search error (documentid={documentId}): {aconexMsg}");
                return null;
            }

            var page = searchResult?.Page;
            if (page?.searchResults == null || page.searchResults.Count == 0)
            {
                log?.Invoke($"  Register destino: sin resultados search documentid={documentId}.");
                return null;
            }

            Searchresult match = page.searchResults.FirstOrDefault(r => r != null && r.Id > 0);
            if (match == null)
                return null;

            return BuildTargetDocumentLookupFromSearchResult(match, log);
        }

        private async Task<TargetDocumentLookup> LookupTargetDocumentInRegisterAsync(
            TransmittalSyncRunRequest request,
            ProyectoSyncItem targetProject,
            AconexRegisterSchemaSnapshot targetSchema,
            string documentNo,
            string revision,
            Action<string> log,
            CancellationToken cancellationToken,
            IReadOnlyList<string> returnFields = null)
        {
            if (string.IsNullOrWhiteSpace(documentNo))
                return null;

            returnFields ??= BuildTargetRegisterExistenceSearchReturnFields();
            log?.Invoke(
                $"  Register destino: docno={documentNo.Trim()}, returnFields ({returnFields.Count}): " +
                string.Join(", ", returnFields));

            var searchResult = await SearchTargetRegisterRobustForLookupAsync(
                request, targetProject.ProjectId, documentNo.Trim(), revision, returnFields, cancellationToken).ConfigureAwait(false);

            if (searchResult != null && (!searchResult.IsHttpSuccess || searchResult.HasAconexError))
            {
                string aconexMsg = !string.IsNullOrWhiteSpace(searchResult.AconexErrorDescription)
                    ? $"{searchResult.AconexErrorCode}: {searchResult.AconexErrorDescription}"
                    : Truncate(searchResult.ResponseBody, 400);
                log?.Invoke($"  Register destino: search error ({documentNo}): {aconexMsg}");
                return null;
            }

            var page = searchResult?.Page;
            if (page?.searchResults == null || page.searchResults.Count == 0)
            {
                log?.Invoke(
                    $"  Register destino: sin resultados search para docno={documentNo} (proyecto {targetProject.ProjectId}).");
                return null;
            }

            Searchresult match = SelectTargetRegisterSearchMatch(page.searchResults, documentNo, revision);
            if (match == null || match.Id <= 0)
            {
                log?.Invoke($"  Register destino: sin match docno={documentNo} entre {page.searchResults.Count} resultado(s).");
                return null;
            }

            return BuildTargetDocumentLookupFromSearchResult(match, log);
        }

        private async Task<TargetDocumentLookup> LookupTargetDocumentByCodelcoBridgeInRegisterAsync(
            TransmittalSyncRunRequest request,
            ProyectoSyncItem targetProject,
            AconexRegisterSchemaSnapshot targetSchema,
            string codelcoBridgeKey,
            string revision,
            Action<string> log,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(codelcoBridgeKey))
                return null;

            IReadOnlyList<string> returnFields = BuildTargetRegisterBridgeSearchReturnFields();
            string searchQuery = BuildCodelcoBridgeSearchQuery(codelcoBridgeKey);
            log?.Invoke(
                $"  Register destino: búsqueda {CodelcoBridgeField}={codelcoBridgeKey.Trim()}, query={searchQuery}");

            var searchResult = await SearchTargetRegisterByQueryAsync(
                request, targetProject.ProjectId, searchQuery, returnFields, cancellationToken).ConfigureAwait(false);

            if (searchResult != null && (!searchResult.IsHttpSuccess || searchResult.HasAconexError))
            {
                string aconexMsg = !string.IsNullOrWhiteSpace(searchResult.AconexErrorDescription)
                    ? $"{searchResult.AconexErrorCode}: {searchResult.AconexErrorDescription}"
                    : Truncate(searchResult.ResponseBody, 400);
                log?.Invoke($"  Register destino: search error ({CodelcoBridgeField}={codelcoBridgeKey}): {aconexMsg}");
                return null;
            }

            var page = searchResult?.Page;
            if (page?.searchResults == null || page.searchResults.Count == 0)
            {
                log?.Invoke($"  Register destino: sin resultados search {CodelcoBridgeField}={codelcoBridgeKey}.");
                return null;
            }

            if (page.searchResults.Count > 1)
                log?.Invoke(
                    $"  Register destino: {page.searchResults.Count} documento(s) con {CodelcoBridgeField}={codelcoBridgeKey.Trim()} " +
                    "(se selecciona uno según revisión).");

            Searchresult match = SelectTargetRegisterSearchMatchByCodelcoBridge(
                page.searchResults, codelcoBridgeKey, revision);
            if (match == null || match.Id <= 0)
            {
                log?.Invoke(
                    $"  Register destino: sin match {CodelcoBridgeField}={codelcoBridgeKey} entre {page.searchResults.Count} resultado(s).");
                return null;
            }

            return BuildTargetDocumentLookupFromSearchResult(match, log);
        }

        private static TargetDocumentLookup BuildTargetDocumentLookupFromSearchResult(
            Searchresult match,
            Action<string> log)
        {
            var hints = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            log?.Invoke(
                $"  Register destino: rev={match.Revision ?? "?"}, ver={GetVersionNumberFromSearchResult(match) ?? "?"}, projectFields={match.ProjectFields?.Count ?? 0}");
            MergeSearchResultIntoHints(hints, match, log, logCodelcoBridge: false);

            return new TargetDocumentLookup
            {
                DocumentId = match.Id.ToString(),
                Revision = match.Revision?.Trim(),
                VersionNumber = GetVersionNumberFromSearchResult(match)?.Trim(),
                RegisterHints = hints
            };
        }

        private static Searchresult SelectTargetRegisterSearchMatch(
            IReadOnlyList<Searchresult> results,
            string documentNo,
            string revision)
        {
            if (results == null || results.Count == 0 || string.IsNullOrWhiteSpace(documentNo))
                return null;

            string trimmedDocNo = documentNo.Trim();
            var byDocNo = results
                .Where(r => r != null
                    && r.Id > 0
                    && string.Equals(r.DocumentNumber?.Trim(), trimmedDocNo, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (byDocNo.Count == 0)
                return null;

            Searchresult match = byDocNo.FirstOrDefault(r =>
                IsWildcardRevision(revision) ||
                string.Equals(NormalizeRevision(r.Revision), NormalizeRevision(revision), StringComparison.OrdinalIgnoreCase));

            if (match == null)
                match = byDocNo.FirstOrDefault(r => RevisionsEqual(r.Revision, revision));

            return match ?? byDocNo[0];
        }

        private static Searchresult SelectTargetRegisterSearchMatchByCodelcoBridge(
            IReadOnlyList<Searchresult> results,
            string codelcoBridgeKey,
            string revision)
        {
            if (results == null || results.Count == 0 || string.IsNullOrWhiteSpace(codelcoBridgeKey))
                return null;

            string trimmedKey = codelcoBridgeKey.Trim();
            var byBridge = results
                .Where(r => r != null
                    && r.Id > 0
                    && string.Equals(GetCodelcoBridgeFromSearchResult(r), trimmedKey, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (byBridge.Count == 0)
                return null;

            Searchresult match = byBridge.FirstOrDefault(r =>
                IsWildcardRevision(revision) ||
                string.Equals(NormalizeRevision(r.Revision), NormalizeRevision(revision), StringComparison.OrdinalIgnoreCase));

            if (match == null)
                match = byBridge.FirstOrDefault(r => RevisionsEqual(r.Revision, revision));

            return match ?? byBridge[0];
        }

        private static string GetCodelcoBridgeFromSearchResult(Searchresult result)
        {
            if (result?.ProjectFields == null)
                return null;

            foreach (var field in result.ProjectFields)
            {
                if (field == null || string.IsNullOrWhiteSpace(field.Name))
                    continue;
                if (!string.Equals(field.Name.Trim(), CodelcoBridgeField, StringComparison.OrdinalIgnoreCase))
                    continue;
                return field.Value?.Trim();
            }

            return null;
        }

        private static string BuildCodelcoBridgeSearchQuery(string codelcoBridgeKey)
        {
            string escaped = EscapeLuceneQuoted(codelcoBridgeKey.Trim());
            return $"{CodelcoBridgeField}:\"{escaped}\"";
        }

        private static string BuildDocumentSearchQuery(string documentNo, string revision)
        {
            string doc = EscapeLuceneQuoted(documentNo.Trim());
            if (IsWildcardRevision(revision))
                return $"docno:\"{doc}\"";
            string rev = EscapeLuceneQuoted(NormalizeRevision(revision));
            return $"docno:\"{doc}\" AND revision:\"{rev}\"";
        }

        private static bool IsWildcardRevision(string revision)
        {
            if (string.IsNullOrWhiteSpace(revision))
                return true;
            string trimmed = revision.Trim();
            return trimmed == "-" || trimmed == "—";
        }

        private static string NormalizeRevision(string revision)
        {
            if (IsWildcardRevision(revision))
                return "A";
            return revision.Trim();
        }

        private static string ResolveSourceDocumentId(TransmittalDocumentAttachment attachment)
        {
            if (attachment == null)
                return null;
            if (!string.IsNullOrWhiteSpace(attachment.DocumentId))
                return attachment.DocumentId.Trim();
            if (!string.IsNullOrWhiteSpace(attachment.RegisteredAs))
                return attachment.RegisteredAs.Trim();
            return null;
        }

        private static string EscapeLuceneQuoted(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private async Task<AconexRegisterSchemaSnapshot> LoadTargetRegisterSchemaAsync(
            TransmittalSyncRunRequest request,
            ProyectoSyncItem targetProject,
            CancellationToken cancellationToken)
        {
            string schemaXml = await _registerWrite.GetRegisterSchemaXmlAsync(
                request.BaseUrl,
                targetProject.ProjectId,
                request.AuthorizationHeaderBase64,
                request.IntegrationId,
                cancellationToken).ConfigureAwait(false);

            return AconexRegisterSchemaParser.ParseSnapshot(schemaXml);
        }

        private async Task<string> BuildRegisterXmlAsync(
            TransmittalSyncRunRequest request,
            ProyectoSyncItem sourceProject,
            ProyectoSyncItem targetProject,
            AconexRegisterSchemaSnapshot targetSchema,
            AconexDocumentCatalog documentCatalog,
            IReadOnlyList<TransmittalSyncCampoMapeoItem> fieldMappings,
            TransmittalDocumentAttachment attachment,
            string revision,
            bool hasFile,
            IReadOnlyDictionary<string, string> mailHints,
            IReadOnlyDictionary<string, string> preloadedSourceHints,
            Action<string> log,
            bool isSupersede = false,
            string destinationDocumentNo = null,
            string targetDocumentId = null,
            IReadOnlyDictionary<string, string> preloadedTargetHints = null,
            CancellationToken cancellationToken = default)
        {
            var sourceHints = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (preloadedSourceHints != null)
            {
                foreach (var kv in preloadedSourceHints)
                {
                    if (!string.IsNullOrWhiteSpace(kv.Key) && !string.IsNullOrWhiteSpace(kv.Value))
                        sourceHints[kv.Key] = kv.Value.Trim();
                }
            }
            else
            {
                var fetched = await FetchSourceDocumentHintsAsync(
                    request, sourceProject, attachment, revision, fieldMappings, mailHints, log, cancellationToken).ConfigureAwait(false);
                foreach (var kv in fetched)
                {
                    if (!string.IsNullOrWhiteSpace(kv.Key) && !string.IsNullOrWhiteSpace(kv.Value))
                        sourceHints[kv.Key] = kv.Value.Trim();
                }
            }

            string fixedDocumentStatusId = ResolveFixedDocumentStatus(
                request, targetProject, documentCatalog, log);

            LogCamposObtenidos(attachment, revision, sourceHints, log);

            string xml;
            string error;
            if (fieldMappings != null && fieldMappings.Count > 0)
            {
                if (isSupersede)
                {
                    string supersedeDocNo = destinationDocumentNo
                        ?? ResolveDestinationDocumentKey(request, sourceProject, documentCatalog, fieldMappings, attachment, sourceHints)
                        ?? attachment.DocumentNo;
                    var targetHints = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    if (preloadedTargetHints != null)
                    {
                        foreach (var kv in preloadedTargetHints)
                        {
                            if (!string.IsNullOrWhiteSpace(kv.Key) && !string.IsNullOrWhiteSpace(kv.Value))
                                targetHints[kv.Key.Trim()] = kv.Value.Trim();
                        }
                    }

                    IReadOnlyDictionary<string, string> fetchedTargetHints = await FetchTargetRegisterHintsAsync(
                        request, targetProject, targetSchema, fieldMappings, targetDocumentId, supersedeDocNo, revision, log,
                        cancellationToken).ConfigureAwait(false);
                    foreach (var kv in fetchedTargetHints)
                    {
                        if (!string.IsNullOrWhiteSpace(kv.Key) && !string.IsNullOrWhiteSpace(kv.Value))
                            targetHints[kv.Key.Trim()] = kv.Value.Trim();
                    }

                    xml = TransmittalRegisterXmlBuilder.BuildSupersedeFromFieldMappings(
                        fieldMappings, targetSchema, documentCatalog, attachment, revision, hasFile, sourceHints,
                        targetHints, fixedDocumentStatusId, out error);
                }
                else
                {
                    bool useAutoNumber = ShouldUseSalfaAutoDocumentNumber(request, sourceProject);
                    xml = TransmittalRegisterXmlBuilder.BuildFromFieldMappings(
                        fieldMappings, targetSchema, documentCatalog, attachment, revision, hasFile, sourceHints,
                        fixedDocumentStatusId, omitDocumentNumber: useAutoNumber, out error);
                }
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(fixedDocumentStatusId))
                {
                    sourceHints["statusid"] = fixedDocumentStatusId;
                    sourceHints["DocumentStatusId"] = fixedDocumentStatusId;
                    sourceHints["DocumentStatus"] = fixedDocumentStatusId;
                    sourceHints["Status"] = fixedDocumentStatusId;
                }

                LogRegisterDiagnostics(targetSchema, attachment.DocumentNo, sourceHints, log);
                xml = TransmittalRegisterXmlBuilder.Build(
                    targetSchema, attachment, revision, hasFile, sourceHints, out error);
            }

            if (string.IsNullOrWhiteSpace(xml))
            {
                log?.Invoke($"No se pudo armar XML Register ({attachment.DocumentNo}): {error}");
                return null;
            }

            string destDocNo = ResolveDestinationDocumentKey(
                request, sourceProject, documentCatalog, fieldMappings, attachment, sourceHints) ?? attachment.DocumentNo;
            log?.Invoke(
                $"CAMPOS ENVIADOS A REGISTER destino ({destDocNo}):{Environment.NewLine}" +
                TransmittalRegisterXmlBuilder.FormatXmlFieldLines(xml));

            return xml;
        }

        /// <summary>
        /// Resuelve idEstatus desde parámetro <see cref="TransmittalSyncRunRequest.IdEstatusDocumentoDestino"/>
        /// cuando el destino es el proyecto configurado (lado 1).
        /// </summary>
        private static string ResolveFixedDocumentStatus(
            TransmittalSyncRunRequest request,
            ProyectoSyncItem targetProject,
            AconexDocumentCatalog documentCatalog,
            Action<string> log)
        {
            if (request == null || targetProject == null)
                return null;

            string configured = request.IdEstatusDocumentoDestino?.Trim();
            if (string.IsNullOrWhiteSpace(configured))
                return null;

            string targetId = request.IdProyectoEstatusFijo?.Trim();
            if (string.IsNullOrWhiteSpace(targetId))
                return null;

            if (!string.Equals(targetProject.ProjectId?.Trim(), targetId, StringComparison.OrdinalIgnoreCase))
                return null;

            string resolved = documentCatalog?.ResolveByCatalog(
                AconexDocumentCatalogNames.EstatusDocumentos,
                configured) ?? configured;

            log?.Invoke(
                $"  Parámetro IdEstatusDocumentoDestino → destino {targetProject.Label} ({targetProject.ProjectId}): " +
                $"{configured} → {resolved}");

            return resolved;
        }

        private static void LogCamposObtenidos(
            TransmittalDocumentAttachment attachment,
            string revision,
            IReadOnlyDictionary<string, string> sourceHints,
            Action<string> log)
        {
            if (log == null || attachment == null)
                return;

            string docNo = attachment.DocumentNo ?? "?";
            log.Invoke($"CAMPOS OBTENIDOS ({docNo}):");

            log.Invoke("  [Transmittal adjunto]");
            log.Invoke($"    DocumentNo={attachment.DocumentNo ?? ""}");
            log.Invoke($"    Title={attachment.Title ?? ""}");
            log.Invoke($"    Revision={attachment.Revision ?? ""} (usada: {revision})");
            log.Invoke($"    VersionNumber={attachment.VersionNumber ?? ""}");
            log.Invoke($"    RevisionDate={attachment.RevisionDate ?? ""}");
            log.Invoke($"    Status={attachment.Status ?? ""}");
            log.Invoke($"    FileName={attachment.FileName ?? ""}");
            log.Invoke($"    FileSize={attachment.FileSize}");
            log.Invoke($"    DocumentId={attachment.DocumentId ?? ""}");
            log.Invoke($"    RegisteredAs={attachment.RegisteredAs ?? ""}");

            log.Invoke("  [Register documento origen]");
            if (sourceHints == null || sourceHints.Count == 0)
            {
                log.Invoke("    (vacío — no se encontró el doc en register/search del origen)");
                return;
            }

            foreach (var kv in sourceHints)
                log.Invoke($"    {kv.Key}={kv.Value}");
        }

        private static void LogRegisterDiagnostics(
            AconexRegisterSchemaSnapshot targetSchema,
            string documentNo,
            IReadOnlyDictionary<string, string> sourceHints,
            Action<string> log)
        {
            if (log == null)
                return;

            var missing = TransmittalRegisterXmlBuilder.ListMandatoryFieldsMissingInSource(targetSchema, sourceHints);
            if (missing.Count > 0)
            {
                log.Invoke(
                    $"  Nota ({documentNo}): obligatorios en destino sin dato en origen (mismo nombre): " +
                    string.Join(", ", missing));
            }
        }

        private async Task<IReadOnlyDictionary<string, string>> FetchSourceDocumentHintsAsync(
            TransmittalSyncRunRequest request,
            ProyectoSyncItem sourceProject,
            TransmittalDocumentAttachment attachment,
            string revision,
            IReadOnlyList<TransmittalSyncCampoMapeoItem> fieldMappings,
            IReadOnlyDictionary<string, string> mailHints,
            Action<string> log,
            CancellationToken cancellationToken,
            bool skipMetadata = false)
        {
            var hints = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (mailHints != null)
            {
                foreach (var kv in mailHints)
                {
                    if (!string.IsNullOrWhiteSpace(kv.Key) && !string.IsNullOrWhiteSpace(kv.Value))
                        hints[kv.Key] = kv.Value.Trim();
                }
            }

            if (!string.IsNullOrWhiteSpace(attachment?.Status))
            {
                hints["Status"] = attachment.Status.Trim();
                hints["statusid"] = attachment.Status.Trim();
            }
            if (!string.IsNullOrWhiteSpace(attachment?.RevisionDate))
                hints["RevisionDate"] = attachment.RevisionDate.Trim();

            string sourceDocumentId = ResolveSourceDocumentId(attachment);
            if (!skipMetadata && !string.IsNullOrWhiteSpace(sourceDocumentId))
            {
                var metadata = await _registerMetadata.GetRegisterMetadataAsync(
                    request.BaseUrl,
                    sourceProject.ProjectId,
                    sourceDocumentId,
                    request.AuthorizationHeaderBase64,
                    cancellationToken).ConfigureAwait(false);

                if (metadata != null)
                {
                    MergeRegisterMetadataHints(hints, metadata);
                    log?.Invoke(
                        $"  Register origen: metadata {sourceDocumentId} → " +
                        $"doctype={metadata.DocumentType ?? ""}, status={metadata.DocumentStatus ?? ""}");
                }
                else
                {
                    log?.Invoke(
                        $"  Register origen: sin metadata para DocumentId={sourceDocumentId} " +
                        $"(proyecto {sourceProject.ProjectId}).");
                }
            }
            else if (skipMetadata && !string.IsNullOrWhiteSpace(sourceDocumentId))
            {
                log?.Invoke($"  Register origen: metadata omitida (supersede/vuelta) para DocumentId={sourceDocumentId}.");
            }

            if (string.IsNullOrWhiteSpace(attachment?.DocumentNo))
                return hints;

            IReadOnlyList<string> returnFields = BuildRegisterSearchReturnFields(fieldMappings);
            log?.Invoke($"  Register origen returnFields: {string.Join(", ", returnFields)}");

            var searchResult = await SearchSourceRegisterAsync(
                request, sourceProject, attachment, revision, returnFields, cancellationToken).ConfigureAwait(false);

            if (searchResult != null && (!searchResult.IsHttpSuccess || searchResult.HasAconexError))
            {
                IReadOnlyList<string> coreFields = BuildCoreRegisterSearchReturnFields(fieldMappings);
                if (coreFields.Count < returnFields.Count)
                {
                    log?.Invoke(
                        $"  Register origen: reintento search solo campos API estándar ({coreFields.Count} de {returnFields.Count})...");
                    searchResult = await SearchSourceRegisterAsync(
                        request, sourceProject, attachment, revision, coreFields, cancellationToken).ConfigureAwait(false);
                }
            }

            if (searchResult != null && (!searchResult.IsHttpSuccess || searchResult.HasAconexError))
            {
                string aconexMsg = !string.IsNullOrWhiteSpace(searchResult.AconexErrorDescription)
                    ? $"{searchResult.AconexErrorCode}: {searchResult.AconexErrorDescription}"
                    : Truncate(searchResult.ResponseBody, 400);
                log?.Invoke(
                    $"  Register origen: search error HTTP {searchResult.StatusCode} ({attachment.DocumentNo}): {aconexMsg}");
                if (!string.IsNullOrWhiteSpace(searchResult.RequestBody))
                    log?.Invoke($"  Register origen search request: {Truncate(searchResult.RequestBody, 600)}");
            }

            var page = searchResult?.Page;

            if (page?.searchResults == null || page.searchResults.Count == 0)
            {
                if (searchResult == null || (searchResult.IsHttpSuccess && !searchResult.HasAconexError))
                {
                    log?.Invoke(
                        $"  Register origen: sin resultados search para docno={attachment.DocumentNo} " +
                        $"(proyecto {sourceProject.ProjectId}, revisión {revision}).");
                }

                if (!string.IsNullOrWhiteSpace(sourceDocumentId))
                {
                    log?.Invoke($"  Register origen: reintento search documentid={sourceDocumentId}...");
                    var idSearch = await _registerSearch.SearchRegisterPageAsync(
                        request.BaseUrl,
                        sourceProject.ProjectId,
                        request.OrgId ?? "",
                        request.UserId ?? "",
                        request.AuthorizationHeaderBase64,
                        returnFields,
                        25,
                        1,
                        throwIfNotSuccess: false,
                        cancellationToken,
                        searchQuery: "documentid:" + sourceDocumentId.Trim()).ConfigureAwait(false);

                    page = idSearch?.Page;
                    if (page?.searchResults != null && page.searchResults.Count > 0)
                        searchResult = idSearch;
                    else
                        log?.Invoke($"  Register origen: sin resultados search documentid={sourceDocumentId}.");
                }

                if (page?.searchResults == null || page.searchResults.Count == 0)
                    return hints;
            }

            Searchresult match = page.searchResults.FirstOrDefault(r =>
                string.Equals(r.DocumentNumber?.Trim(), attachment.DocumentNo.Trim(), StringComparison.OrdinalIgnoreCase) &&
                (IsWildcardRevision(revision) ||
                 string.Equals(NormalizeRevision(r.Revision), NormalizeRevision(revision), StringComparison.OrdinalIgnoreCase)));

            if (match == null && page.searchResults.Count == 1)
                match = page.searchResults[0];

            if (match == null)
                return hints;

            MergeSearchResultIntoHints(hints, match, log, logCodelcoBridge: true);

            return hints;
        }

        /// <summary>
        /// Lee el documento existente en destino (register/search) con todos los returnFields del supersede
        /// (schema mandatory + homologación destino + CamposConsultaRegistroDestino).
        /// </summary>
        private async Task<IReadOnlyDictionary<string, string>> FetchTargetRegisterHintsAsync(
            TransmittalSyncRunRequest request,
            ProyectoSyncItem targetProject,
            AconexRegisterSchemaSnapshot targetSchema,
            IReadOnlyList<TransmittalSyncCampoMapeoItem> fieldMappings,
            string documentId,
            string documentNo,
            string revision,
            Action<string> log,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<string> configuredExtra = ResolveConfiguredExtraFieldsForTargetSupersedeSearch(
                request, targetProject);
            IReadOnlyList<string> returnFields = BuildTargetRegisterSearchReturnFields(
                targetSchema, configuredExtra, fieldMappings);

            TargetDocumentLookup lookup = null;
            if (!string.IsNullOrWhiteSpace(documentId))
            {
                lookup = await LookupTargetDocumentByDocumentIdInRegisterAsync(
                    request, targetProject, documentId.Trim(), returnFields, log, cancellationToken).ConfigureAwait(false);
            }

            if (lookup == null && !string.IsNullOrWhiteSpace(documentNo))
            {
                lookup = await LookupTargetDocumentInRegisterAsync(
                    request, targetProject, targetSchema, documentNo.Trim(), revision, log, cancellationToken,
                    returnFields).ConfigureAwait(false);
            }

            if (lookup?.RegisterHints == null || lookup.RegisterHints.Count == 0)
            {
                log?.Invoke(
                    "  Register destino supersede: sin hints (documentid=" + (documentId ?? "?") +
                    ", docno=" + (documentNo ?? "?") + ").");
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            log?.Invoke(
                $"  Register destino supersede: {lookup.RegisterHints.Count} campo(s) leídos " +
                $"(documentid={lookup.DocumentId ?? documentId ?? "?"}, docno={documentNo ?? "?"}).");
            return lookup.RegisterHints;
        }

        private static void MergeSearchResultIntoHints(
            IDictionary<string, string> hints,
            Searchresult match,
            Action<string> log,
            bool logCodelcoBridge)
        {
            if (hints == null || match == null)
                return;

            AddHint(hints, "DocumentNumber", match.DocumentNumber);
            AddHint(hints, "Title", match.Title);
            AddHint(hints, "Revision", match.Revision);
            string versionNumber = GetVersionNumberFromSearchResult(match);
            AddHint(hints, "versionnumber", versionNumber);
            AddHint(hints, "VersionNumber", versionNumber);
            string revisionDate = match.GetDynamicValue("revisiondate") ?? match.GetDynamicValue("revisionDate");
            AddHint(hints, "revisiondate", revisionDate);
            AddHint(hints, "RevisionDate", revisionDate);
            string docType = match.GetDynamicValue("doctype") ?? match.GetDynamicValue("documentType");
            AddHint(hints, "doctype", docType);
            AddHint(hints, "DocumentType", docType);
            AddHint(hints, "DocumentTypeId", docType);
            string docStatus = match.GetDynamicValue("statusid") ?? match.GetDynamicValue("documentStatusId")
                ?? match.GetDynamicValue("documentStatus");
            AddHint(hints, "statusid", docStatus);
            AddHint(hints, "DocumentStatus", docStatus);
            AddHint(hints, "DocumentStatusId", docStatus);
            AddHint(hints, "discipline", match.GetDynamicValue("discipline"));
            AddHint(hints, "author", match.GetDynamicValue("author"));
            string reviewStatus = match.GetDynamicValue("reviewstatus") ?? match.GetDynamicValue("reviewStatus");
            AddHint(hints, "reviewstatus", reviewStatus);
            AddHint(hints, "ReviewStatusId", reviewStatus);

            if (match.ProjectFields != null)
            {
                foreach (var field in match.ProjectFields)
                {
                    if (field == null || string.IsNullOrWhiteSpace(field.Name) || string.IsNullOrWhiteSpace(field.Value))
                        continue;
                    string name = field.Name.Trim();
                    string value = field.Value.Trim();
                    AddHint(hints, name, value);
                    if (logCodelcoBridge && string.Equals(name, CodelcoBridgeField, StringComparison.OrdinalIgnoreCase))
                        log?.Invoke($"  Register origen: {CodelcoBridgeField}={value}");
                }
            }

            if (match.ExtensionData != null)
            {
                foreach (var kv in match.ExtensionData)
                {
                    if (kv.Value == null)
                        continue;
                    AddHint(hints, kv.Key, kv.Value.ToString());
                }
            }
        }

        private Task<AconexRegisterSearchResult> SearchTargetRegisterAsync(
            TransmittalSyncRunRequest request,
            string targetProjectId,
            string documentNo,
            IReadOnlyList<string> returnFields,
            CancellationToken cancellationToken,
            string filterRevision = null)
        {
            return _registerSearch.SearchRegisterPageAsync(
                request.BaseUrl,
                targetProjectId,
                request.OrgId ?? "",
                request.UserId ?? "",
                request.AuthorizationHeaderBase64,
                returnFields,
                25,
                1,
                throwIfNotSuccess: false,
                cancellationToken,
                filterDocumentNo: documentNo,
                filterRevision: filterRevision);
        }

        /// <summary>
        /// register/search por docno: Aconex falla sin filtro de revisión; se prueba la pedida y luego "-" / A / B.
        /// </summary>
        private async Task<AconexRegisterSearchResult> SearchTargetRegisterRobustForLookupAsync(
            TransmittalSyncRunRequest request,
            string targetProjectId,
            string documentNo,
            string preferredRevision,
            IReadOnlyList<string> returnFields,
            CancellationToken cancellationToken)
        {
            var revisionsToTry = new List<string>();
            if (!string.IsNullOrWhiteSpace(preferredRevision) && !IsWildcardRevision(preferredRevision))
                revisionsToTry.Add(NormalizeRevision(preferredRevision));

            foreach (string candidate in new[] { "-", "A", "B" })
            {
                if (!revisionsToTry.Exists(r => string.Equals(r, candidate, StringComparison.OrdinalIgnoreCase)))
                    revisionsToTry.Add(candidate);
            }

            AconexRegisterSearchResult last = null;
            foreach (string revisionFilter in revisionsToTry)
            {
                var result = await SearchTargetRegisterAsync(
                    request, targetProjectId, documentNo, returnFields, cancellationToken, revisionFilter).ConfigureAwait(false);
                last = result;
                if (result?.IsHttpSuccess == true && result.Page?.searchResults != null && result.Page.searchResults.Count > 0)
                    return result;
            }

            return last;
        }

        private static bool ResponseIndicatesFieldValueAlreadyExists(string responseText)
        {
            return !string.IsNullOrWhiteSpace(responseText)
                && responseText.IndexOf("FIELD_VALUE_ALREADY_EXISTS", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private Task<AconexRegisterSearchResult> SearchTargetRegisterByQueryAsync(
            TransmittalSyncRunRequest request,
            string targetProjectId,
            string searchQuery,
            IReadOnlyList<string> returnFields,
            CancellationToken cancellationToken)
        {
            return _registerSearch.SearchRegisterPageAsync(
                request.BaseUrl,
                targetProjectId,
                request.OrgId ?? "",
                request.UserId ?? "",
                request.AuthorizationHeaderBase64,
                returnFields,
                25,
                1,
                throwIfNotSuccess: false,
                cancellationToken,
                searchQuery: searchQuery);
        }

        /// <summary>Codelco vs SALFA: listas distintas de returnFields extra (no mezclar project fields).</summary>
        private static IReadOnlyList<string> ResolveConfiguredExtraFieldsForTargetSupersedeSearch(
            TransmittalSyncRunRequest request,
            ProyectoSyncItem targetProject)
        {
            if (request == null || targetProject == null)
                return null;

            string targetId = targetProject.ProjectId?.Trim();
            string salfaId = request.IdProyecto2?.Trim();
            if (!string.IsNullOrWhiteSpace(targetId)
                && !string.IsNullOrWhiteSpace(salfaId)
                && string.Equals(targetId, salfaId, StringComparison.OrdinalIgnoreCase))
                return request.CamposConsultaRegistroDestinoSalfa;

            return request.CamposConsultaRegistroDestino;
        }

        private static IReadOnlyList<string> BuildTargetRegisterExistenceSearchReturnFields()
        {
            return new[]
            {
                "docno",
                "revision",
                "versionnumber",
                "title",
                "revisiondate",
                "doctype",
                "statusid",
                "author",
                "reviewstatus",
                "discipline"
            };
        }

        /// <summary>returnFields para búsqueda por <see cref="CodelcoBridgeField"/> (proyecto SALFA).</summary>
        private static IReadOnlyList<string> BuildTargetRegisterBridgeSearchReturnFields()
        {
            var fields = new List<string>(BuildTargetRegisterExistenceSearchReturnFields());
            fields.Add(CodelcoBridgeField);
            return fields;
        }

        private static IReadOnlyList<string> BuildTargetRegisterSearchReturnFields(
            AconexRegisterSchemaSnapshot targetSchema,
            IReadOnlyList<string> configuredExtraFields,
            IReadOnlyList<TransmittalSyncCampoMapeoItem> fieldMappings = null)
        {
            var fields = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "docno",
                "revision",
                "versionnumber",
                "title",
                "revisiondate",
                "doctype",
                "statusid",
                "author",
                "reviewstatus",
                "discipline"
            };

            // Project fields MANDATORY del schema destino (SALFA o Codelco según dirección).
            if (targetSchema?.Fields != null)
            {
                foreach (AconexRegisterSchemaField field in targetSchema.Fields)
                {
                    if (field == null || string.IsNullOrWhiteSpace(field.Identifier))
                        continue;
                    if (!string.Equals(field.MandatoryStatus, "MANDATORY", StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (!field.Identifier.EndsWith("_singleSelect", StringComparison.OrdinalIgnoreCase)
                        && !field.Identifier.EndsWith("_singleLineText", StringComparison.OrdinalIgnoreCase)
                        && !field.Identifier.EndsWith("_multiLineText", StringComparison.OrdinalIgnoreCase))
                        continue;
                    AddRegisterSearchReturnField(fields, field.Identifier.Trim());
                }
            }

            if (fieldMappings != null)
            {
                foreach (TransmittalSyncCampoMapeoItem map in fieldMappings)
                {
                    if (map == null || string.IsNullOrWhiteSpace(map.CampoDestino))
                        continue;
                    AddRegisterSearchReturnField(fields, map.CampoDestino.Trim());
                }
            }

            if (configuredExtraFields != null)
            {
                foreach (string extra in configuredExtraFields)
                    AddRegisterSearchReturnField(fields, extra);
            }

            return fields.ToList();
        }

        private static void AddRegisterSearchReturnField(HashSet<string> fields, string fieldName)
        {
            if (fields == null || string.IsNullOrWhiteSpace(fieldName))
                return;
            string trimmed = fieldName.Trim();
            if (ProjectSyncCampoOrigenTokens.IsSyntheticToken(trimmed))
                return;
            if (!IsRegisterSearchReturnFieldName(trimmed))
                return;
            string apiField = ToRegisterSearchApiFieldFromDestino(trimmed);
            fields.Add(!string.IsNullOrWhiteSpace(apiField) ? apiField : trimmed);
        }

        private static bool IsRegisterSearchReturnFieldName(string fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
                return false;
            if (!string.IsNullOrWhiteSpace(ToRegisterSearchApiFieldFromDestino(fieldName)))
                return true;
            return fieldName.EndsWith("_singleSelect", StringComparison.OrdinalIgnoreCase)
                || fieldName.EndsWith("_singleLineText", StringComparison.OrdinalIgnoreCase)
                || fieldName.EndsWith("_multiLineText", StringComparison.OrdinalIgnoreCase);
        }

        private static string ToRegisterSearchApiFieldFromDestino(string campoDestino)
        {
            if (string.IsNullOrWhiteSpace(campoDestino))
                return null;

            switch (campoDestino.Trim().ToLowerInvariant())
            {
                case "documentnumber":
                case "docno":
                    return "docno";
                case "title":
                    return "title";
                case "revision":
                    return "revision";
                case "documenttypeid":
                case "doctype":
                case "documenttype":
                    return "doctype";
                case "documentstatusid":
                case "statusid":
                case "documentstatus":
                    return "statusid";
                case "author":
                    return "author";
                case "revisiondate":
                    return "revisiondate";
                case "reviewstatusid":
                case "reviewstatus":
                    return "reviewstatus";
                case "discipline":
                    return "discipline";
                default:
                    if (campoDestino.EndsWith("_singleSelect", StringComparison.OrdinalIgnoreCase)
                        || campoDestino.EndsWith("_multiLineText", StringComparison.OrdinalIgnoreCase)
                        || campoDestino.EndsWith("_singleLineText", StringComparison.OrdinalIgnoreCase))
                        return campoDestino.Trim();
                    return null;
            }
        }

        private static void MergeRegisterMetadataHints(IDictionary<string, string> hints, DocumentMetadata metadata)
        {
            if (hints == null || metadata == null)
                return;

            AddHint(hints, "DocumentNumber", metadata.DocumentNumber);
            AddHint(hints, "Title", metadata.Title);
            AddHint(hints, "Revision", metadata.Revision);
            AddHint(hints, "RevisionDate", metadata.RevisionDate);
            AddHint(hints, "revisiondate", metadata.RevisionDate);
            AddHint(hints, "doctype", metadata.DocumentType);
            AddHint(hints, "DocumentType", metadata.DocumentType);
            AddHint(hints, "statusid", metadata.DocumentStatus);
            AddHint(hints, "Status", metadata.DocumentStatus);
            AddHint(hints, "DocumentStatus", metadata.DocumentStatus);
            AddHint(hints, "discipline", metadata.Discipline);
            AddHint(hints, "author", metadata.Author);
            AddHint(hints, "reviewstatus", metadata.ReviewStatus);
            AddHint(hints, "ReviewStatusId", metadata.ReviewStatus);
            AddHint(hints, "SelectList1", metadata.SelectList1);
            AddHint(hints, "SelectList2", metadata.SelectList2);
            AddHint(hints, "SelectList3", metadata.SelectList3);
            AddHint(hints, "ProjectField1", metadata.ProjectField1);
            AddHint(hints, "ProjectField2", metadata.ProjectField2);
        }

        private Task<AconexRegisterSearchResult> SearchSourceRegisterAsync(
            TransmittalSyncRunRequest request,
            ProyectoSyncItem sourceProject,
            TransmittalDocumentAttachment attachment,
            string revision,
            IReadOnlyList<string> returnFields,
            CancellationToken cancellationToken)
        {
            return _registerSearch.SearchRegisterPageAsync(
                request.BaseUrl,
                sourceProject.ProjectId,
                request.OrgId ?? "",
                request.UserId ?? "",
                request.AuthorizationHeaderBase64,
                returnFields,
                25,
                1,
                throwIfNotSuccess: false,
                cancellationToken,
                filterDocumentNo: attachment.DocumentNo.Trim(),
                filterRevision: IsWildcardRevision(revision) ? null : NormalizeRevision(revision));
        }

        /// <summary>Campos API estándar (sin *_singleSelect del destino que pueden no existir en origen).</summary>
        private static IReadOnlyList<string> BuildCoreRegisterSearchReturnFields(IReadOnlyList<TransmittalSyncCampoMapeoItem> fieldMappings)
        {
            var all = BuildRegisterSearchReturnFields(fieldMappings);
            return all
                .Where(f => !f.EndsWith("_singleSelect", StringComparison.OrdinalIgnoreCase)
                         && !f.EndsWith("_multiLineText", StringComparison.OrdinalIgnoreCase)
                         && !f.EndsWith("_singleLineText", StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        /// <summary>
        /// Campos a pedir en register/search del origen. Mapea homologación → nombres API (docno, title, …).
        /// Omite destinos solo-transmittal (AutoNumber) y nombres XML inválidos (DocumentNumber).
        /// </summary>
        private static IReadOnlyList<string> BuildRegisterSearchReturnFields(IReadOnlyList<TransmittalSyncCampoMapeoItem> fieldMappings)
        {
            var fields = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "docno",
                "revision"
            };

            if (fieldMappings != null)
            {
                foreach (TransmittalSyncCampoMapeoItem map in fieldMappings)
                {
                    if (map == null)
                        continue;
                    string apiField = ToRegisterSearchApiField(map.CampoOrigen, map.CampoDestino);
                    if (!string.IsNullOrWhiteSpace(apiField))
                        fields.Add(apiField);
                }
            }
            else
            {
                fields.Add("revisiondate");
                fields.Add("doctype");
                fields.Add("statusid");
                fields.Add("discipline");
                fields.Add("author");
                fields.Add("title");
                fields.Add("reviewstatus");
            }

            return fields.ToList();
        }

        /// <summary>Nombre en returnFields del POST register/search (consulta), no tag XML destino.</summary>
        private static string ToRegisterSearchApiField(string campoOrigen, string campoDestino)
        {
            string key = !string.IsNullOrWhiteSpace(campoOrigen) ? campoOrigen.Trim() : campoDestino?.Trim();
            if (string.IsNullOrWhiteSpace(key))
                return null;

            switch (key.ToLowerInvariant())
            {
                case "documentnumber":
                case "docno":
                    return "docno";
                case "title":
                    return "title";
                case "revision":
                    return "revision";
                case "documenttypeid":
                case "doctype":
                case "documenttype":
                    return "doctype";
                case "documentstatusid":
                case "statusid":
                case "documentstatus":
                    return "statusid";
                case "author":
                    return "author";
                case "revisiondate":
                    return "revisiondate";
                case "reviewstatusid":
                case "reviewstatus":
                    return "reviewstatus";
                case "discipline":
                    return "discipline";
                case "autonumber":
                case "hasfile":
                case "id":
                case "mailno":
                case "mailid":
                case "subject":
                case "referencenumber":
                    return null;
                default:
                    if (key.EndsWith("_singleSelect", StringComparison.OrdinalIgnoreCase)
                        || key.EndsWith("_multiLineText", StringComparison.OrdinalIgnoreCase)
                        || key.EndsWith("_singleLineText", StringComparison.OrdinalIgnoreCase))
                        return key;
                    return null;
            }
        }

        private static void AddHint(IDictionary<string, string> hints, string key, string value)
        {
            if (hints == null || string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
                return;
            hints[key] = value.Trim();
        }

        private static void LogRegisterHttpDebug(
            TransmittalSyncRunRequest request,
            ProyectoSyncItem targetProject,
            string multipartBody,
            string boundary,
            string xmlDocument,
            string label,
            Action<string> log,
            bool isSupersede = false,
            string localDocumentId = null)
        {
            if (log == null)
                return;

            string root = string.IsNullOrWhiteSpace(request?.BaseUrl) ? "https://us1.aconex.com" : request.BaseUrl.TrimEnd('/');
            string url = isSupersede
                ? $"{root}/api/projects/{targetProject.ProjectId}/register/{localDocumentId}/supersede"
                : $"{root}/api/projects/{targetProject.ProjectId}/register";

            LogSingleSelectEncodingWarnings(xmlDocument, log);

            log($"REGISTER HTTP ({label}): POST {url}");
            log($"REGISTER multipart ({label}, {multipartBody?.Length ?? 0} chars):{Environment.NewLine}{FormatMultipartBodyForLog(multipartBody)}");
            log(
                $"REGISTER curl ({label}):{Environment.NewLine}" +
                AconexRegisterMultipart.FormatCurlEquivalent(
                    "POST",
                    url,
                    request?.AuthorizationHeaderBase64,
                    request?.IntegrationId,
                    multipartBody,
                    boundary));
        }

        private static void LogSingleSelectEncodingWarnings(string xmlDocument, Action<string> log)
        {
            if (string.IsNullOrEmpty(xmlDocument) || log == null)
                return;

            foreach (string field in new[] { "TipoDeDocumento_singleSelect", "Discipline_singleSelect", "Cwa_singleSelect" })
                LogXmlFieldEncodingWarning(xmlDocument, field, log);
        }

        private static void LogXmlFieldEncodingWarning(string xml, string fieldName, Action<string> log)
        {
            string open = "<" + fieldName + ">";
            string close = "</" + fieldName + ">";
            int start = xml.IndexOf(open, StringComparison.Ordinal);
            if (start < 0)
                return;

            start += open.Length;
            int end = xml.IndexOf(close, start, StringComparison.Ordinal);
            if (end < 0)
                return;

            string value = xml.Substring(start, end - start);
            string issue = AconexRegisterMultipart.DescribeMojibakeSequences(value);
            if (!string.IsNullOrEmpty(issue))
                log($"  AVISO encoding en {fieldName}='{value}': {issue}");
        }

        private static string FormatMultipartBodyForLog(string multipartBody)
        {
            if (string.IsNullOrEmpty(multipartBody))
                return "";

            const int maxChars = 12000;
            if (multipartBody.Length <= maxChars)
                return multipartBody;

            int fileHeader = multipartBody.IndexOf("X-Filename:", StringComparison.Ordinal);
            if (fileHeader > 0)
                return multipartBody.Substring(0, fileHeader) +
                       $"...(parte binaria/base64 omitida, {multipartBody.Length - fileHeader} chars)";

            return Truncate(multipartBody, maxChars);
        }

        private static string Truncate(string text, int max)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= max)
                return text ?? "";
            return text.Substring(0, max) + "...";
        }

        private static void TryDeleteFile(string path)
        {
            try
            {
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                    File.Delete(path);
            }
            catch
            {
                // ignorar limpieza temp
            }
        }
    }

    public sealed class TransmittalSyncProjectResult
    {
        public string SourceProjectId { get; set; }
        public string TargetProjectId { get; set; }
        public string Mailbox { get; set; }
        public int TotalMails { get; set; }
        public int ProcessedMails { get; set; }
        public int SkippedAlreadyProcessed { get; set; }
        public int SkippedSubjectFilter { get; set; }
        public int PlaceholdersCreated { get; set; }
        public int FilesApplied { get; set; }
        public int Errors { get; set; }
    }
}
