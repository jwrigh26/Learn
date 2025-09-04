using System.Text.RegularExpressions;
using Microsoft.ML;
using Microsoft.ML.Data;
using FuzzySharp;
using MLIntentClassifierAPI.Models;
using MSConfiguration = Microsoft.Extensions.Configuration.IConfiguration;
using System.IO;
using System.Linq;

namespace MLIntentClassifierAPI.Services;

public class QueryUnderstandingService
{
    private readonly PredictionEngine<QueryRecord, IntentPrediction>? _engine;
    private readonly DomainDictionaries _domain;
    private readonly ISlotService _slotService;
    
    public QueryUnderstandingService(MSConfiguration configuration, ISlotService slotService)
    {
        _domain = InitializeDomainDictionaries();
        _slotService = slotService;
        
        // Load ML.NET model from ModelTrainer output (configured path)
        string modelPath;

        var trainerOutput = configuration["ModelTrainerOutputPath"];
        if (string.IsNullOrWhiteSpace(trainerOutput))
            throw new InvalidOperationException("Configuration 'ModelTrainerOutputPath' is required and must point to the ModelTrainer Output directory.");

        if (!Directory.Exists(trainerOutput))
            throw new InvalidOperationException($"Configured ModelTrainerOutputPath does not exist: {trainerOutput}");

        var candidates = Directory.GetFiles(trainerOutput, "intent_model_v*.zip", SearchOption.TopDirectoryOnly);
        if (candidates.Length == 0)
            throw new FileNotFoundException($"No model file matching 'intent_model_v*.zip' found in {trainerOutput}");

        // If multiple, pick the most recently written one
        modelPath = candidates.Length == 1 ? candidates[0] : candidates.OrderByDescending(File.GetLastWriteTimeUtc).First();

        // Load the model (will throw if file is invalid)
        var ml = new MLContext(seed: 42);
        using var fs = File.OpenRead(modelPath);
        var trained = ml.Model.Load(fs, out var _);
        _engine = ml.Model.CreatePredictionEngine<QueryRecord, IntentPrediction>(trained);
    }
    
    public QueryUnderstanding Understand(string text)
    {
        // Pure NLP processing - intent prediction and slot extraction
        var intent = PredictIntent(text);
    var slots = _slotService.ExtractSlots(text);

        return new QueryUnderstanding {
            Intent = intent,
            Slots = slots
            // No employees here - that's the controller's responsibility
        };
    }

    private static List<string> ExtractNamesFromQuery(string text, List<string> employeeNames)
    {
        var tokens = text.Split(new[]{' ', ',', ';'}, StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim(new[]{'.','?','!'}))
            .Select(t => t.EndsWith("'s", StringComparison.OrdinalIgnoreCase) ? t[..^2] : t)
            .Where(t => t.Length > 1).ToList();
        var uniqueTokens = tokens.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var matchedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        // Try all two-word combinations for full name matching
        for (int i = 0; i < uniqueTokens.Count - 1; i++)
        {
            var candidate = uniqueTokens[i] + " " + uniqueTokens[i + 1];
            var best = Process.ExtractOne(candidate, employeeNames);
            if (best != null && best.Score >= 88)
                matchedNames.Add(best.Value);
        }
        // Fallback: single token match only if it is a unique substring among employees (no fuzzy)
        foreach (var token in uniqueTokens)
        {
            var matches = employeeNames.Where(n => n.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            if (matches.Count == 1)
                matchedNames.Add(matches[0]);
        }
        return matchedNames.ToList();
    }

    private Intent PredictIntent(string text)
    {
        if (_engine is not null)
        {
            var pred = _engine.Predict(new QueryRecord { Text = text });
            if (Enum.TryParse<Intent>(pred.PredictedLabel, out var parsed))
                return parsed;
        }

        // Fallback rules (fast & predictable)
        if (ContainsAny(text, "email","e-mail","mail")) return Intent.CONTACT_EMAIL;
        if (ContainsAny(text, "phone","cell","mobile","call")) return Intent.CONTACT_PHONE;
        if (ContainsAny(text, "address","location","where is")) return Intent.CONTACT_ADDRESS;
        if (ContainsAny(text, "hire date", "hired", "start date", "joined", "before", "after", "between"))
            return Intent.CONTACT_HIRE_DATE;
        if (ContainsAny(text, "department", "engineering", "hr", "sales", "support"))
            return Intent.CONTACT_INFO;
        if (ContainsAny(text, "office", "remote", "salt lake", "hq", "slc"))
            return Intent.CONTACT_INFO;
        if (ContainsAny(text, "birthday","birth date","turns","age"))
            return Intent.CONTACT_BIRTHDAY;

        return Intent.UNKNOWN;
    }
    
    private static string Normalize(string s) =>
        Regex.Replace(s, @"\s+", " ").Trim();

    private static bool ContainsAny(string text, params string[] terms) =>
        terms.Any(t => text.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0);

    // map synonyms using dictionary; returns canonical if matched
    private static string? MapCanonical(string text, Dictionary<string,string> dict)
    {
        if (dict.TryGetValue(text, out var canonical)) return canonical;
        foreach (var kvp in dict)
        {
            if (text.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
                return kvp.Value;
        }
        return null;
    }
    
    private static DomainDictionaries InitializeDomainDictionaries()
    {
        var domain = new DomainDictionaries();

        // Canonical field type names
        const string Department = "department";
        const string Position = "position";
        const string Location = "location";
        const string Job = "job";

        // Department field synonyms (ways to refer to the concept of "department")
        foreach (var pair in new (string from, string to)[] {
            ("dept", Department),
            ("department", Department),
            ("departments", Department),
            ("division", Department),
            ("branch", Department),
            ("office", Department),
            ("section", Department),
            ("unit", Department),
            ("team", Department),
            ("group", Department),
        }) domain.Departments[pair.from] = pair.to;

        // Position/Role field synonyms (ways to refer to the concept of "position")
        foreach (var pair in new (string from, string to)[] {
            ("position", Position),
            ("positions", Position),
            ("role", Position),
            ("roles", Position),
            ("title", Position),
            ("titles", Position),
            ("rank", Position),
            ("level", Position),
        }) domain.Roles[pair.from] = pair.to;

        // Location field synonyms (ways to refer to the concept of "location")
        foreach (var pair in new (string from, string to)[] {
            ("location", Location),
            ("locations", Location),
            ("place", Location),
            ("site", Location),
            ("office", Location),
            ("offices", Location),
            ("workplace", Location),
            ("facility", Location),
        }) domain.Locations[pair.from] = pair.to;

        // Job field synonyms (ways to refer to the concept of "job" - if different from position)
        foreach (var pair in new (string from, string to)[] {
            ("job", Job),
            ("jobs", Job),
            ("work", Job),
            ("task", Job),
            ("assignment", Job),
        }) domain.RoleBuckets[pair.from] = new List<string> { pair.to };

        return domain;
    }
}
