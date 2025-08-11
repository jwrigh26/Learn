namespace MLIntentClassifierAPI.Models;

public sealed class QuerySlots
{
    public string? Field { get; set; }                // e.g., "OriginalHireDate"
    public string? Operator { get; set; }             // e.g., "before", "after", "between"
    public DateTime? DateStart { get; set; }
    public DateTime? DateEnd { get; set; }
    public string? Department { get; set; }           // canonical department
    public string? Role { get; set; }                 // canonical role
    public string? Location { get; set; }             // canonical location
    public List<string> Names { get; set; } = new();  // resolved employee display names
}

public sealed class QueryUnderstanding
{
    public Intent Intent { get; set; }
    public QuerySlots Slots { get; set; } = new();
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
