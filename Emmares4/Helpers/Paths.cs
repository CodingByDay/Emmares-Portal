using System.Linq;
using System.Collections.Generic;

namespace Emmares4.Helpers
{
    public class Paths
    {
        public static string ContentRootDir { get; set; }

        public static string BaseUrl { get; set; }

        public static string BasePath
        {
            get
            {
                var parts = BaseUrl.Split('/').ToList();
                var paths = new List<string>();
                for (int i = 3; i < parts.Count; i++)
                {
                    paths.Add(parts[i]);
                }
                var path = string.Join("/", paths);
                if (!path.EndsWith("/")) { path += "/"; }
                if (!path.StartsWith("/")) { path = "/" + path; }
                return path;
            }
        }
    }
}
