using System.Text.Json;
using System.Text.Json.Nodes;
using FuzzySharp;
using Microsoft.Recognizers.Text;
using Microsoft.Recognizers.Text.DateTime;
using RestSharp;

// ---------- Domain ----------
public enum Intent { GetContactInfo, FilterByHireDate, FilterByRole, Unknown }

public record Slots(
    string[]? Names = null,
    DateTime? Date = null,
    (DateTime Start, DateTime End)? Range = null,
    string? Operator = null,   // "before" | "after" | "between"
    string? Department = null,
    string? Role = null
);

public record QuerySpec(Intent Intent, Slots Slots);

public record Employee(
    string DisplayName,
    string Email,
    string Department,
    string Role,
    DateTime OriginalHireDate
);

// ---------- Seed Data ----------
var employees = new List<Employee> {
    new("Rick Sanchez",   "rick.sanchez@company.com",   "Engineering", "Staff Engineer",  new DateTime(2015,  5, 10)),
    new("Morty Smith",    "morty.smith@company.com",    "Engineering", "Engineer I",      new DateTime(2023, 10, 12)),
    new("Summer Smith",   "summer.smith@company.com",   "Product",     "PM",              new DateTime(2021,  2,  1)),
    new("Beth Smith",     "beth.smith@company.com",     "HR",          "HR Manager",      new DateTime(2019,  7,  3)),
    new("Jerry Smith",    "jerry.smith@company.com",    "Sales",       "Account Manager", new DateTime(2022,  9, 15))
};

Console.WriteLine($"Demo employees loaded: {employees.Count}");

// ---------- Intent ----------
static Intent ClassifyIntent(string query)
{
    var q = query.ToLowerInvariant();
    if (q.Contains("email") || q.Contains("contact")) return Intent.GetContactInfo;
    if (q.Contains("hire") && (q.Contains("before") || q.Contains("after") || q.Contains("between"))) return Intent.FilterByHireDate;
    if (q.Contains("manager") || q.Contains("engineer") || q.Contains("director") || q.Contains("role")) return Intent.FilterByRole;
    return Intent.Unknown;
}

// ---------- Dates ----------
public record DateExtraction(DateTime? Date, (DateTime Start, DateTime End)? Range, string? Operator);
static DateExtraction ExtractDates(string query)
{
    var results = DateTimeRecognizer.RecognizeDateTime(query, Culture.English);
    var values = new List<DateTime>();

    foreach (var r in results)
    {
        if (!r.Resolution.TryGetValue("values", out var valsObj)) continue;
        if (valsObj is List<Dictionary<string, string>> vals)
        {
            foreach (var v in vals)
            {
                if (v.TryGetValue("value", out var s) && DateTime.TryParse(s, out var dt))
                    values.Add(dt);
                else if (v.TryGetValue("start", out var s1) && v.TryGetValue("end", out var s2)
                         && DateTime.TryParse(s1, out var d1) && DateTime.TryParse(s2, out var d2))
                    return new DateExtraction(null, (d1, d2), "between");
            }
        }
    }

    string? op = null;
    var lower = query.ToLowerInvariant();
    if (lower.Contains("before")) op = "before";
    else if (lower.Contains("after")) op = "after";
    else if (lower.Contains("between")) op = "between";

    if (values.Count >= 2) return new DateExtraction(null, (values.Min(), values.Max()), "between");
    if (values.Count == 1) return new DateExtraction(values[0], null, op);
    return new DateExtraction(null, null, null);
}

// ---------- Lexicon ----------
var lexiconJson = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "lexicon.json"));
var lexicon = JsonNode.Parse(lexiconJson)!.AsObject();

static string? MapAlias(JsonObject section, string input)
{
    var q = input.ToLowerInvariant();
    foreach (var kvp in section)
    {
        var canon = kvp.Key;
        var aliases = kvp.Value!.AsArray().Select(n => n!.ToString().ToLowerInvariant()).ToList();
        if (aliases.Contains(q) || canon.ToLowerInvariant() == q) return canon;
        var best = Process.ExtractOne(q, aliases);
        if (best != null && best.Score >= 90) return canon;
    }
    return null;
}
string? MapDepartment(string text) => MapAlias((JsonObject)lexicon["departments"]!, text);
string? MapRole(string text) => MapAlias((JsonObject)lexicon["roles"]!, text);

