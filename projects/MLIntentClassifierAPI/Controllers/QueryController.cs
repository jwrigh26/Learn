using Microsoft.AspNetCore.Mvc;
using MLIntentClassifierAPI.Models;
using MLIntentClassifierAPI.Services;

namespace MLIntentClassifierAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QueryController : ControllerBase
{
    private readonly QueryUnderstandingService _queryService;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IFuzzyService _fuzzyService;
    private readonly ILogger<QueryController> _logger;

    public QueryController(
        QueryUnderstandingService queryService, 
        IEmployeeRepository employeeRepository,
        IFuzzyService fuzzyService,
        ILogger<QueryController> logger)
    {
        _queryService = queryService;
        _employeeRepository = employeeRepository;
        _fuzzyService = fuzzyService;
        _logger = logger;
    }

    [HttpPost("understand")]
    public ActionResult<object> UnderstandQuery([FromBody] QueryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
        {
            return BadRequest("Query text cannot be empty");
        }

        try
        {
            var understanding = _queryService.Understand(request.Text);
            _logger.LogInformation("Processed query: {Query} -> Intent: {Intent}", 
                request.Text, understanding.Intent);

            // Project intent as object with id and label
            var result = new {
                intent = new {
                    id = (int)understanding.Intent,
                    label = understanding.Intent.ToString()
                },
                slots = understanding.Slots,
                employees = understanding.Employees,
                nameVariations = understanding.NameVariantMap,
            };
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing query: {Query}", request.Text);
            return StatusCode(500, "An error occurred while processing the query");
        }
    }

    [HttpGet("health")]
    public ActionResult<object> Health()
    {
        return Ok(new { 
            Status = "Healthy", 
            Timestamp = DateTime.UtcNow,
            Version = "1.0.0"
        });
    }

    [HttpGet("test-queries")]
    public ActionResult<List<object>> TestQueries()
    {
        var testQueries = new[]
        {
            "Show me managers in engineering hired before 2024",
            "Emails for rick, summer and morty",
            "All employees between 2020 and 2023 in HR",
            "Who is remote in SLC?",
            "Phone for Carol Danvers",
            "Directors hired after last year in sales"
        };

        var results = testQueries.Select(q => new {
            query = q,
            understanding = new {
                intent = new {
                    id = (int)_queryService.Understand(q).Intent,
                    label = _queryService.Understand(q).Intent.ToString()
                },
                slots = _queryService.Understand(q).Slots
            }
        }).ToList();

        return Ok(results);
    }

    [HttpPost("test-fuzzy")]
    public ActionResult<object> TestFuzzy([FromBody] QueryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
        {
            return BadRequest("Query text cannot be empty");
        }

        try
        {
            var nameVariantMap = _employeeRepository.GetNameVariantMap();
            var matches = _fuzzyService.ExtractNamesFromQuery(request.Text, nameVariantMap);
            
            // Get the actual employee objects for the matches
            var employeeIds = matches.Select(m => m.EmployeeId).Distinct().ToList();
            var employees = _employeeRepository.GetEmployeesByIds(employeeIds);
            
            var result = new {
                query = request.Text,
                nameMatches = matches.Select(m => new {
                    employeeId = m.EmployeeId,
                    employee = employees.FirstOrDefault(e => e.Id == m.EmployeeId),
                    matchedVariant = m.MatchedVariant,
                    queryToken = m.QueryToken,
                    score = m.Score,
                    matchType = m.MatchType
                }).ToList(),
                summary = new {
                    totalMatches = matches.Count,
                    uniqueEmployees = employeeIds.Count,
                    averageScore = matches.Any() ? matches.Average(m => m.Score) : 0
                }
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in fuzzy name matching for query: {Query}", request.Text);
            return StatusCode(500, "An error occurred while processing the fuzzy search");
        }
    }
}

public class QueryRequest
{
    public string Text { get; set; } = "";
}
