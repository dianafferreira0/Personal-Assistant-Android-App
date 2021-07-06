using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Didimo.Utils
{
    public class StringUtils
    {
#if UNITY_EDITOR
        public static string ScriptableObjectFolderPath(ScriptableObject scriptableObject)
        {
            UnityEditor.MonoScript script = UnityEditor.MonoScript.FromScriptableObject(scriptableObject);
            string scriptPath = UnityEditor.AssetDatabase.GetAssetPath(script);
            return Path.GetDirectoryName(scriptPath);
        }

#endif

        /// <summary>
        /// Find the range of a given pattern in a string.
        /// </summary>
        /// <param name="source">The source string.</param>
        /// <param name="pattern">The pattern to look for.</param>
        /// <param name="start">The start of the range. If pattern is not found, returns -1.</param>
        /// <param name="end">The end of the range. If pattern is not found, returns -1.</param>
        /// <returns>True if pattern is found, false otherwise.</returns>
        public static bool FindRange(string source, string pattern, out int start, out int end)
        {
            Match match = Regex.Match(source, pattern);

            if (match == null)
            {
                start = -1;
                end = -1;
                return false;
            }

            start = match.Groups[0].Captures[0].Index;
            end = start + match.Groups[0].Captures[0].Length;

            return true;
        }

        public static string EncodeNonAsciiCharacters(string value)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in value)
            {
                if (c > 127)
                {
                    // This character is too big for ASCII
                    string encodedValue = "\\u" + ((int)c).ToString("x4");
                    sb.Append(encodedValue);
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        public static string DecodeEncodedNonAsciiCharacters(string value)
        {
            return Regex.Replace(
                value,
                @"\\u(?<Value>[a-zA-Z0-9]{4})",
                m => {
                    return ((char)int.Parse(m.Groups["Value"].Value, System.Globalization.NumberStyles.HexNumber)).ToString();
                });
        }
    }
}