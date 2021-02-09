using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Emmares4.Helpers
{
    public class RegexUtils
    {
        // A regular expression for validating slugs.
        // Does not allow leading or trailing hypens or whitespace.
        public static readonly Regex SlugRegex = new Regex(@"(^[a-z0-9])([a-z0-9_-]+)*([a-z0-9])$");

    }
}
