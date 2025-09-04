using Microsoft.Recognizers.Text;
using Microsoft.Recognizers.Text.DateTime;
using MLIntentClassifierAPI.Models;

namespace MLIntentClassifierAPI.Services;

public interface ISlotService
{
    // Extracts fields (department/location/position/job) and dates from a query
    QuerySlots ExtractSlots(string text);

    // Returns the predefined org-level value sets used for fuzzy matching
    Dictionary<string, List<string>> GetOrgValueSets();
}

public class SlotService : ISlotService
{
    private readonly IFuzzyService _fuzzyService;
    private readonly IModel _dateModel;

    // Rick and Morty themed org-level values (POC)
    private static readonly List<string> Departments = new()
    {
        "R&D",
        "Portal Engineering",
        "Interdimensional Logistics",
        "Citadel Operations",
        "Galactic Compliance",
        "Multiverse HR"
    };

    private static readonly List<string> Locations = new()
    {
        "Rick's Garage",
        "Citadel of Ricks",
        "Earth C-137",
        "Birdperson's Nest",
        "Squanch Planet Outpost",
        "Anatomy Park"
    };

    private static readonly List<string> Positions = new()
    {
        "Chief Scientist",
        "Jr. Assistant",
        "Portal Engineer",
        "Council Liaison",
        "DevOps Engineer",
        "Marketing Specialist",
        "HR Director"
    };

    private static readonly List<string> Jobs = new()
    {
        "Lab Technician",
        "Temporal Analyst",
        "Portal Calibration Tech",
        "Universe Compliance Officer",
        "Logistics Dispatcher",
        "Support Specialist",
        "Social Media Manager"
    };

    public SlotService(IFuzzyService fuzzyService)
    {
        _fuzzyService = fuzzyService;
        _dateModel = new DateTimeRecognizer(Culture.English, options: DateTimeOptions.None, lazyInitialization: true)
            .GetDateTimeModel();
    }

