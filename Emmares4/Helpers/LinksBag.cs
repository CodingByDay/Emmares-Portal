using System.Collections.Generic;

namespace Emmares4.Helpers
{
    public class LinksBag
    {
        private static Dictionary<string, Queue<string>> links = new Dictionary<string, Queue<string>>();

        public static void Add(string user, string url)
        {
            lock (links)
            {
                if (!links.ContainsKey(user))
                {
                    links.Add(user, new Queue<string>());
                }
            }
            links[user].Enqueue(url);
        }

        public static string Get (string user)
        {
            if (links.ContainsKey(user))
            {
                string url;
                if (links[user].TryDequeue(out url))
                {
                    return url;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }
    }
}
