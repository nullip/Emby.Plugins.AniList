using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using FuzzySharp;
using System.Threading.Tasks;
using System.Xml.Linq;
using MediaBrowser.Model.Extensions;

namespace Emby.Anime
{
    internal static class Equals_check
    {

        private static string ReplaceSafe(this string str, string find, string replace, StringComparison comparison)
        {
            if (string.IsNullOrEmpty(find))
            {
                return str;
            }

            return str.Replace(find, replace, comparison);
        }

        /// <summary>
        /// Clear name
        /// </summary>
        /// <param name="a"></param>
        /// <returns></returns>
        public static string Clear_name(string a)
        {
            a = a.Trim().ReplaceSafe(One_line_regex(new Regex(@"(?s) \(.*?\)"), a.Trim(), 0), "", StringComparison.OrdinalIgnoreCase);

            a = a.Replace("\"", "", StringComparison.OrdinalIgnoreCase);
            a = a.Replace(".", " ", StringComparison.OrdinalIgnoreCase);
            a = a.Replace("-", " ", StringComparison.OrdinalIgnoreCase);
            a = a.Replace("`", "", StringComparison.OrdinalIgnoreCase);
            a = a.Replace("'", "", StringComparison.OrdinalIgnoreCase);
            a = a.Replace("&", "and", StringComparison.OrdinalIgnoreCase);
            a = a.Replace("(", "", StringComparison.OrdinalIgnoreCase);
            a = a.Replace(")", "", StringComparison.OrdinalIgnoreCase);

            a = a.ReplaceSafe(One_line_regex(new Regex(@"(?s)(S[0-9]+)"), a.Trim()), One_line_regex(new Regex(@"(?s)S([0-9]+)"), a.Trim()), StringComparison.OrdinalIgnoreCase);

            return a;
        }

        /// <summary>
        /// If a and b match it return true
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool Compare_strings(string a, string b)
        {
            if (!string.IsNullOrEmpty(a) && !string.IsNullOrEmpty(b))
            {
                if (Fuzz.WeightedRatio(a, b) >= 85) // Realistically should be 90-95% to prevent false positives, but will need aditional function check.  IDEA: Check for if there is only 1 result or multiple prior.
                {
                    return true;
                }
                return false;
            }
            return false;

        }

        public static bool Compare_strings_less_strict(string a, string b)
        {
            if (!string.IsNullOrEmpty(a) && !string.IsNullOrEmpty(b))
            {
                if (Fuzz.WeightedRatio(a, b) >= 70) // Realistically should be 90-95% to prevent false positives, but will need aditional function check.  IDEA: Check for if there is only 1 result or multiple prior.
                {
                    return true;
                }
                return false;
            }
            return false;

        }

        /// <summary>
        /// simple regex
        /// </summary>
        /// <param name="regex"></param>
        /// <param name="match"></param>
        /// <param name="group"></param>
        /// <param name="match_int"></param>
        /// <returns></returns>
        private static string One_line_regex(Regex regex, string match, int group = 1, int match_int = 0)
        {
            int x = 0;
            var matches = regex.Matches(match);

            foreach (Match _match in matches)
            {
                if (x > match_int)
                {
                    break;
                }
                if (x == match_int)
                {
                    return _match.Groups[group].Value.ToString();
                }
                x++;
            }
            return "";
        }
    }
}