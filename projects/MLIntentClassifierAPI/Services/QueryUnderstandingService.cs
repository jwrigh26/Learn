using System.Text.RegularExpressions;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.Recognizers.Text;
using Microsoft.Recognizers.Text.DateTime;
using FuzzySharp;
using MLIntentClassifierAPI.Models;
using MSConfiguration = Microsoft.Extensions.Configuration.IConfiguration;

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
        
        // Load ML.NET model - using the path from your notebooks
        var modelPath = "/Users/maneki-neko/learning/notebooks/NLP/intent_model.zip";
        
        if (!string.IsNullOrWhiteSpace(modelPath) && File.Exists(modelPath))
        {
            var ml = new MLContext(seed: 123);
            using var fs = File.OpenRead(modelPath);
            var trained = ml.Model.Load(fs, out var _);
            _engine = ml.Model.CreatePredictionEngine<QueryRecord, IntentPrediction>(trained);
        }
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
            Employees = filteredEmployees,
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
        if (ContainsAny(text, "email","e-mail","mail")) return Intent.GET_CONTACT_EMAIL;
        if (ContainsAny(text, "phone","cell","mobile","call")) return Intent.GET_CONTACT_PHONE;
        if (ContainsAny(text, "address","location","where is")) return Intent.GET_CONTACT_ADDRESS;
        if (ContainsAny(text, "hire date","hired","start date","joined","before","after","between"))
            return Intent.FILTER_BY_HIRE_DATE;
        if (ContainsAny(text, "department","engineering","hr","sales","support"))
            return Intent.FILTER_BY_DEPARTMENT;
        if (ContainsAny(text, "office","remote","salt lake","hq","slc"))
            return Intent.FILTER_BY_LOCATION;
        if (ContainsAny(text, "birthday","birth date","turns","age"))
            return Intent.FILTER_BY_BIRTHDAY;

        return Intent.UNKNOWN;
    }
    
    private QuerySlots ExtractSlots(string text, DomainDictionaries domain, List<string> foundNames)
    {
        text = Normalize(text);
        var slots = new QuerySlots();

        // Field + operator from phrasing
        if (ContainsAny(text, "hire date","hired","start date","joined"))
        {
            slots.Field = "OriginalHireDate";
            if (ContainsAny(text, "before","earlier than","prior to")) slots.Operator = "before";
            else if (ContainsAny(text, "after","later than","since")) slots.Operator = "after";
            else if (ContainsAny(text, "between","from","to","range")) slots.Operator = "between";
        }

        // Department / Role / Location via domain dictionaries
        slots.Department = MapCanonical(text, domain.Departments);
        slots.Role = MapCanonical(text, domain.Roles);
        slots.Location = MapCanonical(text, domain.Locations);

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
                        if (slots.Operator == "before") { slots.DateEnd = d; }
                        else if (slots.Operator == "after") { slots.DateStart = d; }
                        else if (slots.DateStart is null) { slots.DateStart = d; }
                        else if (slots.DateEnd is null) { slots.DateEnd = d; }
                    }
                }
                else if (type == "daterange")
                {
                    if (v.TryGetValue("start", out var s) && DateTime.TryParse(s, out var ds))
                        slots.DateStart = ds;
                    if (v.TryGetValue("end", out var e) && DateTime.TryParse(e, out var de))
                        slots.DateEnd = de;
                    slots.Operator ??= "between";
                }
            }
        }

        // Use found names
        slots.Names = foundNames;

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
        
        // Canonical names as consts
        const string DeptEngineering = "Engineering";
        const string DeptHR = "Human Resources";
        const string DeptSales = "Sales";
        const string DeptSupport = "Support";

        const string RoleManager = "Manager";
        const string RoleSupervisor = "Supervisor";
        const string RoleTeamLead = "Team Lead";
        const string RoleSoftwareEngineer = "Software Engineer";
        const string RoleDirector = "Director";

        const string LocSaltLakeCity = "Salt Lake City";
        const string LocRemote = "Remote";
        const string LocHQ = "Headquarters";

        // Departments (synonym -> canonical)
        foreach (var pair in new (string from, string to)[] {
            ("engineering", DeptEngineering),
            ("eng", DeptEngineering),
            ("dev", DeptEngineering),
            ("development", DeptEngineering),
            ("hr", DeptHR),
            ("human resources", DeptHR),
            ("sales", DeptSales),
            ("revenue", DeptSales),
            ("support", DeptSupport),
        }) domain.Departments[pair.from] = pair.to;

        // Roles (synonym -> canonical)
        foreach (var pair in new (string from, string to)[] {
            ("manager", RoleManager),
            ("managers", RoleManager),
            ("supervisor", RoleSupervisor),
            ("supervisors", RoleSupervisor),
            ("team lead", RoleTeamLead),
            ("lead", RoleTeamLead),
            ("leads", RoleTeamLead),
            ("developer", RoleSoftwareEngineer),
            ("engineer", RoleSoftwareEngineer),
            ("software engineer", RoleSoftwareEngineer),
            ("director", RoleDirector),
            ("directors", RoleDirector),
        }) domain.Roles[pair.from] = pair.to;

        // Locations (synonym -> canonical)
        foreach (var pair in new (string from, string to)[] {
            ("slc", LocSaltLakeCity),
            ("salt lake", LocSaltLakeCity),
            ("salt lake city", LocSaltLakeCity),
            ("remote", LocRemote),
            ("hq", LocHQ),
        }) domain.Locations[pair.from] = pair.to;

        // Role buckets (category -> list of canonical roles)
        domain.RoleBuckets["managers"] = new List<string> { RoleManager, RoleSupervisor, RoleTeamLead, RoleDirector };

        // Known names (optional, for fuzzy)
        domain.EmployeeNames.AddRange(new [] {
            "Rick Sanchez","Summer Smith","Morty Smith","Beth Smith","Jerry Smith",
            "Alice Johnson","Bob Lee","Carol Danvers","Tony Stark","Bruce Wayne","Bird Person"
        });
        
        return domain;
    }
}
