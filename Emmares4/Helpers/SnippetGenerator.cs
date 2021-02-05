using System;
using System.IO;
using System.Text;

using Emmares4.Models;

namespace Emmares4.Helpers
{
    public class SnippetGenerator
    {
        public static string Get (Campaign c)
        {
            if ((c == null) || (c.ID == null) || c.ID.ToString () == Guid.Empty.ToString ())
            {
                return "Snippet will be available once campaign is generated.";
            }
            else
            {
                var root = Paths.ContentRootDir;
                var path = Path.Combine(root, @"App_Data\Footer");
                var footerFile = Path.Combine(path, "footer.html");
                using (var sr = new StreamReader(footerFile, Encoding.UTF8))
                {
                    var snippet = sr.ReadToEnd();
                    snippet = snippet.Replace("{{cid}}", c.ID.ToString());
                    if (string.IsNullOrWhiteSpace (Paths.BaseUrl)) { throw new ApplicationException("Base URL undefined!?"); }
                    snippet = snippet.Replace("{{baseUrl}}", Paths.BaseUrl);
                    return snippet;
                }
            }
        }
    }
}
