using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using SigmabotSync.Domain.Models.Synchronization;
using SigmabotSync.Domain.Ports;

namespace SigmabotSync.Infrastructure.External
{
    /// <summary>List Mail (inbox/sentbox, corrtypeid configurable) + View Mail Metadata para ProjectSync.</summary>
    public sealed class AconexMailTransmittalAdapter : IMailTransmittalReadPort
    {
        private const string MailContentType = "application/vnd.aconex.mail.v3+xml";
        private const string LegacyMailApplicationKey = "a7f7bf46-a848-4b7a-ae8c-ed55b3952010";
        private const int PageSize = 300;
        private const int DefaultCorrTypeId = 23;

        private readonly IAconexHttpGetPort _httpGet;

        public AconexMailTransmittalAdapter(IAconexHttpGetPort httpGet)
        {
            _httpGet = httpGet ?? throw new ArgumentNullException(nameof(httpGet));
        }

        public async Task<IReadOnlyList<TransmittalMailSummary>> ListTransmittalsAsync(
            string baseUrl,
            string projectId,
            string authorizationHeaderBase64,
            DateTime desdeUtc,
            DateTime hastaUtc,
            string mailbox,
            int corrTypeId = DefaultCorrTypeId,
            CancellationToken cancellationToken = default)
        {
            string mailBox = string.IsNullOrWhiteSpace(mailbox) ? "inbox" : mailbox.Trim().ToLowerInvariant();
            int corrId = corrTypeId > 0 ? corrTypeId : DefaultCorrTypeId;
            string root = NormalizeBaseUrl(baseUrl);
            string fechaInicio = desdeUtc.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
            string fechaFin = hastaUtc.ToString("yyyyMMdd", CultureInfo.InvariantCulture);

            long totalPages = await GetTotalPagesAsync(root, projectId, authorizationHeaderBase64, fechaInicio, fechaFin, mailBox, corrId, cancellationToken)
                .ConfigureAwait(false);
            if (totalPages <= 0)
                return Array.Empty<TransmittalMailSummary>();

            var results = new List<TransmittalMailSummary>();
            for (long page = 1; page <= totalPages; page++)
            {
                string pageXml = await GetPageXmlAsync(root, projectId, authorizationHeaderBase64, fechaInicio, fechaFin, mailBox, corrId, page, cancellationToken)
                    .ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(pageXml))
                    continue;

                ParseMailSearchPage(pageXml, results);
            }

            return results;
        }

        public async Task<TransmittalMailDetail> GetTransmittalDetailAsync(
            string baseUrl,
            string projectId,
            string mailId,
            string authorizationHeaderBase64,
            CancellationToken cancellationToken = default)
        {
            string root = NormalizeBaseUrl(baseUrl);
            string url = $"{root}/api/projects/{projectId}/mail/{mailId}";

            string responseXml = await _httpGet.GetStringAsync(new AconexHttpGetRequest
            {
                Url = url,
                AuthorizationHeaderBase64 = authorizationHeaderBase64,
                Accept = "application/xml",
                ContentType = MailContentType,
                ExtraHeaders = new[] { ("X-Application-Key", LegacyMailApplicationKey) }
            }, cancellationToken).ConfigureAwait(false);

            return ParseMailDetail(responseXml, mailId);
        }

        private async Task<long> GetTotalPagesAsync(
            string root,
            string projectId,
            string authorizationHeaderBase64,
            string fechaInicio,
            string fechaFin,
            string mailbox,
            int corrTypeId,
            CancellationToken cancellationToken)
        {
            string url = BuildListMailUrl(root, projectId, fechaInicio, fechaFin, mailbox, corrTypeId, pageNumber: null);
            string responseXml = await _httpGet.GetStringAsync(new AconexHttpGetRequest
            {
                Url = url,
                AuthorizationHeaderBase64 = authorizationHeaderBase64,
                Accept = "application/xml",
                ContentType = MailContentType,
                ExtraHeaders = null
            }, cancellationToken).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(responseXml))
                return 0;

            var doc = new XmlDocument();
            doc.LoadXml(CleanXml(responseXml));
            string totalResultsText = doc.SelectSingleNode("MailSearch")?.Attributes?["TotalResults"]?.InnerText ?? "0";
            if (!long.TryParse(totalResultsText, out long totalResults) || totalResults <= 0)
                return 0;