// ---------- Names ----------
static List<string> ExtractCandidateNames(string query)
{
    var seps = new [] {","," and "," & "};
    var temp = query.ToLowerInvariant();
    foreach (var s in seps) temp = temp.Replace(s, "|");
    return temp.Split("|", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
}

static List<Employee> MatchNames(string query, IEnumerable<Employee> all, int minScore = 85, int topK = 3)
{
    var tokens = ExtractCandidateNames(query);
    var results = new List<Employee>();
    foreach (var t in tokens)
    {
        var best = Process.ExtractTop(t, all.ToList(), e => e.DisplayName, limit: topK);
        var accepted = best.FirstOrDefault(b => b.Score >= minScore);
        if (accepted != null && !results.Any(r => r.DisplayName == accepted.Value.DisplayName))
            results.Add(accepted.Value);
    }
    return results;
}

// ---------- Slots & Query ----------
static QuerySpec BuildQuerySpec(string query, List<Employee> employees, Func<string?, string?> mapDept, Func<string?, string?> mapRole)
{
    var intent = ClassifyIntent(query);
    var names = MatchNames(query, employees).Select(e => e.DisplayName).ToArray();
    var dates = ExtractDates(query);

    string? dept = null; string? role = null;
    var words = query.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    foreach (var w in words)
    {
        dept ??= mapDept(w);
        role ??= mapRole(w);
    }

    var slots = new Slots(
        Names: names.Length > 0 ? names : null,
        Date: dates.Date,
        Range: dates.Range,
        Operator: dates.Operator,
        Department: dept,
        Role: role
    );

    return new QuerySpec(intent, slots);
}

static IEnumerable<Employee> ExecuteQuery(QuerySpec spec, IEnumerable<Employee> all)
{
    IEnumerable<Employee> q = all;

    if (spec.Slots.Names is { Length: > 0 })
        q = q.Where(e => spec.Slots.Names!.Contains(e.DisplayName));

    if (!string.IsNullOrWhiteSpace(spec.Slots.Department))
        q = q.Where(e => e.Department.Equals(spec.Slots.Department, StringComparison.OrdinalIgnoreCase));

    if (!string.IsNullOrWhiteSpace(spec.Slots.Role))
        q = q.Where(e => e.Role.Contains(spec.Slots.Role!, StringComparison.OrdinalIgnoreCase));

    if (spec.Slots.Operator == "before" && spec.Slots.Date is DateTime d1)
        q = q.Where(e => e.OriginalHireDate < d1);
    else if (spec.Slots.Operator == "after" && spec.Slots.Date is DateTime d2)
        q = q.Where(e => e.OriginalHireDate > d2);
    else if (spec.Slots.Operator == "between" && spec.Slots.Range is (DateTime s, DateTime e2))
        q = q.Where(e => e.OriginalHireDate >= s && e.OriginalHireDate <= e2);

    return q.ToList();
}

// ---------- Optional: Ollama JSON formatting ----------
public record AnswerDto(List<PersonDto> Answer);
public record PersonDto(string Name, string Email);

static string BuildEmailFormattingPrompt(IEnumerable<Employee> people, string question)
{
    var context = people.Select(p => new { name = p.DisplayName, email = p.Email });
    var ctxJson = JsonSerializer.Serialize(context);
    return $@"
You are an API that returns ONLY compact JSON matching this schema:
{{ ""answer"": [ {{ ""name"": ""string"", ""email"": ""string"" }} ] }}
Do not include markdown fences, commentary, or extra keys.

Question: {question}
Context (JSON array of people): {ctxJson}
Return ONLY JSON.
";
}

static void MaybeFormatWithOllama(IEnumerable<Employee> selected, string question, bool enableCall = false)
{
    if (!enableCall)
    {
        Console.WriteLine("Ollama call disabled. Rendering locally:");
        foreach (var p in selected) Console.WriteLine($"{p.DisplayName}: {p.Email}");
        return;
    }

    var client = new RestClient("http://localhost:11434");
    var req = new RestRequest("/api/generate").AddJsonBody(new {
        model = "phi3:mini",
        prompt = BuildEmailFormattingPrompt(selected, question),
        stream = false
    });
    var resp = client.Post(req);
    var raw = resp.Content ?? "{}";
    using var doc = JsonDocument.Parse(raw);
    var text = doc.RootElement.GetProperty("response").GetString();

    try
    {
        var parsed = JsonSerializer.Deserialize<AnswerDto>(text!, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Console.WriteLine("LLM JSON parsed:");
        foreach (var p in parsed!.Answer) Console.WriteLine($"{p.Name}: {p.Email}");
    }
    catch (Exception ex)
    {
        Console.WriteLine("Failed to parse LLM JSON. Raw text follows:");
        Console.WriteLine(text);
        Console.WriteLine(ex.Message);
    }
}

// ---------- Demo ----------
var query = args.Length > 0 ? string.Join(" ", args) : "show emails for rick, summer and morty hired before 2024 in engineering";

var spec = BuildQuerySpec(query, employees,
    s => s is null ? null : MapDepartment(s)!,
    s => s is null ? null : MapRole(s)!);

Console.WriteLine($"Intent: {spec.Intent}");
Console.WriteLine($"Slots: names={string.Join(", ", spec.Slots.Names ?? Array.Empty<string>())} dept={spec.Slots.Department} role={spec.Slots.Role} op={spec.Slots.Operator}");

var filtered = ExecuteQuery(spec, employees).ToList();
Console.WriteLine("\\nFiltered results:");
foreach (var e in filtered) Console.WriteLine($"{e.DisplayName} — {e.Email} — {e.Department} — {e.Role} — {e.OriginalHireDate:yyyy-MM-dd}");

if (spec.Intent == Intent.GetContactInfo && filtered.Any())
{
    MaybeFormatWithOllama(filtered, query, enableCall: false); // flip to true when Ollama is running
}
