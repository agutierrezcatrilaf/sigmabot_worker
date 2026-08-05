using System.Xml;

namespace SigmabotSync.Application.Common
{
    public static class AconexRegisterResponseParser
    {
        public static string ParseRegisterDocumentNumber(string responseText)
        {
            if (string.IsNullOrWhiteSpace(responseText))
                return null;
            try
            {
                var doc = new XmlDocument();
                doc.LoadXml(responseText);

                XmlNode numberNode =
                    doc.SelectSingleNode("//*[local-name()='RegisterDocumentResult']/*[local-name()='DocumentNumber']")
                    ?? doc.SelectSingleNode("//*[local-name()='DocumentNumber']")
                    ?? doc.SelectSingleNode("//RegisterDocumentResult/DocumentNumber");
                if (numberNode != null && !string.IsNullOrWhiteSpace(numberNode.InnerText))
                    return numberNode.InnerText.Trim();

                return null;
            }
            catch
            {
                return null;
            }
        }

        public static string ParseRegisterDocumentId(string responseText)
        {
            if (string.IsNullOrWhiteSpace(responseText))
                return null;
            try
            {
                var doc = new XmlDocument();
                doc.LoadXml(responseText);

                // Preferir hijo DocumentId (InnerText del contenedor concatena ej. "…idtrue").
                XmlNode idNode =
                    doc.SelectSingleNode("//*[local-name()='RegisterDocumentResult']/*[local-name()='DocumentId']")
                    ?? doc.SelectSingleNode("//*[local-name()='DocumentId']")
                    ?? doc.SelectSingleNode("//RegisterDocumentResult/DocumentId");
                if (idNode != null && !string.IsNullOrWhiteSpace(idNode.InnerText))
                    return idNode.InnerText.Trim();

                XmlNode node = doc.SelectSingleNode("//RegisterDocumentResult")
                    ?? doc.SelectSingleNode("/*[local-name()='RegisterDocumentResult']");
                string raw = node?.InnerText?.Trim();
                if (string.IsNullOrWhiteSpace(raw))
                    return null;

                // Fallback: solo dígitos iniciales si el texto trae basura concatenada.
                int end = 0;
                while (end < raw.Length && char.IsDigit(raw[end]))
                    end++;
                return end > 0 ? raw.Substring(0, end) : raw;
            }
            catch
            {
                return null;
            }
        }
    }
}
