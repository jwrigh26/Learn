using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ModelBuilder.Preprocessing
{
    /// <summary>
    /// Lightweight text normalization + stopword removal tuned for intent classification + slot filling.
    /// Keep-words (like temporal operators) are preserved even if they appear in stopwords.
    /// </summary>
    public static class StopwordPreprocessor
    {
        // Basic tokenizer: words and words with apostrophes (jim's)
        private static readonly Regex Tokenizer = new Regex(@"[A-Za-z0-9]+'[A-Za-z0-9]+|[A-Za-z0-9]+",
            RegexOptions.Compiled);

        // Possessive 's remover (jim's -> jim)
        private static readonly Regex Possessive = new Regex(@"^(.+?)'?s$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Temporal operator / comparison patterns that must be preserved
        private static readonly HashSet<string> TemporalOps = new HashSet<string>(new[]
        {
            "before","after","since","until","between","during","next","last","previous","prior","upcoming","past",
            "earlier","later","from","to","through","thru","by"
        }, StringComparer.OrdinalIgnoreCase);

        // Time words that map to slots (do not remove)
        private static readonly HashSet<string> TimeWords = new HashSet<string>(new[]
        {
            "today","tomorrow","yesterday","week","month","year","quarter","q1","q2","q3","q4",
            "monday","tuesday","wednesday","thursday","friday","saturday","sunday",
            "jan","january","feb","february","mar","march","apr","april","may","jun","june","jul","july",
            "aug","august","sep","sept","september","oct","october","nov","november","dec","december"
        }, StringComparer.OrdinalIgnoreCase);

        // Domain words to keep (extend in your app with departments/roles, etc.)
        private static readonly HashSet<string> DomainKeep = new HashSet<string>(new[]
        {
            "email","phone","address","anniversary","birthday","hire","hired","hiredate","start","startdate",
            "department","role","manager","director","engineer","engineering","finance","hr","sales",
            "location","office","remote"
        }, StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Normalize a text input:
        /// - lowercase
        /// - tokenize
        /// - strip possessive 's
        /// - simple plural lemmatization
        /// - remove stopwords except "keep" words (temporal ops, time words, domain terms)
        /// - optionally collapse numbers to &lt;NUM&gt; for generalization
        /// </summary>
        public static IReadOnlyList<string> NormalizeTokens(
            string text,
            ISet<string> stopwords,
            ISet<string>? extraKeep = null,
            bool collapseNumbers = false)
        {
            if (string.IsNullOrWhiteSpace(text))
                return Array.Empty<string>();

            var keep = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var s in TemporalOps) keep.Add(s);
            foreach (var s in TimeWords) keep.Add(s);
            foreach (var s in DomainKeep) keep.Add(s);
            if (extraKeep != null) foreach (var s in extraKeep) keep.Add(s);

            var tokens = new List<string>();

            foreach (Match m in Tokenizer.Matches(text.ToLowerInvariant()))
            {
                var t = m.Value;

                // Collapse pure numbers (optionally)
                if (collapseNumbers && t.All(char.IsDigit))
                {
                    tokens.Add("<NUM>");
                    continue;
                }

                // Strip possessive: jim's -> jim (but keep "it's" -> "it" as intended)
                var poss = Possessive.Match(t);
                if (poss.Success)
                {
                    t = poss.Groups[1].Value;
                }

                // Simple lemmatization: plural -> singular (engineers -> engineer)
                t = SimpleLemma(t);

                // Decide if we keep or drop
                bool isKeepWord = keep.Contains(t);
                bool isStop = stopwords.Contains(t);

                if (!isStop || isKeepWord)
                {
                    tokens.Add(t);
                }
            }

            return tokens;
        }

        /// <summary>
        /// Convenience helper: returns a single normalized string with spaces.
        /// </summary>
        public static string Normalize(
            string text,
            ISet<string> stopwords,
            ISet<string>? extraKeep = null,
            bool collapseNumbers = false)
        {
            var toks = NormalizeTokens(text, stopwords, extraKeep, collapseNumbers);
            return string.Join(' ', toks);
        }

        /// <summary>
        /// Load stopwords (one per line, comments start with '#').
        /// </summary>
        public static HashSet<string> LoadStopwords(string filePath)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var line in System.IO.File.ReadLines(filePath))
            {
                var s = line.Trim();
                if (s.Length == 0) continue;
                if (s.StartsWith("#")) continue;
                set.Add(s);
            }
            return set;
        }

        private static string SimpleLemma(string token)
        {
            // Do not singularize very short tokens
            if (token.Length <= 3) return token;

            // Leave proper acronyms like "hr" / "ss" alone; naive rules:
            if (token.EndsWith("ies"))
                return token.Substring(0, token.Length - 3) + "y";
            if (token.EndsWith("ses") || token.EndsWith("xes") || token.EndsWith("zes") || token.EndsWith("ches") || token.EndsWith("shes"))
                return token.Substring(0, token.Length - 2); // e.g., "boxes" -> "box"
            if (token.EndsWith("s") && !token.EndsWith("ss"))
                return token.Substring(0, token.Length - 1);

            return token;
        }
    }
}
