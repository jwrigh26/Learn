using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ModelBuilder.Preprocessing
{
    /// <summary>
    /// Splits training sentences on coordinating conjunctions and discourse markers
    /// to create separate training examples for multi-intent queries.
    /// </summary>
    public static class SentenceSplitter
    {
        // Conjunctions and discourse markers that typically introduce new intents
        private static readonly HashSet<string> SplitWords = new HashSet<string>(new[]
        {
            "but", "however", "except", "and", "also", "additionally", "plus", "furthermore",
            "although", "though", "while", "whereas", "yet", "still", "nevertheless",
            "otherwise", "instead", "rather", "or", "either", "neither"
        }, StringComparer.OrdinalIgnoreCase);

        // Regex to find split points (word boundaries around split words)
        private static readonly Regex SplitPattern = new Regex(
            @"\b(" + string.Join("|", SplitWords.Select(Regex.Escape)) + @")\b",
            RegexOptions.Compiled | RegexOptions.IgnoreCase
        );

        /// <summary>
        /// Split a query record into multiple fragments based on coordinating conjunctions.
        /// Returns the original record plus any fragments that were split off.
        /// </summary>
        public static List<QueryRecord> SplitQuery(QueryRecord original)
        {
            if (string.IsNullOrWhiteSpace(original.Text))
                return new List<QueryRecord> { original };

            var matches = SplitPattern.Matches(original.Text);
            if (matches.Count == 0)
                return new List<QueryRecord> { original };

            var results = new List<QueryRecord>();
            var text = original.Text;
            var lastEnd = 0;

            foreach (Match match in matches)
            {
                // Get the fragment before the split word
                if (match.Index > lastEnd)
                {
                    var beforeFragment = text.Substring(lastEnd, match.Index - lastEnd).Trim();
                    if (!string.IsNullOrWhiteSpace(beforeFragment))
                    {
                        results.Add(new QueryRecord
                        {
                            Text = beforeFragment,
                            Label = original.Label, // Keep original label for now
                            OriginalText = original.Text
                        });
                    }
                }

                // Start the next fragment after the split word
                lastEnd = match.Index + match.Length;
            }

            // Get the final fragment after the last split word
            if (lastEnd < text.Length)
            {
                var finalFragment = text.Substring(lastEnd).Trim();
                if (!string.IsNullOrWhiteSpace(finalFragment))
                {
                    results.Add(new QueryRecord
                    {
                        Text = finalFragment,
                        Label = "UNKNOWN", // Default for fragments that may have different intent
                        OriginalText = original.Text
                    });
                }
            }

            // If no valid fragments were created, return the original
            return results.Count > 0 ? results : new List<QueryRecord> { original };
        }

        /// <summary>
        /// Process a list of query records, splitting multi-intent queries into fragments.
        /// </summary>
        public static List<QueryRecord> SplitQueries(List<QueryRecord> queries)
        {
            var results = new List<QueryRecord>();
            
            foreach (var query in queries)
            {
                var fragments = SplitQuery(query);
                results.AddRange(fragments);
            }

            return results;
        }

        /// <summary>
        /// Check if a text contains split words that might indicate multiple intents.
        /// </summary>
        public static bool ContainsSplitWords(string text)
        {
            return !string.IsNullOrWhiteSpace(text) && SplitPattern.IsMatch(text);
        }
    }
}
