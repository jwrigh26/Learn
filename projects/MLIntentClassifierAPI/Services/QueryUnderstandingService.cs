using System.Text.RegularExpressions;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.Recognizers.Text;
using Microsoft.Recognizers.Text.DateTime;
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
    private readonly IModel _dateModel;
    
    public QueryUnderstandingService(MSConfiguration configuration)
    {
        _domain = InitializeDomainDictionaries();
        _dateModel = new DateTimeRecognizer(Culture.English, options: DateTimeOptions.None, lazyInitialization: true)
            .GetDateTimeModel();
        
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
        var intent = PredictIntent(text);
        // Get all employees as objects
        var allEmployeeObjects = typeof(EmployeeQueryOrchestrator)
            .GetField("Employees", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
            ?.GetValue(null) as List<Employee> ?? new List<Employee>();
        var allEmployeeNames = allEmployeeObjects.Select(e => e.Name).ToList();
        // Build fuzzy list from all employee names
        var foundNames = ExtractNamesFromQuery(text, allEmployeeNames);
        Console.WriteLine($"Matched names: {string.Join(", ", foundNames)} for query: {text}");
        // Build slots after filtering names
        var slots = ExtractSlots(text, _domain, foundNames);

        // filteredEmployees is the list of Employee objects whose names were matched
        var filteredEmployees = allEmployeeObjects
            .Where(e => foundNames.Contains(e.Name, StringComparer.OrdinalIgnoreCase))
            .ToList();

        return new QueryUnderstanding {
            Intent = intent,
            Slots = slots,
            // Employees = filteredEmployees,
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
    
    private QuerySlots ExtractSlots(string text, DomainDictionaries domain, List<string> foundNames)
    {
        text = Normalize(text);
        var slots = new QuerySlots();

        // Field + operator from phrasing
        if (ContainsAny(text, "hire date","hired","start date","joined"))
        {
            // QuerySlots now has a list of Fields
            slots.Fields.Add("OriginalHireDate");
            if (ContainsAny(text, "before","earlier than","prior to")) slots.Operator = "before";
            else if (ContainsAny(text, "after","later than","since")) slots.Operator = "after";
            else if (ContainsAny(text, "between","from","to","range")) slots.Operator = "between";
        }

        // NOTE: QuerySlots no longer contains Department/Role/Location properties
        // Map domain matches into Fields (as canonical tokens) if needed by downstream logic
        var dept = MapCanonical(text, domain.Departments);
        if (!string.IsNullOrEmpty(dept)) slots.Fields.Add($"department:{dept}");
        var role = MapCanonical(text, domain.Roles);
        if (!string.IsNullOrEmpty(role)) slots.Fields.Add($"role:{role}");
        var loc = MapCanonical(text, domain.Locations);
        if (!string.IsNullOrEmpty(loc)) slots.Fields.Add($"location:{loc}");

        // Date parsing (supports "before 2024", "last year", ranges, etc.)
        var dateResults = _dateModel.Parse(text);
        // pick first resolution; improve as needed
        foreach (var r in dateResults)
        {
            if (!r.Resolution.TryGetValue("values", out var valuesObj)) continue;
            if (valuesObj is not List<Dictionary<string, string>> values) continue;

            foreach (var v in values)
            {
                if (!v.TryGetValue("type", out var type)) continue;
                if (type == "date" && v.TryGetValue("value", out var val))
                {
                    if (DateTime.TryParse(val, out var d))
                    {
                        // For single dates: store as Date. If operator indicates a bound, convert to Range.
                        if (slots.Operator == "before")
                        {
                            slots.Range ??= new DateRange();
                            slots.Range.End = d;
                        }
                        else if (slots.Operator == "after")
                        {
                            slots.Range ??= new DateRange();
                            slots.Range.Start = d;
                        }
                        else
                        {
                            // default: prefer Date for single specific dates
                            slots.Date ??= d;
                        }
                    }
                }
                else if (type == "daterange")
                {
                    if (v.TryGetValue("start", out var s) && DateTime.TryParse(s, out var ds))
                        slots.Range ??= new DateRange { Start = ds };
                    if (v.TryGetValue("end", out var e) && DateTime.TryParse(e, out var de))
                        slots.Range ??= new DateRange { End = de };
                    // If we created a partial Range above, ensure both bounds set when possible
                    if (slots.Range != null)
                    {
                        if (slots.Range.Start == default && v.TryGetValue("start", out var s2) && DateTime.TryParse(s2, out var ds2))
                            slots.Range.Start = ds2;
                        if (slots.Range.End == default && v.TryGetValue("end", out var e2) && DateTime.TryParse(e2, out var de2))
                            slots.Range.End = de2;
                    }
                    slots.Operator ??= "between";
                }
            }
        }


        return slots;
    }
    
    private static string Normalize(string s) =>
        Regex.Replace(s, @"\s+", " ").Trim();

    private static bool ContainsAny(string text, params string[] terms) =>
        terms.Any(t => text.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0);

    // map synonyms using dictionary; returns canonical if matched
    private static string? MapCanonical(string text, Dictionary<string,string> dict)
    {
        // exact/single-token match first
        if (dict.TryGetValue(text, out var canonical)) return canonical;

        // contains-phrase match
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