    public Dictionary<string, List<string>> GetOrgValueSets() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["department"] = Departments,
        ["location"] = Locations,
        ["position"] = Positions,
        ["job"] = Jobs
    };

    // Build variants for field values (similar to BuildNameVariants in EmployeeRepository)
    private static List<string> BuildFieldVariants(string value)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var normalized = value.Trim().ToLowerInvariant();

        if (string.IsNullOrEmpty(normalized)) return new List<string>();

        // Add the original value
        set.Add(normalized);

        // Handle common abbreviations and variations
        var words = normalized.Split(new[] { ' ', '-', '&' }, StringSplitOptions.RemoveEmptyEntries)
            .SelectMany(w => w.Split(new[] { "and" }, StringSplitOptions.RemoveEmptyEntries))
            .Select(w => w.Trim())
            .Where(w => !string.IsNullOrEmpty(w))
            .ToArray();
        
        if (words.Length > 1)
        {
            // Add individual words
            foreach (var word in words.Where(w => w.Length > 1))
            {
                set.Add(word);
            }

            // Add acronyms (first letters)
            var acronym = string.Join("", words.Select(w => w[0]));
            if (acronym.Length > 1)
            {
                set.Add(acronym);
                // Add R&D style only for specific patterns
                if (acronym.Length == 2)
                {
                    set.Add($"{acronym[0]}&{acronym[1]}");
                }
            }

            // Add without spaces/punctuation
            set.Add(string.Join("", words));
        }

        // Common synonyms and variations
        var synonyms = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["r&d"] = new[] { "research", "development", "research and development", "rd", "r and d" },
            ["portal engineering"] = new[] { "portal", "engineering", "portals", "eng" },
            ["interdimensional logistics"] = new[] { "logistics", "interdimensional", "shipping", "transport" },
            ["citadel operations"] = new[] { "operations", "citadel", "ops", "operations team" },
            ["galactic compliance"] = new[] { "compliance", "galactic", "legal", "regulatory" },
            ["multiverse hr"] = new[] { "hr", "human resources", "people", "personnel" },
            
            ["rick's garage"] = new[] { "garage", "ricks garage", "rick garage", "lab", "workshop" },
            ["citadel of ricks"] = new[] { "citadel", "headquarters", "hq", "main office" },
            ["earth c-137"] = new[] { "earth", "c137", "c-137", "home", "earth c137" },
            ["birdperson's nest"] = new[] { "nest", "birdperson nest", "bird nest", "phoenix" },
            ["squanch planet outpost"] = new[] { "squanch", "outpost", "squanch planet", "remote" },
            ["anatomy park"] = new[] { "anatomy", "park", "medical", "health" },
            
            ["chief scientist"] = new[] { "scientist", "chief", "senior scientist", "lead scientist" },
            ["jr. assistant"] = new[] { "junior", "assistant", "jr assistant", "junior assistant" },
            ["portal engineer"] = new[] { "engineer", "portal", "engineering", "tech" },
            ["council liaison"] = new[] { "liaison", "council", "representative", "contact" },
            ["devops engineer"] = new[] { "devops", "engineer", "infrastructure", "ops" },
            ["marketing specialist"] = new[] { "marketing", "specialist", "marketer", "promo" },
            ["hr director"] = new[] { "director", "hr", "manager", "head" },
            
            ["lab technician"] = new[] { "technician", "lab", "tech", "laboratory" },
            ["temporal analyst"] = new[] { "analyst", "temporal", "time", "analysis" },
            ["portal calibration tech"] = new[] { "calibration", "tech", "portal", "maintenance" },
            ["universe compliance officer"] = new[] { "compliance", "officer", "universe", "regulatory" },
            ["logistics dispatcher"] = new[] { "dispatcher", "logistics", "shipping", "coordinator" },
            ["support specialist"] = new[] { "support", "specialist", "help", "customer support" },
            ["social media manager"] = new[] { "social media", "manager", "social", "media" }
        };

        // Add synonyms if this value has them
        if (synonyms.TryGetValue(normalized, out var syns))
        {
            foreach (var syn in syns)
            {
                set.Add(syn);
            }
        }

        // Remove empty/very short tokens and return
        return set.Where(s => !string.IsNullOrWhiteSpace(s) && s.Length > 1)
                  .Distinct(StringComparer.OrdinalIgnoreCase)
                  .ToList();
    }

    // Get field variant maps (like GetNameVariantMap in EmployeeRepository)
    public Dictionary<string, Dictionary<string, List<string>>> GetFieldVariantMaps()
    {
        var result = new Dictionary<string, Dictionary<string, List<string>>>(StringComparer.OrdinalIgnoreCase);
        var orgSets = GetOrgValueSets();

        foreach (var (fieldType, values) in orgSets)
        {
            var variants = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var value in values)
            {
                variants[value] = BuildFieldVariants(value);
            }
            result[fieldType] = variants;
        }

        return result;
    }

    public QuerySlots ExtractSlots(string text)
    {
        var slots = new QuerySlots();
        if (string.IsNullOrWhiteSpace(text)) return slots;

        var normalized = Normalize(text);

        // Operator signals for date bounds
        if (ContainsAny(normalized, "before", "earlier than", "prior to")) slots.Operator = "before";
        else if (ContainsAny(normalized, "after", "later than", "since")) slots.Operator = "after";
        else if (ContainsAny(normalized, "between", "from", "to", "range")) slots.Operator = "between";

        // If query mentions hire-date concepts, include the hire date field marker
        if (ContainsAny(normalized, "hire date", "hired", "start date", "joined"))
        {
            slots.Fields.Add("OriginalHireDate");
        }

        // Field matching via fuzzy service with variants
        var fieldVariantMaps = GetFieldVariantMaps();
        var fieldMatches = new List<FieldValueMatch>();

        foreach (var (fieldType, valueToVariants) in fieldVariantMaps)
        {
            // Flatten variants for this field type (similar to FuzzyService name matching)
            var allVariantsForField = valueToVariants
                .SelectMany(kvp => kvp.Value.Select(variant => new { CanonicalValue = kvp.Key, Variant = variant }))
                .ToList();

            var matches = _fuzzyService.ExtractFieldValuesFromQuery(normalized, 
                new Dictionary<string, List<string>> { [fieldType] = allVariantsForField.Select(v => v.Variant).ToList() },
                topN: 3, minScore: 85);

            // Map back to canonical values
            foreach (var match in matches)
            {
                var canonical = allVariantsForField
                    .FirstOrDefault(v => string.Equals(v.Variant, match.CanonicalValue, StringComparison.OrdinalIgnoreCase))
                    ?.CanonicalValue ?? match.CanonicalValue;

                fieldMatches.Add(new FieldValueMatch
                {
                    FieldType = fieldType,
                    CanonicalValue = canonical,
                    QueryToken = match.QueryToken,
                    Score = match.Score,
                    MatchType = match.MatchType
                });
            }
        }

        // Keep best value per field type
        foreach (var best in fieldMatches
                     .GroupBy(m => m.FieldType)
                     .Select(g => g.OrderByDescending(x => x.Score)
                                   .ThenBy(x => x.MatchType == "exact" ? 0 : x.MatchType == "substring" ? 1 : 2)
                                   .First()))
        {
            slots.Fields.Add($"{best.FieldType}:{best.CanonicalValue}");
        }

        // Date parsing (supports single dates and ranges)
        var dateResults = _dateModel.Parse(normalized);
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
                            slots.Date ??= d;
                        }
                    }
                }
                else if (type == "daterange")
                {
                    if (v.TryGetValue("start", out var s) && DateTime.TryParse(s, out var ds))
                        slots.Range ??= new DateRange { Start = ds };
                    if (v.TryGetValue("end", out var e) && DateTime.TryParse(e, out var de))
                        slots.Range ??= slots.Range == null ? new DateRange { End = de } : new DateRange { Start = slots.Range.Start, End = de };

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

    private static string Normalize(string s) => System.Text.RegularExpressions.Regex.Replace(s, @"\s+", " ").Trim();
    private static bool ContainsAny(string text, params string[] terms) => terms.Any(t => text.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0);
}
