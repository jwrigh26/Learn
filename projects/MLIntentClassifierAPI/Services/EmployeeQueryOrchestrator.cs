using MLIntentClassifierAPI.Models;

namespace MLIntentClassifierAPI.Services;

public class EmployeeQueryOrchestrator
{
    // Mock employee data
    private static readonly List<Employee> Employees = new()
    {
        new Employee { Name = "Rick Sanchez", Department = "Engineering", Role = "Manager", Location = "Salt Lake City", Email = "rick@corp.com", Phone = "555-0001" },
        new Employee { Name = "Summer Smith", Department = "Engineering", Role = "Software Engineer", Location = "Salt Lake City", Email = "summer@corp.com", Phone = "555-0002" },
        new Employee { Name = "Morty Smith", Department = "Sales", Role = "Team Lead", Location = "Reno", Email = "morty@corp.com", Phone = "555-0003" },
        new Employee { Name = "Beth Smith", Department = "Human Resources", Role = "Director", Location = "Headquarters", Email = "beth@corp.com", Phone = "555-0004" },
        new Employee { Name = "Jerry Smith", Department = "Support", Role = "Supervisor", Location = "Provo", Email = "jerry@corp.com", Phone = "555-0005" },
        new Employee { Name = "Carol Danvers", Department = "Engineering", Role = "Software Engineer", Location = "Salt Lake City", Email = "carol@corp.com", Phone = "555-0006" },
        new Employee { Name = "Tony Stark", Department = "Engineering", Role = "Director", Location = "Salt Lake City", Email = "tony@corp.com", Phone = "555-0007" },
        new Employee { Name = "Bruce Wayne", Department = "Sales", Role = "Manager", Location = "Las Vegas", Email = "bruce@corp.com", Phone = "555-0008" },
        new Employee { Name = "Alice Johnson", Department = "Support", Role = "Supervisor", Location = "Ogden", Email = "alice@corp.com", Phone = "555-0009" },
        new Employee { Name = "Bob Lee", Department = "Engineering", Role = "Software Engineer", Location = "Salt Lake City", Email = "bob@corp.com", Phone = "555-0010" },
        // New employees
        new Employee { Name = "Sarah Connor", Department = "IT", Role = "SysAdmin", Location = "San Jose", Email = "sarah@corp.com", Phone = "555-0011" },
        new Employee { Name = "Ellen Ripley", Department = "Research and Development", Role = "Scientist", Location = "Palo Alto", Email = "ellen@corp.com", Phone = "555-0012" },
        new Employee { Name = "Peter Parker", Department = "Marketing", Role = "Content Creator", Location = "Los Angeles", Email = "peter@corp.com", Phone = "555-0013" },
        new Employee { Name = "Clark Kent", Department = "Legal", Role = "Attorney", Location = "San Francisco", Email = "clark@corp.com", Phone = "555-0014" },
        new Employee { Name = "Diana Prince", Department = "Marketing", Role = "Manager", Location = "San Diego", Email = "diana@corp.com", Phone = "555-0015" },
        new Employee { Name = "Barry Allen", Department = "IT", Role = "Network Engineer", Location = "Sacramento", Email = "barry@corp.com", Phone = "555-0016" },
        new Employee { Name = "Hal Jordan", Department = "Research and Development", Role = "Engineer", Location = "Fresno", Email = "hal@corp.com", Phone = "555-0017" },
        new Employee { Name = "Arthur Curry", Department = "Legal", Role = "Paralegal", Location = "Bakersfield", Email = "arthur@corp.com", Phone = "555-0018" },
        new Employee { Name = "Victor Stone", Department = "IT", Role = "Support Specialist", Location = "Riverside", Email = "victor@corp.com", Phone = "555-0019" },
        new Employee { Name = "Selina Kyle", Department = "Marketing", Role = "Designer", Location = "Anaheim", Email = "selina@corp.com", Phone = "555-0020" },
        new Employee { Name = "Harvey Dent", Department = "Legal", Role = "Attorney", Location = "Long Beach", Email = "harvey@corp.com", Phone = "555-0021" },
        new Employee { Name = "Pamela Isley", Department = "Research and Development", Role = "Botanist", Location = "Santa Barbara", Email = "pamela@corp.com", Phone = "555-0022" },
        new Employee { Name = "Edward Nigma", Department = "IT", Role = "Security Analyst", Location = "Irvine", Email = "edward@corp.com", Phone = "555-0023" },
        new Employee { Name = "Lucius Fox", Department = "Engineering", Role = "Architect", Location = "Henderson", Email = "lucius@corp.com", Phone = "555-0024" },
        new Employee { Name = "Barbara Gordon", Department = "Support", Role = "Help Desk", Location = "St. George", Email = "barbara@corp.com", Phone = "555-0025" },
        new Employee { Name = "Alfred Pennyworth", Department = "Human Resources", Role = "Recruiter", Location = "Elko", Email = "alfred@corp.com", Phone = "555-0026" },
        new Employee { Name = "Jonathan Crane", Department = "Research and Development", Role = "Scientist", Location = "Carson City", Email = "jonathan@corp.com", Phone = "555-0027" },
        new Employee { Name = "Harleen Quinzel", Department = "Legal", Role = "Legal Assistant", Location = "Reno", Email = "harleen@corp.com", Phone = "555-0028" },
        new Employee { Name = "Oswald Cobblepot", Department = "Marketing", Role = "Analyst", Location = "Las Vegas", Email = "oswald@corp.com", Phone = "555-0029" },
        new Employee { Name = "Victor Fries", Department = "Research and Development", Role = "Engineer", Location = "Salt Lake City", Email = "victor@corp.com", Phone = "555-0030" },
        new Employee { Name = "Talia al Ghul", Department = "Legal", Role = "Attorney", Location = "San Jose", Email = "talia@corp.com", Phone = "555-0031" },
        new Employee { Name = "Bane", Department = "Support", Role = "Supervisor", Location = "Ogden", Email = "bane@corp.com", Phone = "555-0032" },
        new Employee { Name = "Ra's al Ghul", Department = "Research and Development", Role = "Scientist", Location = "Palo Alto", Email = "ras@corp.com", Phone = "555-0033" },
        new Employee { Name = "Zatanna Zatara", Department = "Marketing", Role = "Content Creator", Location = "Los Angeles", Email = "zatanna@corp.com", Phone = "555-0034" },
        new Employee { Name = "John Constantine", Department = "IT", Role = "SysAdmin", Location = "San Diego", Email = "john@corp.com", Phone = "555-0035" },
        new Employee { Name = "Kara Zor-El", Department = "Engineering", Role = "Software Engineer", Location = "Salt Lake City", Email = "kara@corp.com", Phone = "555-0036" }
    };

