using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Hackathon.Api.DTOs.Submissions;
using Hackathon.Api.Services;

namespace Hackathon.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SubmissionsController : ControllerBase
{
    private readonly ISubmissionService _submissionService;

    public SubmissionsController(ISubmissionService submissionService)
    {
        _submissionService = submissionService;
    }

    /// <summary>
    /// Przesyła rozwiązanie dla wyzwania
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Submit([FromForm] IFormFile file, [FromForm] int challengeId)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "File is required" });
        }

        // TODO: Implementacja przesyłania rozwiązania
        return Ok(new { message = "Submission received and queued for evaluation" });
    }

    /// <summary>
    /// Pobiera historię zgłoszeń zalogowanego użytkownika
    /// </summary>
    [HttpGet("my")]
    public async Task<ActionResult<IEnumerable<SubmissionDto>>> GetMySubmissions()
    {
        // TODO: Implementacja pobierania historii zgłoszeń
        return Ok(new List<SubmissionDto>());
    }
}
