﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FreddiChatClient {

    public static class UrlExtractor {

        private static readonly Regex matcher = new Regex(@"\b(?:https?://|www\.)\S+\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly List<string> validUriSchemes = new List<string> { Uri.UriSchemeFile, Uri.UriSchemeFtp, Uri.UriSchemeHttp, Uri.UriSchemeHttps, Uri.UriSchemeMailto };

        /// <summary>
        /// Process the specified <code>text</code> and convert to a list of key-value pairs.
        /// The key is a partial text. The value is a URI if the partial text is a URL, <code>null</code> otherwise.
        /// The partial texts are returned in the same order as they occured.
        /// </summary>
        /// <remarks>Converts invalid "www" links to "http://www"</remarks>
        /// <param name="text">The text to extract URLs from</param>
        /// <returns>A list of partial text (key-value pairs) where the value is a URI if the partial text is a valid URL</returns>
        public static List<KeyValuePair<string, Uri>> Extract(string text) {
            var extracted = new List<KeyValuePair<string, Uri>>();

            var lastMatchIndex = 0;

            foreach (Match match in matcher.Matches(text)) {
                // Add non-URL (the text up until this match)
                extracted.Add(new KeyValuePair<string, Uri>(text.Substring(0, match.Index), null));

                // Add URL if valid, otherwise just add as non-URL
                var url = text.Substring(match.Index, match.Length);
                Uri uri;
                TryCreate(url, out uri);
                extracted.Add(new KeyValuePair<string, Uri>(url, uri));

                lastMatchIndex = match.Index + match.Length;
            }

            // Add non-URL (the remaining text)
            extracted.Add(new KeyValuePair<string, Uri>(text.Substring(lastMatchIndex), null));

            return extracted;
        }

        /// <summary>
        /// Try to create a <see cref="Uri"/> from the specified <code>url</code>.
        /// </summary>
        /// <remarks>Prepends "http://" for invalid "www" links</remarks>
        /// <param name="url">The URL to create an URI for</param>
        /// <param name="uri">The created URI or <code>null</code> if invalid</param>
        /// <returns><code>true</code> if the URL is valid, <code>false</code> otherwise</returns>
        private static bool TryCreate(string url, out Uri uri) {
            if (url.StartsWith("www")) {
                url = string.Format("http://{0}", url);
            }
            return Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out uri) && validUriSchemes.Contains(uri.Scheme);
        }

    }

}