    public object QueryEmployees(QueryUnderstanding understanding)
    {
        var slots = understanding.Slots;
        var filtered = Employees.AsEnumerable();

        // Filter by names if present
        if (slots.Names != null && slots.Names.Count > 0)
            filtered = filtered.Where(e => slots.Names.Contains(e.Name));

        // Filter by department
        if (!string.IsNullOrWhiteSpace(slots.Department))
            filtered = filtered.Where(e => e.Department == slots.Department);

        // Filter by role
        if (!string.IsNullOrWhiteSpace(slots.Role))
            filtered = filtered.Where(e => e.Role == slots.Role);

        // Filter by location
        if (!string.IsNullOrWhiteSpace(slots.Location))
            filtered = filtered.Where(e => e.Location == slots.Location);

        // Project only relevant info (for demo, return all fields)
        var result = filtered.Select(e => new {
            e.Name,
            e.Department,
            e.Role,
            e.Location,
            e.Email,
            e.Phone
        }).ToList();

        return new {
            intent = new { id = (int)understanding.Intent, label = understanding.Intent.ToString() },
            slots,
            employees = result
        };
    }

    public static List<string> GetAllEmployeeNames()
    {
        return Employees.Select(e => e.Name).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }
}

public class Employee
{
    public string Name { get; set; } = "";
    public string Department { get; set; } = "";
    public string Role { get; set; } = "";
    public string Location { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
}
