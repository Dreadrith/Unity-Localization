using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace DreadScripts.Localization
{
    public static class LocalizationStringUtility
    {
        public static bool ICContains(string a, string b) => a.IndexOf(b, StringComparison.OrdinalIgnoreCase) >= 0;
        public static string Quote(string input) => $"\"{input}\"";
        public static string Unquote(string input) => input.StartsWith("\"") && input.EndsWith("\"") ? input.Substring(1, input.Length - 2)  : input;
        public static string EscapeQuote(string input) => input.Replace("\"", "\\\"");
        public static string UnescapeQuote(string input) => input.Replace("\\\"", "\"");
        
        public static string EscapeNewLines(string input) => Regex.Replace(input, @"\r\n?|\n", "\\n");
        public static string UnescapeNewLines(string input) => Regex.Replace(input, @"\\n", "\n");
        
        public static string LiteEscape(string input) => EscapeQuote(EscapeNewLines(input));
        public static string LiteUnescape(string input) => UnescapeNewLines(UnescapeQuote(input));
        
        public static string EscapeAndQuote(string input) => Quote(LiteEscape(input));
        public static string UnquoteAndUnescape(string input) => LiteUnescape(Unquote(input));
    }
}

