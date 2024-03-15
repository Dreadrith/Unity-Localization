using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace DreadScripts.Localization
{
    public static class LocalizationStringUtility
    {
        public static string Quote(string input) => $"\"{input.Replace("\"","\\\"")}\"";
        public static string Unquote(string input) => UnescapeQuote(input.Substring(1, input.Length - 2));
        public static string UnescapeQuote(string input) => input.Replace("\\\"", "\"");
        
        public static bool ICContains(string a, string b) => a.IndexOf(b, StringComparison.OrdinalIgnoreCase) >= 0;
        public static string EscapeNewLines(string input) => Regex.Replace(input, @"\r\n?|\n", "\\n");
        public static string UnescapeNewLines(string input) => Regex.Replace(input, @"\\n", "\n");
    }
}

