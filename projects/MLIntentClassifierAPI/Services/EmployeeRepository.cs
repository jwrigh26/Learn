using MLIntentClassifierAPI.Models;
using System.Text.Json;

namespace MLIntentClassifierAPI.Services;

public interface IEmployeeRepository
{
    List<Employee> GetAllEmployees();
    List<Employee> GetEmployeesByIds(List<int> ids);
    Employee? GetEmployeeById(int id);
    Dictionary<int, List<string>> GetNameVariantMap();
    List<string> GetNameVariantsForEmployee(int id);
    void RefreshNameVariantCache();
}

public class EmployeeRepository : IEmployeeRepository
{
    // Rick & Morty themed employees loaded from JSON
    private static readonly List<Employee> Employees = LoadEmployeesFromJson();

    private static List<Employee> LoadEmployeesFromJson()
    {
        try
        {
            // Get the path to the JSON file
            var jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "employees.json");
            
            // If file doesn't exist, return empty list
            if (!File.Exists(jsonPath))
            {
                Console.WriteLine($"Employee JSON file not found at: {jsonPath}");
                return new List<Employee>();
            }

            // Read and deserialize JSON
            var jsonString = File.ReadAllText(jsonPath);
            var employees = JsonSerializer.Deserialize<List<Employee>>(jsonString, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return employees ?? new List<Employee>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading employees from JSON: {ex.Message}");
            return new List<Employee>();
        }
    }

    // Cache for name variant lookups to make fuzzy matching cheap
    private static Dictionary<int, List<string>>? _nameVariantsCache;
    private static readonly object _variantsLock = new();

    // Build a small set of useful name variants for fuzzy matching
    private static List<string> BuildNameVariants(Employee e)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var fn = (e.FirstName ?? string.Empty).Trim().ToLowerInvariant();
        var ln = (e.LastName ?? string.Empty).Trim().ToLowerInvariant();

        if (!string.IsNullOrEmpty(fn)) set.Add(fn);
        if (!string.IsNullOrEmpty(ln)) set.Add(ln);

        if (!string.IsNullOrEmpty(fn) && !string.IsNullOrEmpty(ln))
        {
            // common combinations
            set.Add($"{fn} {ln}");            // morty smith
            set.Add($"{ln} {fn}");            // smith morty
            set.Add($"{fn} {ln[0]}");         // morty s
            // set.Add($"{fn} {ln[0]}.");        // morty s.
            set.Add($"{fn[0]} {ln}");         // m smith
            set.Add($"{fn}{ln}");             // mortysmith

            // possessive / plural-ish forms that people sometimes type
            set.Add($"{fn}s");                // mortys
            set.Add($"{fn} {ln}s");           // morty smiths
            set.Add($"{fn}{ln}s");            // mortysmiths

            // initials
            // set.Add($"{fn[0]}{ln[0]}");       // ms
            set.Add($"{fn[0]}{ln}");          // msmith
        }

        // sanitize: remove empty / very short tokens
        return set.Where(s => !string.IsNullOrWhiteSpace(s) && s.Length > 1).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    // Public accessor: returns the cached map of employee id -> name variants
    public Dictionary<int, List<string>> GetNameVariantMap()
    {
        if (_nameVariantsCache != null)
            return _nameVariantsCache;

        lock (_variantsLock)
        {
            if (_nameVariantsCache == null)
            {
                _nameVariantsCache = Employees.ToDictionary(e => e.Id, e => BuildNameVariants(e));
            }
        }

        return _nameVariantsCache;
    }

    // Public accessor for a single employee's variants (falls back to empty list)
    public List<string> GetNameVariantsForEmployee(int id)
    {
        var map = GetNameVariantMap();
        return map.TryGetValue(id, out var list) ? list : new List<string>();
    }

    // Optional: allow refreshing the cache if Employees might change at runtime
    public void RefreshNameVariantCache()
    {
        lock (_variantsLock)
        {
            _nameVariantsCache = Employees.ToDictionary(e => e.Id, e => BuildNameVariants(e));
        }
    }

    public List<Employee> GetAllEmployees()
    {
        return Employees.ToList();
    }

    public List<Employee> GetEmployeesByIds(List<int> ids)
    {
        return Employees.Where(e => ids.Contains(e.Id)).ToList();
    }

    public Employee? GetEmployeeById(int id)
    {
        return Employees.FirstOrDefault(e => e.Id == id);
    }
}
