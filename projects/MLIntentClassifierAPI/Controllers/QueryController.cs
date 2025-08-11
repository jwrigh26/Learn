using Microsoft.AspNetCore.Mvc;
using MLIntentClassifierAPI.Models;
using MLIntentClassifierAPI.Services;

namespace MLIntentClassifierAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QueryController : ControllerBase
{
    private readonly QueryUnderstandingService _queryService;
    private readonly ILogger<QueryController> _logger;

    public QueryController(QueryUnderstandingService queryService, ILogger<QueryController> logger)
    {
        _queryService = queryService;
        _logger = logger;
    }

    [HttpPost("understand")]
    public ActionResult<QueryUnderstanding> UnderstandQuery([FromBody] QueryRequest request)
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
            
            return Ok(understanding);
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
    public ActionResult<List<QueryUnderstanding>> TestQueries()
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

        var results = testQueries.Select(q => new
        {
            Query = q,
            Understanding = _queryService.Understand(q)
        }).ToList();

        return Ok(results);
    }
}

public class QueryRequest
{
    public string Text { get; set; } = "";
}
