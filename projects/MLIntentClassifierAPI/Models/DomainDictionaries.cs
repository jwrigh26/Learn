namespace MLIntentClassifierAPI.Models;

public sealed class DomainDictionaries
{
    // Map any synonym -> canonical
    public Dictionary<string,string> Departments { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string,string> Roles { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string,string> Locations { get; } = new(StringComparer.OrdinalIgnoreCase);

    // Optional: label -> list of canonical roles (hierarchy buckets like "managers")
    public Dictionary<string,List<string>> RoleBuckets { get; } = new(StringComparer.OrdinalIgnoreCase);

    // Optional: list of known employee names for fuzzy matching
    public List<string> EmployeeNames { get; } = new();
}
