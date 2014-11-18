using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FreddiChatClient {

    public static class UrlExtractor {

        private static readonly Regex matcher = new Regex(@"\b(?:https?://|www\.)\S+\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Process the specified <code>text</code> and convert to a list of key-value pairs.
        /// The key is a partial text. The value specifies if the partial text is a URL or not.
        /// The partial texts are returned in the same order as they occured.
        /// </summary>
        /// <param name="text">The text to extract URLs from</param>
        /// <returns>A list of partial text (key-value pairs) where the value specifies if the of the key is a URL or not</returns>
        public static List<KeyValuePair<string, bool>> Extract(string text) {
            var extracted = new List<KeyValuePair<string, bool>>();

            var lastMatchIndex = 0;

            foreach (Match match in matcher.Matches(text)) {
                // Add non-url (the text up until this match)
                extracted.Add(new KeyValuePair<string, bool>(text.Substring(0, match.Index), false));

                // Add url
                extracted.Add(new KeyValuePair<string, bool>(text.Substring(match.Index, match.Length), true));

                lastMatchIndex = match.Index + match.Length;
            }

            // Add non-url (the remaining text)
            extracted.Add(new KeyValuePair<string, bool>(text.Substring(lastMatchIndex), false));

            return extracted;
        }

    }

}
