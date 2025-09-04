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
    private readonly ISlotService _slotService;
    private readonly ILogger<QueryController> _logger;

    public QueryController(
        QueryUnderstandingService queryService, 
        IEmployeeRepository employeeRepository,
        IFuzzyService fuzzyService,
        ISlotService slotService,
        ILogger<QueryController> logger)
    {
        _queryService = queryService;
        _employeeRepository = employeeRepository;
        _fuzzyService = fuzzyService;
        _slotService = slotService;
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
            // 1. Pure NLP processing (intent + slots)
            var understanding = _queryService.Understand(request.Text);
            
            // 2. Employee fuzzy matching (separate concern)
            var nameVariantMap = _employeeRepository.GetNameVariantMap();
            var nameMatches = _fuzzyService.ExtractNamesFromQuery(request.Text, nameVariantMap);
            var matchedEmployeeIds = nameMatches.Select(m => m.EmployeeId).ToList();
            var matchedEmployees = _employeeRepository.GetEmployeesByIds(matchedEmployeeIds);
            
            _logger.LogInformation("Processed query: {Query} -> Intent: {Intent}, Found {EmployeeCount} employees", 
                request.Text, understanding.Intent, matchedEmployees.Count);

            // 3. Combine results
            var result = new {
                intent = new {
                    id = (int)understanding.Intent,
                    label = understanding.Intent.ToString()
                },
                slots = understanding.Slots,
                employees = matchedEmployees,
                nameMatches = nameMatches.Select(m => new {
                    employeeId = m.EmployeeId,
                    matchedVariant = m.MatchedVariant,
                    queryToken = m.QueryToken,
                    score = m.Score,
                    matchType = m.MatchType
                }).ToList(),
                // Keep nameVariations for backward compatibility if needed
                nameVariations = nameVariantMap
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
            "Do you know bird person's email?",
            "Phone for Carol Danvers",
            "Directors hired after last year in sales"
        };

        var results = testQueries.Select(q => new {
            query = q,
            intent = new {
                id = (int)_queryService.Understand(q).Intent,
                label = _queryService.Understand(q).Intent.ToString()
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

    [HttpPost("test-slots")]
    public ActionResult<object> TestSlots([FromBody] QueryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
        {
            return BadRequest("Query text cannot be empty");
        }

        try
        {
            var slots = _slotService.ExtractSlots(request.Text);
            var fieldSets = _slotService.GetOrgValueSets();

            // Also expose what values were considered per field
            return Ok(new
            {
                query = request.Text,
                slots = new
                {
                    fields = slots.Fields,
                    op = slots.Operator,
                    date = slots.Date,
                    range = slots.Range
                },
                knownValues = fieldSets
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing slots for query: {Query}", request.Text);
            return StatusCode(500, "An error occurred while testing slots");
        }
    }
}

public class QueryRequest
{
    public string Text { get; set; } = "";
}
