// Copyright (c) Pixel Crushers. All rights reserved.

#if USE_OPENAI

using System.Globalization;
using System.Text.RegularExpressions;

namespace PixelCrushers.DialogueSystem.OpenAIAddon
{

    /// <summary>
    /// Utility methods for OpenAI addon.
    /// </summary>
    public static class AITextUtility
    {

        /// <summary>
        /// Gets English language name from a language code. 
        /// If can't determine English name, returns language code itself.
        /// </summary>
        public static string DetermineLanguage(string languageCode)
        {
            var cultureInfo = new CultureInfo(languageCode);
            return (cultureInfo == null || string.IsNullOrEmpty(cultureInfo.Name)) ? languageCode : cultureInfo.EnglishName;
        }

        public static string ExtractSpeaker(ref string line, DialogueDatabase database)
        {
            var speaker = string.Empty;

            // Match **speaker**: text
            var pattern = @"^\*\*[^\*]+:\*\* ";
            var match = Regex.Match(line, pattern);
            if (match.Success)
            {
                speaker = match.Value.Substring(2, match.Value.Length - 6);
                line = RemoveSurroundingQuotes(line.Substring(match.Value.Length));
                return speaker;
            }

            // Match <speaker>: text or <speaker> <says>: text
            var pos = line.IndexOf(":");
            if (pos != -1)
            {
                speaker = RemoveLastWord(line.Substring(0, pos));
                line = RemoveSurroundingQuotes(line.Substring(pos + 1));
                return speaker;
            }

            // Otherwise if line ends with a quote character,
            // grab text from first quote to end.
            if (line.EndsWith("\""))
            {
                var firstQuotePos = line.IndexOf("\"");
                speaker = RemoveLastWord(line.Substring(0, firstQuotePos));
                line = RemoveSurroundingQuotes(line.Substring(firstQuotePos));
                return speaker;
            }
            if (line.EndsWith("'"))
            {
                var firstQuotePos = line.IndexOf("'");
                speaker = RemoveLastWord(line.Substring(0, firstQuotePos));
                line = RemoveSurroundingQuotes(line.Substring(firstQuotePos));
                return speaker;
            }

            // Otherwise look for <speaker> says, text
            if (line.Contains(" says, "))
            {
                var saysPos = line.IndexOf(" says, ");
                speaker = line.Substring(0, saysPos).Trim();
                line = RemoveSurroundingQuotes(line.Substring(pos + " says, ".Length));
                return speaker;
            }

            // Fallback:
            return speaker;
        }

        public static string RemoveLastWord(string line)
        {
            var lastSpacePos = line.LastIndexOf(' ');
            if (lastSpacePos != -1) return line.Substring(0, lastSpacePos).Trim();
            return line.Trim();
        }

        /// <summary>
        /// Removes "speaker:", "speaker says:", or surrounding quotes around lines.
        /// </summary>
        public static string RemoveSpeaker(string speaker, string line)
        {
            // Match <speaker>: text
            if (line.StartsWith($"{speaker}:"))
            {
                return RemoveSurroundingQuotes(line.Substring(speaker.Length + 2));
            }

            // Match **speaker**: text
            if (line.StartsWith($"**{speaker}**:"))
            {
                return RemoveSurroundingQuotes(line.Substring(speaker.Length + 6));
            }

            // Match <speaker> <says>: text
            // where <says> could be any word and text could have surrounding quotes.
            var pattern = $"^{speaker} \\w+[:,] ";
            var match = Regex.Match(line, pattern);
            if (match.Success)
            {
                return RemoveSurroundingQuotes(line.Substring(match.Value.Length));
            }

            // Otherwise if line ends with a quote character,
            // grab text from first quote to end.
            if (line.EndsWith("\""))
            {
                var pos = line.IndexOf('"');
                return RemoveSurroundingQuotes(line.Substring(pos));
            }
            if (line.EndsWith("'"))
            {
                var pos = line.IndexOf(" '");
                return RemoveSurroundingQuotes(line.Substring(pos + 1));
            }

            // Otherwise look for <speaker> says, text
            if (line.Contains(" says, "))
            {
                var pos = line.IndexOf(" says, ");
                return RemoveSurroundingQuotes(line.Substring(pos + " says, ".Length));
            }

            // Fallback:
            return line;
        }

        /// <summary>
        /// Removes double quotes around string if present.
        /// </summary>
        public static string RemoveSurroundingQuotes(string text)
        {
            if (text.StartsWith("\"") || text.StartsWith("'"))
            {
                return text.Substring(1, text.Length - 2);
            }
            else
            {
                return text;
            }
        }

        public static string DoubleQuotesToSingle(string text)
        {
            return text.Replace('"', '\'');
        }

    }

}

#endif
