using MLIntentClassifierAPI.Models;

namespace MLIntentClassifierAPI.Services;

public class EmployeeQueryOrchestrator
{
    // Mock employee data
    private static readonly List<Employee> Employees = new()
    {
        new Employee {
            Name = "Rick Sanchez", Department = "Engineering", Role = "Manager", Location = "Salt Lake City", Email = "rick@corp.com", Phone = "555-0001",
            HireDate = new DateTime(2015, 3, 1), Birthday = new DateTime(1970, 1, 26),
            Address = new EmployeeAddress { Street = "123 Portal Ave", City = "Salt Lake City", State = "UT", Zip = "84101" }
        },
        new Employee {
            Name = "Summer Smith", Department = "Engineering", Role = "Software Engineer", Location = "Salt Lake City", Email = "summer@corp.com", Phone = "555-0002",
            HireDate = new DateTime(2021, 6, 15), Birthday = new DateTime(2001, 6, 21),
            Address = new EmployeeAddress { Street = "456 Summer St", City = "Salt Lake City", State = "UT", Zip = "84102" }
        },
        new Employee {
            Name = "Morty Smith", Department = "Sales", Role = "Team Lead", Location = "Reno", Email = "morty@corp.com", Phone = "555-0003",
            HireDate = new DateTime(2019, 9, 10), Birthday = new DateTime(2003, 2, 19),
            Address = new EmployeeAddress { Street = "789 Morty Rd", City = "Reno", State = "NV", Zip = "89501" }
        },
        new Employee {
            Name = "Beth Smith", Department = "Human Resources", Role = "Director", Location = "Headquarters", Email = "beth@corp.com", Phone = "555-0004",
            HireDate = new DateTime(2010, 2, 5), Birthday = new DateTime(1975, 8, 12),
            Address = new EmployeeAddress { Street = "101 Beth Blvd", City = "Salt Lake City", State = "UT", Zip = "84103" }
        },
        new Employee {
            Name = "Jerry Smith", Department = "Support", Role = "Supervisor", Location = "Provo", Email = "jerry@corp.com", Phone = "555-0005",
            HireDate = new DateTime(2018, 11, 20), Birthday = new DateTime(1972, 4, 2),
            Address = new EmployeeAddress { Street = "202 Jerry Ln", City = "Provo", State = "UT", Zip = "84601" }
        },
        new Employee {
            Name = "Carol Danvers", Department = "Engineering", Role = "Software Engineer", Location = "Salt Lake City", Email = "carol@corp.com", Phone = "555-0006",
            HireDate = new DateTime(2022, 1, 10), Birthday = new DateTime(1985, 10, 24),
            Address = new EmployeeAddress { Street = "303 Carol Ct", City = "Salt Lake City", State = "UT", Zip = "84104" }
        },
        new Employee {
            Name = "Tony Stark", Department = "Engineering", Role = "Director", Location = "Salt Lake City", Email = "tony@corp.com", Phone = "555-0007",
            HireDate = new DateTime(2012, 5, 4), Birthday = new DateTime(1975, 5, 29),
            Address = new EmployeeAddress { Street = "404 Stark Tower", City = "Salt Lake City", State = "UT", Zip = "84105" }
        },
        new Employee {
            Name = "Bruce Wayne", Department = "Sales", Role = "Manager", Location = "Las Vegas", Email = "bruce@corp.com", Phone = "555-0008",
            HireDate = new DateTime(2016, 7, 18), Birthday = new DateTime(1972, 2, 19),
            Address = new EmployeeAddress { Street = "505 Wayne Manor", City = "Las Vegas", State = "NV", Zip = "89101" }
        },
        new Employee {
            Name = "Alice Johnson", Department = "Support", Role = "Supervisor", Location = "Ogden", Email = "alice@corp.com", Phone = "555-0009",
            HireDate = new DateTime(2020, 3, 22), Birthday = new DateTime(1990, 9, 10),
            Address = new EmployeeAddress { Street = "606 Alice Ave", City = "Ogden", State = "UT", Zip = "84401" }
        },
        new Employee {
            Name = "Bob Lee", Department = "Engineering", Role = "Software Engineer", Location = "Salt Lake City", Email = "bob@corp.com", Phone = "555-0010",
            HireDate = new DateTime(2023, 2, 1), Birthday = new DateTime(1995, 12, 5),
            Address = new EmployeeAddress { Street = "707 Bob Blvd", City = "Salt Lake City", State = "UT", Zip = "84106" }
        },
        // New employees
        new Employee {
            Name = "Sarah Connor", Department = "IT", Role = "SysAdmin", Location = "San Jose", Email = "sarah@corp.com", Phone = "555-0011",
            HireDate = new DateTime(2017, 8, 12), Birthday = new DateTime(1980, 5, 12),
            Address = new EmployeeAddress { Street = "111 Connor Rd", City = "San Jose", State = "CA", Zip = "95101" }
        },
        new Employee {
            Name = "Ellen Ripley", Department = "Research and Development", Role = "Scientist", Location = "Palo Alto", Email = "ellen@corp.com", Phone = "555-0012",
            HireDate = new DateTime(2014, 4, 18), Birthday = new DateTime(1978, 10, 8),
            Address = new EmployeeAddress { Street = "222 Ripley St", City = "Palo Alto", State = "CA", Zip = "94301" }
        },
        new Employee {
            Name = "Peter Parker", Department = "Marketing", Role = "Content Creator", Location = "Los Angeles", Email = "peter@corp.com", Phone = "555-0013",
            HireDate = new DateTime(2020, 7, 1), Birthday = new DateTime(1998, 8, 10),
            Address = new EmployeeAddress { Street = "333 Parker Ave", City = "Los Angeles", State = "CA", Zip = "90001" }
        },
        new Employee {
            Name = "Clark Kent", Department = "Legal", Role = "Attorney", Location = "San Francisco", Email = "clark@corp.com", Phone = "555-0014",
            HireDate = new DateTime(2013, 3, 15), Birthday = new DateTime(1976, 6, 18),
            Address = new EmployeeAddress { Street = "444 Kent Blvd", City = "San Francisco", State = "CA", Zip = "94101" }
        },
        new Employee {
            Name = "Diana Prince", Department = "Marketing", Role = "Manager", Location = "San Diego", Email = "diana@corp.com", Phone = "555-0015",
            HireDate = new DateTime(2018, 11, 30), Birthday = new DateTime(1985, 3, 22),
            Address = new EmployeeAddress { Street = "555 Diana Dr", City = "San Diego", State = "CA", Zip = "92101" }
        },
        new Employee {
            Name = "Barry Allen", Department = "IT", Role = "Network Engineer", Location = "Sacramento", Email = "barry@corp.com", Phone = "555-0016",
            HireDate = new DateTime(2022, 2, 14), Birthday = new DateTime(1992, 9, 30),
            Address = new EmployeeAddress { Street = "666 Allen Way", City = "Sacramento", State = "CA", Zip = "95814" }
        },
        new Employee {
            Name = "Hal Jordan", Department = "Research and Development", Role = "Engineer", Location = "Fresno", Email = "hal@corp.com", Phone = "555-0017",
            HireDate = new DateTime(2016, 5, 20), Birthday = new DateTime(1982, 7, 7),
            Address = new EmployeeAddress { Street = "777 Jordan Cir", City = "Fresno", State = "CA", Zip = "93701" }
        },
        new Employee {
            Name = "Arthur Curry", Department = "Legal", Role = "Paralegal", Location = "Bakersfield", Email = "arthur@corp.com", Phone = "555-0018",
            HireDate = new DateTime(2021, 10, 5), Birthday = new DateTime(1990, 12, 25),
            Address = new EmployeeAddress { Street = "888 Curry Pl", City = "Bakersfield", State = "CA", Zip = "93301" }
        },
        new Employee {
            Name = "Victor Stone", Department = "IT", Role = "Support Specialist", Location = "Riverside", Email = "victor@corp.com", Phone = "555-0019",
            HireDate = new DateTime(2019, 6, 8), Birthday = new DateTime(1993, 11, 11),
            Address = new EmployeeAddress { Street = "999 Stone Rd", City = "Riverside", State = "CA", Zip = "92501" }
        },
        new Employee {
            Name = "Selina Kyle", Department = "Marketing", Role = "Designer", Location = "Anaheim", Email = "selina@corp.com", Phone = "555-0020",
            HireDate = new DateTime(2017, 9, 17), Birthday = new DateTime(1987, 2, 14),
            Address = new EmployeeAddress { Street = "1010 Kyle St", City = "Anaheim", State = "CA", Zip = "92801" }
        },
        new Employee {
            Name = "Harvey Dent", Department = "Legal", Role = "Attorney", Location = "Long Beach", Email = "harvey@corp.com", Phone = "555-0021",
            HireDate = new DateTime(2015, 12, 2), Birthday = new DateTime(1979, 4, 17),
            Address = new EmployeeAddress { Street = "1111 Dent Ave", City = "Long Beach", State = "CA", Zip = "90802" }
        },
        new Employee {
            Name = "Pamela Isley", Department = "Research and Development", Role = "Botanist", Location = "Santa Barbara", Email = "pamela@corp.com", Phone = "555-0022",
            HireDate = new DateTime(2016, 8, 23), Birthday = new DateTime(1984, 5, 5),
            Address = new EmployeeAddress { Street = "1212 Isley Blvd", City = "Santa Barbara", State = "CA", Zip = "93101" }
        },
        new Employee {
            Name = "Edward Nigma", Department = "IT", Role = "Security Analyst", Location = "Irvine", Email = "edward@corp.com", Phone = "555-0023",
            HireDate = new DateTime(2018, 3, 3), Birthday = new DateTime(1986, 8, 8),
            Address = new EmployeeAddress { Street = "1313 Nigma St", City = "Irvine", State = "CA", Zip = "92602" }
        },
        new Employee {
            Name = "Lucius Fox", Department = "Engineering", Role = "Architect", Location = "Henderson", Email = "lucius@corp.com", Phone = "555-0024",
            HireDate = new DateTime(2014, 11, 11), Birthday = new DateTime(1977, 3, 3),
            Address = new EmployeeAddress { Street = "1414 Fox Rd", City = "Henderson", State = "NV", Zip = "89002" }
        },
        new Employee {
            Name = "Barbara Gordon", Department = "Support", Role = "Help Desk", Location = "St. George", Email = "barbara@corp.com", Phone = "555-0025",
            HireDate = new DateTime(2020, 5, 20), Birthday = new DateTime(1992, 7, 7),
            Address = new EmployeeAddress { Street = "1515 Gordon Ave", City = "St. George", State = "UT", Zip = "84770" }
        },
        new Employee {
            Name = "Alfred Pennyworth", Department = "Human Resources", Role = "Recruiter", Location = "Elko", Email = "alfred@corp.com", Phone = "555-0026",
            HireDate = new DateTime(2011, 1, 1), Birthday = new DateTime(1965, 9, 15),
            Address = new EmployeeAddress { Street = "1616 Pennyworth St", City = "Elko", State = "NV", Zip = "89801" }
        },
        new Employee {
            Name = "Jonathan Crane", Department = "Research and Development", Role = "Scientist", Location = "Carson City", Email = "jonathan@corp.com", Phone = "555-0027",
            HireDate = new DateTime(2013, 6, 6), Birthday = new DateTime(1981, 10, 31),
            Address = new EmployeeAddress { Street = "1717 Crane Blvd", City = "Carson City", State = "NV", Zip = "89701" }
        },
        new Employee {
            Name = "Harleen Quinzel", Department = "Legal", Role = "Legal Assistant", Location = "Reno", Email = "harleen@corp.com", Phone = "555-0028",
            HireDate = new DateTime(2019, 4, 4), Birthday = new DateTime(1991, 2, 2),
            Address = new EmployeeAddress { Street = "1818 Quinzel Rd", City = "Reno", State = "NV", Zip = "89502" }
        },
        new Employee {
            Name = "Oswald Cobblepot", Department = "Marketing", Role = "Analyst", Location = "Las Vegas", Email = "oswald@corp.com", Phone = "555-0029",
            HireDate = new DateTime(2015, 7, 7), Birthday = new DateTime(1983, 11, 11),
            Address = new EmployeeAddress { Street = "1919 Cobblepot Ave", City = "Las Vegas", State = "NV", Zip = "89102" }
        },
        new Employee {
            Name = "Victor Fries", Department = "Research and Development", Role = "Engineer", Location = "Salt Lake City", Email = "victor@corp.com", Phone = "555-0030",
            HireDate = new DateTime(2017, 12, 12), Birthday = new DateTime(1988, 12, 24),
            Address = new EmployeeAddress { Street = "2020 Fries St", City = "Salt Lake City", State = "UT", Zip = "84107" }
        },
        new Employee {
            Name = "Talia al Ghul", Department = "Legal", Role = "Attorney", Location = "San Jose", Email = "talia@corp.com", Phone = "555-0031",
            HireDate = new DateTime(2016, 8, 8), Birthday = new DateTime(1989, 5, 5),
            Address = new EmployeeAddress { Street = "2121 Ghul Blvd", City = "San Jose", State = "CA", Zip = "95112" }
        },
        new Employee {
            Name = "Bane", Department = "Support", Role = "Supervisor", Location = "Ogden", Email = "bane@corp.com", Phone = "555-0032",
            HireDate = new DateTime(2018, 10, 10), Birthday = new DateTime(1980, 8, 8),
            Address = new EmployeeAddress { Street = "2222 Bane St", City = "Ogden", State = "UT", Zip = "84402" }
        },
        new Employee {
            Name = "Ra's al Ghul", Department = "Research and Development", Role = "Scientist", Location = "Palo Alto", Email = "ras@corp.com", Phone = "555-0033",
            HireDate = new DateTime(2012, 2, 2), Birthday = new DateTime(1970, 3, 3),
            Address = new EmployeeAddress { Street = "2323 Ras Rd", City = "Palo Alto", State = "CA", Zip = "94302" }
        },
        new Employee {
            Name = "Zatanna Zatara", Department = "Marketing", Role = "Content Creator", Location = "Los Angeles", Email = "zatanna@corp.com", Phone = "555-0034",
            HireDate = new DateTime(2021, 11, 11), Birthday = new DateTime(1994, 7, 7),
            Address = new EmployeeAddress { Street = "2424 Zatara Ave", City = "Los Angeles", State = "CA", Zip = "90002" }
        },
        new Employee {
            Name = "John Constantine", Department = "IT", Role = "SysAdmin", Location = "San Diego", Email = "john@corp.com", Phone = "555-0035",
            HireDate = new DateTime(2015, 5, 5), Birthday = new DateTime(1982, 1, 1),
            Address = new EmployeeAddress { Street = "2525 Constantine St", City = "San Diego", State = "CA", Zip = "92102" }
        },
        new Employee {
            Name = "Kara Zor-El", Department = "Engineering", Role = "Software Engineer", Location = "Salt Lake City", Email = "kara@corp.com", Phone = "555-0036",
            HireDate = new DateTime(2023, 3, 3), Birthday = new DateTime(1996, 6, 6),
            Address = new EmployeeAddress { Street = "2626 Kara Blvd", City = "Salt Lake City", State = "UT", Zip = "84108" }
        },
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
    public DateTime? HireDate { get; set; }
    public DateTime? Birthday { get; set; }
    public EmployeeAddress? Address { get; set; }
}

public class EmployeeAddress
{
    public string Street { get; set; } = "";
    public string City { get; set; } = "";
    public string State { get; set; } = "";
    public string Zip { get; set; } = "";
    public string Print => $"{Street}, {City}, {State} {Zip}";
}
