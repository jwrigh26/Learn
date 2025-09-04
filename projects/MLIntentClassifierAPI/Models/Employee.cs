namespace MLIntentClassifierAPI.Models;

public class Employee
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public string EmployeeCode { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Address1 { get; set; } = "";
    public string Address2 { get; set; } = "";
    public string City { get; set; } = "";
    public string State { get; set; } = "";
    public string Country { get; set; } = "";
    public string TimeZone { get; set; } = "";
    public string Department { get; set; } = "";
    public string Location { get; set; } = "";
    public string Position { get; set; } = "";
    public string Job { get; set; } = "";
    public string OriginalHireDate { get; set; } = "";
    public string Status { get; set; } = "";
    public string Title { get; set; } = "";
    public string Name => $"{FirstName} {LastName}";
}