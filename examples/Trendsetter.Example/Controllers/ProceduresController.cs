namespace Trendsetter.Example.Controllers;

using Microsoft.AspNetCore.Mvc;
using Trendsetter.Example.Models;
using Trendsetter.Example.Services;

[ApiController]
[Route("[controller]")]
public class ProceduresController : ControllerBase
{
    private readonly IMyAiService _aiService;

    public ProceduresController(IMyAiService aiService)
    {
        _aiService = aiService;
    }

    [HttpGet("extract")]
    public async Task<ActionResult<IReadOnlyList<ProcedureModel>>> Extract()
    {
        string input = """
            Patient: John Doe
            Date of Treatment: 03/15/2024
            Provider: Dr. Smith
            Procedure: Rotator cuff repair
            Description: Surgical repair of torn rotator cuff tendon
            Diagnosis: M75.1 - Rotator cuff syndrome
            """;
        var results = await _aiService.ExtractProceduresAsync(input);
        return Ok(results);
    }
}