            string totalPagesText = doc.SelectSingleNode("MailSearch")?.Attributes?["TotalPages"]?.InnerText ?? "0";
            return long.TryParse(totalPagesText, out long totalPages) ? totalPages : 0;
        }

        private Task<string> GetPageXmlAsync(
            string root,
            string projectId,
            string authorizationHeaderBase64,
            string fechaInicio,
            string fechaFin,
            string mailbox,
            int corrTypeId,
            long pageNumber,
            CancellationToken cancellationToken)
        {
            string url = BuildListMailUrl(root, projectId, fechaInicio, fechaFin, mailbox, corrTypeId, pageNumber);
            return _httpGet.GetStringAsync(new AconexHttpGetRequest
            {
                Url = url,
                AuthorizationHeaderBase64 = authorizationHeaderBase64,
                Accept = "application/xml",
                ContentType = MailContentType,
                ExtraHeaders = new[] { ("X-Application-Key", LegacyMailApplicationKey) }
            }, cancellationToken);
        }

        private static string BuildListMailUrl(string root, string projectId, string fechaInicio, string fechaFin, string mailbox, int corrTypeId, long? pageNumber)
        {
            string mailBox = string.IsNullOrWhiteSpace(mailbox) ? "inbox" : mailbox.Trim().ToLowerInvariant();
            int corrId = corrTypeId > 0 ? corrTypeId : DefaultCorrTypeId;
            string url = $"{root}/api/projects/{projectId}/mail?mail_box={mailBox}" +
                         "&return_fields=docno,sentdate,subject,fromUserDetails,inreftomailno,corrtypeid,hasAttachments" +
                         "&page_size=" + PageSize +
                         "&search_type=PAGED" +
                         $"&search_query=corrtypeid:{corrId} AND sentdate:[{fechaInicio} TO {fechaFin}]";
            if (pageNumber.HasValue)
                url += "&page_number=" + pageNumber.Value;
            return url;
        }

        private static void ParseMailSearchPage(string pageXml, IList<TransmittalMailSummary> results)
        {
            var doc = new XmlDocument();
            doc.LoadXml(CleanXml(pageXml));

            XmlNodeList mails = doc.SelectSingleNode("MailSearch")?
                .SelectSingleNode("SearchResults")?
                .SelectNodes("Mail");
            if (mails == null)
                return;

            foreach (XmlElement mailNode in mails)
            {
                results.Add(new TransmittalMailSummary
                {
                    MailId = mailNode.GetAttribute("MailId"),
                    MailNo = mailNode.SelectSingleNode("MailNo")?.InnerText ?? "",
                    Subject = mailNode.SelectSingleNode("Subject")?.InnerText ?? "",
                    ReferenceNumber = mailNode.SelectSingleNode("ReferenceNumber")?.InnerText ?? "",
                    SentDate = ParseSentDate(mailNode.SelectSingleNode("SentDate")?.InnerText),
                    FromOrganizationName = mailNode.SelectSingleNode("FromUserDetails/OrganizationName")?.InnerText ?? "",
                    FromUserName = mailNode.SelectSingleNode("FromUserDetails/Name")?.InnerText ?? ""
                });
            }
        }

        private static TransmittalMailDetail ParseMailDetail(string responseXml, string mailId)
        {
            var doc = new XmlDocument();
            doc.LoadXml(CleanXml(responseXml));

            XmlElement mailNode = doc.SelectSingleNode("Mail") as XmlElement;
            var detail = new TransmittalMailDetail
            {
                MailId = mailNode?.GetAttribute("MailId") ?? mailId,
                MailNo = mailNode?.SelectSingleNode("MailNo")?.InnerText ?? "",
                Subject = mailNode?.SelectSingleNode("Subject")?.InnerText ?? "",
                InRefToMailId = mailNode?.SelectSingleNode("InRefToMailId")?.InnerText ?? "",
                ThreadId = mailNode?.SelectSingleNode("ThreadId")?.InnerText ?? ""
            };

            XmlNodeList attachments = mailNode?
                .SelectSingleNode("Attachments")?
                .SelectNodes("RegisteredDocumentAttachment");
            if (attachments == null)
                return detail;

            foreach (XmlElement attachmentNode in attachments)
            {
                detail.Attachments.Add(new TransmittalDocumentAttachment
                {
                    AttachmentId = attachmentNode.GetAttribute("attachmentId"),
                    DocumentId = attachmentNode.SelectSingleNode("DocumentId")?.InnerText ?? "",
                    RegisteredAs = attachmentNode.SelectSingleNode("RegisteredAs")?.InnerText ?? "",
                    DocumentNo = attachmentNode.SelectSingleNode("DocumentNo")?.InnerText ?? "",
                    FileName = attachmentNode.SelectSingleNode("FileName")?.InnerText ?? "",
                    FileSize = ParseFileSize(attachmentNode.SelectSingleNode("FileSize")?.InnerText),
                    Revision = attachmentNode.SelectSingleNode("Revision")?.InnerText ?? "",
                    VersionNumber = attachmentNode.SelectSingleNode("VersionNumber")?.InnerText
                        ?? attachmentNode.SelectSingleNode("Version")?.InnerText ?? "",
                    RevisionDate = attachmentNode.SelectSingleNode("RevisionDate")?.InnerText ?? "",
                    Status = attachmentNode.SelectSingleNode("Status")?.InnerText ?? "",
                    Title = attachmentNode.SelectSingleNode("Title")?.InnerText ?? ""
                });
            }

            return detail;
        }

        private static long ParseFileSize(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return 0;
            return long.TryParse(raw.Trim(), out long size) ? size : 0;
        }

        private static DateTime? ParseSentDate(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;
            if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out DateTime dt))
                return dt;
            return null;
        }

        private static string NormalizeBaseUrl(string baseUrl)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
                return "https://us1.aconex.com";
            return baseUrl.TrimEnd('/');
        }

        private static string CleanXml(string xml)
        {
            if (string.IsNullOrEmpty(xml))
                return xml;
            return xml.Replace("\u0003", "");
        }
    }
}
