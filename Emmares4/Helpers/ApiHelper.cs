using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Emmares4.Helpers
{
    public static class ApiHelper
    {


        /// <summary>
        ///  Gets the substring before a certain char,
        ///  if char is not present within the given resource, 
        ///  method returns the whole string. If the resource is NULL or whitespace, 
        ///  method returns String.Empty object.
        /// </summary>
        /// <param name="text">The string that needs to be split.</param>
        /// <param name="stopAt">Split at this char delimeter.</param>
        /// <returns></returns>
        public static string GetUntilOrEmpty(this string text, string stopAt = "-")
        {
            if (!String.IsNullOrWhiteSpace(text))
            {
                int charLocation = text.IndexOf(stopAt, StringComparison.Ordinal);

                if (charLocation > 0)
                {
                    return text.Substring(0, charLocation);
                }
            }

            return String.Empty;
        }
    }
}
