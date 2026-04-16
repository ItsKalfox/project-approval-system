using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PAS.API.DTOs.Submission;
using PAS.API.Services;

namespace PAS.API.Controllers;

/// <summary>
/// E-Assurance submission system — student project proposal submissions.
/// All endpoints require a valid Bearer JWT token with the STUDENT role.
/// </summary>
[ApiController]
[Route("api/submissions")]
[Authorize(Roles = "STUDENT")]
public class SubmissionController : ControllerBase
{
    private readonly ISubmissionService _submissionService;
    private readonly IResearchAreaService _researchAreaService;

    public SubmissionController(
        ISubmissionService submissionService,
        IResearchAreaService researchAreaService)
    {
        _submissionService   = submissionService;
        _researchAreaService = researchAreaService;
    }

    // ─── Helper: extract userId from JWT ─────────────────────────────────
    private int GetCurrentUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue("sub")
                  ?? User.FindFirstValue("userId");

        if (string.IsNullOrEmpty(sub) || !int.TryParse(sub, out var userId))
            throw new UnauthorizedAccessException("Unable to identify the logged-in user.");

        return userId;
    }

    // ─────────────────────────────────────────────────────────────────────
    // GET /api/submissions/submission-points
    // List all available submission points for the student
    // ─────────────────────────────────────────────────────────────────────
    [HttpGet("submission-points")]
    public async Task<IActionResult> GetSubmissionPoints()
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _submissionService.GetSubmissionPointsAsync(userId);
            return Ok(new
            {
                message = "Submission points retrieved successfully.",
                data    = result
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "An unexpected error occurred.",
                detail  = ex.Message
            });
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // GET /api/submissions/my-submissions
    // List all submissions for the logged-in student
    // ─────────────────────────────────────────────────────────────────────
    [HttpGet("my-submissions")]
    public async Task<IActionResult> GetMySubmissions()
    {
        try
        {
            var userId = GetCurrentUserId();
            var submissions = await _submissionService.GetStudentSubmissionsAsync(userId);
            return Ok(new
            {
                message = "Student submissions retrieved successfully.",
                data    = submissions
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "An unexpected error occurred.",
                detail  = ex.Message
            });
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // GET /api/submissions/research-areas
    // Research areas for the dropdown selection
    // ─────────────────────────────────────────────────────────────────────
    [HttpGet("research-areas")]
    public async Task<IActionResult> GetResearchAreas()
    {
        try
        {
            var areas = await _researchAreaService.GetAllResearchAreasAsync();
            return Ok(new
            {
                message = "Research areas retrieved successfully.",
                data    = areas
            });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "An unexpected error occurred.",
                detail  = ex.Message
            });
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // GET /api/submissions/coursework/{courseworkId}
    // Get the student's own submission for a specific coursework
    // ─────────────────────────────────────────────────────────────────────
    [HttpGet("coursework/{courseworkId:int}")]
    public async Task<IActionResult> GetMySubmission(int courseworkId)
    {
        try
        {
            var userId     = GetCurrentUserId();
            var submission = await _submissionService.GetMySubmissionAsync(userId, courseworkId);

            if (submission == null)
                return NotFound(new { message = "No submission found for this coursework." });

            return Ok(new
            {
                message = "Submission retrieved successfully.",
                data    = submission
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "An unexpected error occurred.",
                detail  = ex.Message
            });
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // POST /api/submissions/coursework/{courseworkId}
    // Create a new submission with PDF upload (multipart/form-data)
    // ─────────────────────────────────────────────────────────────────────
    [HttpPost("coursework/{courseworkId:int}")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> CreateSubmission(
        int courseworkId,
        [FromForm] CreateSubmissionDto dto,
        IFormFile file)
    {
        try
        {
            var userId     = GetCurrentUserId();
            var submission = await _submissionService.CreateSubmissionAsync(userId, courseworkId, dto, file);

            return StatusCode(StatusCodes.Status201Created, new
            {
                message = "Submission created successfully.",
                data    = submission
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "An unexpected error occurred.",
                detail  = ex.Message
            });
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // PUT /api/submissions/{projectId}
    // Update an existing submission before the deadline
    // ─────────────────────────────────────────────────────────────────────
    [HttpPut("{projectId:int}")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UpdateSubmission(
        int projectId,
        [FromForm] UpdateSubmissionDto dto,
        IFormFile? file = null)
    {
        try
        {
            var userId     = GetCurrentUserId();
            var submission = await _submissionService.UpdateSubmissionAsync(userId, projectId, dto, file);

            return Ok(new
            {
                message = "Submission updated successfully.",
                data    = submission
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "An unexpected error occurred.",
                detail  = ex.Message
            });
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // DELETE /api/submissions/{projectId}
    // Delete a submission before the deadline
    // ─────────────────────────────────────────────────────────────────────
    [HttpDelete("{projectId:int}")]
    public async Task<IActionResult> DeleteSubmission(int projectId)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _submissionService.DeleteSubmissionAsync(userId, projectId);

            return Ok(new { message = "Submission deleted successfully." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "An unexpected error occurred.",
                detail  = ex.Message
            });
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // GET /api/submissions/{projectId}/file
    // Securely view/download the submitted PDF
    // ─────────────────────────────────────────────────────────────────────
    [HttpGet("{projectId:int}/file")]
    public async Task<IActionResult> GetSubmissionFile(int projectId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var (fileStream, contentType, fileName) =
                await _submissionService.GetSubmissionFileAsync(userId, projectId);

            return File(fileStream, contentType, fileName);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (FileNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "An unexpected error occurred.",
                detail  = ex.Message
            });
        }
    }
}
