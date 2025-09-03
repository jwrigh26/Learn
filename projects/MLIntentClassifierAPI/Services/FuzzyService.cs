using System.Text.RegularExpressions;
using FuzzySharp;
using MLIntentClassifierAPI.Models;

namespace MLIntentClassifierAPI.Services;

public class NameMatch
{
    public int EmployeeId { get; set; }
    public string MatchedVariant { get; set; } = "";
    public string QueryToken { get; set; } = "";
    public int Score { get; set; }
    public string MatchType { get; set; } = ""; // "exact", "fuzzy", "substring"
}

public interface IFuzzyService
{
    /// <summary>
    /// Extract employee matches from query text using the name variant map
    /// </summary>
    /// <param name="text">Query text to search for names</param>
    /// <param name="nameVariantMap">Map of employee ID -> list of name variants</param>
    /// <param name="topN">Number of top matches to return per token (default 3)</param>
    /// <param name="minScore">Minimum fuzzy score threshold (default 85)</param>
    /// <returns>List of name matches with scores and metadata</returns>
    List<NameMatch> ExtractNamesFromQuery(string text, Dictionary<int, List<string>> nameVariantMap, int topN = 3, int minScore = 85);
}

public class FuzzyService : IFuzzyService
{
    public List<NameMatch> ExtractNamesFromQuery(string text, Dictionary<int, List<string>> nameVariantMap, int topN = 3, int minScore = 85)
    {
        if (string.IsNullOrWhiteSpace(text) || nameVariantMap == null || !nameVariantMap.Any())
            return new List<NameMatch>();

        // Tokenize the input text
        var stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase) 
        { 
            "get", "me", "show", "find", "the", "a", "an", "and", "or", "for", "with", "email", "phone", "address"
        };
        
        var tokens = text.Split(new[] { ' ', ',', ';', ':', '\t' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim(new[] { '.', '?', '!', '"', '\'' }))
            .Select(t => t.EndsWith("'s", StringComparison.OrdinalIgnoreCase) ? t[..^2] : t)
            .Where(t => t.Length > 1 && !stopWords.Contains(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var allMatches = new List<NameMatch>();

        // Build a flat list of all variants with their employee IDs for efficient searching
        var allVariants = nameVariantMap
            .SelectMany(kvp => kvp.Value.Select(variant => new { EmployeeId = kvp.Key, Variant = variant }))
            .ToList();

        // Process each token
        foreach (var token in tokens)
        {
            var tokenMatches = new List<NameMatch>();

            // 1. Exact match (highest priority)
            var exactMatches = allVariants
                .Where(v => string.Equals(v.Variant, token, StringComparison.OrdinalIgnoreCase))
                .Select(v => new NameMatch
                {
                    EmployeeId = v.EmployeeId,
                    MatchedVariant = v.Variant,
                    QueryToken = token,
                    Score = 100,
                    MatchType = "exact"
                })
                .ToList();

            tokenMatches.AddRange(exactMatches);

            // 2. Substring match (medium priority)
            if (!exactMatches.Any())
            {
                var substringMatches = allVariants
                    .Where(v => 
                        // Only do substring matching if the token is at least 3 characters
                        // and either:
                        // - variant contains token as a word boundary (e.g., "rick" in "rick sanchez")
                        // - token contains variant as a word boundary (e.g., "ricksanchez" contains "rick")
                        token.Length >= 3 && (
                            (v.Variant.Contains(token, StringComparison.OrdinalIgnoreCase) && 
                             (v.Variant.StartsWith(token, StringComparison.OrdinalIgnoreCase) || 
                              v.Variant.Contains($" {token}", StringComparison.OrdinalIgnoreCase) ||
                              v.Variant.EndsWith(token, StringComparison.OrdinalIgnoreCase))) ||
                            (token.Contains(v.Variant, StringComparison.OrdinalIgnoreCase) && 
                             v.Variant.Length >= 3 &&
                             (token.StartsWith(v.Variant, StringComparison.OrdinalIgnoreCase) ||
                              token.Contains($" {v.Variant}", StringComparison.OrdinalIgnoreCase) ||
                              token.EndsWith(v.Variant, StringComparison.OrdinalIgnoreCase)))
                        ))
                    .Select(v => new NameMatch
                    {
                        EmployeeId = v.EmployeeId,
                        MatchedVariant = v.Variant,
                        QueryToken = token,
                        Score = 95,
                        MatchType = "substring"
                    })
                    .ToList();

                tokenMatches.AddRange(substringMatches);
            }

            // 3. Fuzzy match (fallback)
            if (!tokenMatches.Any())
            {
                var variantList = allVariants.Select(v => v.Variant).ToList();
                var fuzzyResults = Process.ExtractTop(token, variantList, limit: topN)
                    .Where(result => result.Score >= minScore)
                    .ToList();

                foreach (var fuzzyResult in fuzzyResults)
                {
                    var employeeId = allVariants.First(v => v.Variant == fuzzyResult.Value).EmployeeId;
                    tokenMatches.Add(new NameMatch
                    {
                        EmployeeId = employeeId,
                        MatchedVariant = fuzzyResult.Value,
                        QueryToken = token,
                        Score = fuzzyResult.Score,
                        MatchType = "fuzzy"
                    });
                }
            }

            allMatches.AddRange(tokenMatches);
        }

        // Two-word combinations (for full names like "morty smith")
        for (int i = 0; i < tokens.Count - 1; i++)
        {
            var twoWordToken = $"{tokens[i]} {tokens[i + 1]}";
            
            // Exact match for two-word combinations
            var exactTwoWord = allVariants
                .Where(v => string.Equals(v.Variant, twoWordToken, StringComparison.OrdinalIgnoreCase))
                .Select(v => new NameMatch
                {
                    EmployeeId = v.EmployeeId,
                    MatchedVariant = v.Variant,
                    QueryToken = twoWordToken,
                    Score = 100,
                    MatchType = "exact"
                })
                .ToList();

            allMatches.AddRange(exactTwoWord);

            // Fuzzy match for two-word combinations (lower threshold since it's more specific)
            if (!exactTwoWord.Any())
            {
                var variantList = allVariants.Select(v => v.Variant).ToList();
                var fuzzyTwoWord = Process.ExtractTop(twoWordToken, variantList, limit: topN)
                    .Where(result => result.Score >= Math.Max(minScore - 10, 75)) // Lower threshold for two-word
                    .Select(result => new NameMatch
                    {
                        EmployeeId = allVariants.First(v => v.Variant == result.Value).EmployeeId,
                        MatchedVariant = result.Value,
                        QueryToken = twoWordToken,
                        Score = result.Score,
                        MatchType = "fuzzy"
                    })
                    .ToList();

                allMatches.AddRange(fuzzyTwoWord);
            }
        }

        // Remove duplicates and sort by score (best matches first)
        // Group by employee ID only - one result per employee, keeping the best match
        return allMatches
            .GroupBy(m => m.EmployeeId)
            .Select(g => g.OrderByDescending(m => m.Score)
                          .ThenBy(m => m.MatchType == "exact" ? 0 : m.MatchType == "substring" ? 1 : 2)
                          .First())
            .OrderByDescending(m => m.Score)
            .ThenBy(m => m.MatchType == "exact" ? 0 : m.MatchType == "substring" ? 1 : 2)
            .ToList();
    }
}