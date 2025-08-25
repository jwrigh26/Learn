using MLIntentClassifierAPI.Services;
namespace MLIntentClassifierAPI.Models;

public sealed class QuerySlots
{
    public List<string> Fields { get; set; } = new();                // e.g., "OriginalHireDate"
    public string? Operator { get; set; }             // e.g., "before", "after", "between"
    public DateTime? Date{ get; set; }
    public DateRange? Range { get; set; }           // canonical department
}

public sealed class DateRange
{
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
}

public sealed class NameMatch
{
    public string Name { get; set; } = "";
    public int Score { get; set;  }
}

public sealed class QueryUnderstanding
{
    public Intent Intent { get; set; }
    public QuerySlots Slots { get; set; } = new();
    public List<Employee> Employees { get; set; } = new();
    // Map of employee id -> generated name variants for fuzzy matching (used for testing)
    public Dictionary<int, List<string>> NameVariantMap { get; set; } = new();
}

public sealed class QueryRecord 
{ 
    public string Text { get; set; } = ""; 
    public string Label { get; set; } = ""; 
}

public sealed class IntentPrediction 
{ 
    public string PredictedLabel { get; set; } = ""; 
    public float[] Score { get; set; } = Array.Empty<float>();
}
