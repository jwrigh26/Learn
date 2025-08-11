using Microsoft.AspNetCore.Mvc;
using MLIntentClassifierAPI.Models;
using MLIntentClassifierAPI.Services;

namespace MLIntentClassifierAPI.Controllers;

[ApiController]
[Route("api/employee/workflow")]
public class EmployeeWorkflowController : ControllerBase
{
    private readonly QueryUnderstandingService _queryService;
    private readonly EmployeeQueryOrchestrator _orchestrator;
    private readonly ILogger<EmployeeWorkflowController> _logger;

    public EmployeeWorkflowController(QueryUnderstandingService queryService, ILogger<EmployeeWorkflowController> logger)
    {
        _queryService = queryService;
        _orchestrator = new EmployeeQueryOrchestrator();
        _logger = logger;
    }

    [HttpPost]
    public ActionResult<object> RunWorkflow([FromBody] QueryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
            return BadRequest("Query text cannot be empty");

        try
        {
            // Step 1: Extract intent and slots
            var understanding = _queryService.Understand(request.Text);
            _logger.LogInformation("Workflow: {Query} -> Intent: {Intent}", request.Text, understanding.Intent);

            // Step 2: Decide what to do (for now, always call EmployeeQueryOrchestrator)
            var result = _orchestrator.QueryEmployees(understanding);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in workflow for query: {Query}", request.Text);
            return StatusCode(500, "An error occurred while processing the workflow");
        }
    }
}
