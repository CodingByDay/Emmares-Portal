using System.Text;

namespace Emmares4.Elastic
{
    public class CampaignContent
    {
        public string ID { get; set; }
        public string Content { get; set; }

        public CampaignContent(string id, string plainTextContent, string htmlContent)
        {
            this.ID = id;

            var html2Plain = "";
            if (!string.IsNullOrWhiteSpace (htmlContent))
            {
                html2Plain = StripHtml(htmlContent);
            }

            this.Content = plainTextContent + " " + html2Plain;
        }

        private static string StripHtml(string html)
        {
            html = StripSection(html, "style");
            html = StripSection(html, "script");
            html = StripTags(html);
            return html;
        }

        private static string StripSection(string html, string section)
        {
            var startAt = html.ToLower().IndexOf("<" + section.ToLower());
            while (startAt >= 0)
            {
                var secEnd = html.ToLower().IndexOf("</" + section.ToLower() + ">") + section.Length + 3;
                html = html.Substring(0, startAt) + " " + html.Substring(secEnd);
                startAt = html.ToLower().IndexOf("<" + section.ToLower());
            }
            return html;
        }

        private static string StripTags(string html)
        {
            var stripped = new StringBuilder(html.Length);
            var isTag = false;
            foreach (char c in html)
            {
                if (!isTag && c == '<') { isTag = true; }
                if (!isTag) { stripped.Append(c); }
                if (isTag && c == '>') { isTag = false; }
            }
            var final = stripped
                .ToString()
                .Replace("\t", " ")
                .Replace("\n", " ")
                .Replace("\r", " ");
            while (final.IndexOf ("  ") >= 0) { final = final.Replace("  ", " "); }
            return final;
        }
    }
}
