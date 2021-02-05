using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Emmares4.Helpers
{
    public class SetFavoriteDictionary
    {
        private static Dictionary<string, string> favorites = new Dictionary<string, string>();

        public static void Set(string userGuid, string favorite)
        {
            favorites[userGuid] = favorite;
        }

        public static string Get (string userGuid)
        {
            return favorites.ContainsKey(userGuid) ? favorites[userGuid] : null;
        }
    }
}
