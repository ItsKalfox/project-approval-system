using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PAS.API.Services;

namespace PAS.API.Controllers;

[ApiController]
[Route("api/supervisor/dashboard")]
[Authorize(Policy = "SupervisorOnly")]
public class SupervisorDashboardController : ControllerBase
{
    private readonly ISupervisorDashboardService _service;

    public SupervisorDashboardController(ISupervisorDashboardService service)
    {
        _service = service;
    }

    [HttpGet("projects")]
public async Task<IActionResult> GetAvailableProjects(
    [FromQuery] int courseworkId,
    [FromQuery] int? researchAreaId)
{
    try
    {
        var userId   = GetCurrentUserId();
        var projects = await _service.GetAvailableProjectsAsync(userId, courseworkId, researchAreaId);
        return Ok(new { message = "Projects retrieved.", data = projects });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { message = "Unexpected error.", detail = ex.Message });
    }
}

    [HttpGet("projects/{id:int}/proposal")]
    public async Task<IActionResult> GetProposalPdf(int id)
    {
        try
        {
            var (stream, contentType, fileName) = await _service.GetProposalPdfAsync(id);
            return File(stream, contentType, fileName);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Unexpected error.", detail = ex.Message });
        }
    }

    [HttpPost("projects/{id:int}/interest")]
    public async Task<IActionResult> ExpressInterest(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _service.ExpressInterestAsync(userId, id);
            return Ok(new { message = "Interest recorded successfully." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Unexpected error.", detail = ex.Message });
        }
    }

    private int GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value
            ?? throw new UnauthorizedAccessException("User ID not in token.");
        return int.Parse(claim);
    }
    [HttpDelete("projects/{id:int}/interest")]
public async Task<IActionResult> WithdrawInterest(int id)
{
    try
    {
        var userId = GetCurrentUserId();
        await _service.WithdrawInterestAsync(userId, id);
        return Ok(new { message = "Interest withdrawn successfully." });
    }
    catch (KeyNotFoundException ex)
    {
        return NotFound(new { message = ex.Message });
    }
    catch (InvalidOperationException ex)
    {
        return Conflict(new { message = ex.Message });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { message = "Unexpected error.", detail = ex.Message });
    }
}
}