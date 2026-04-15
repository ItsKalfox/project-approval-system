using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PAS.API.Data;
using PAS.API.DTOs.Allocation;
using PAS.API.Models;
using System.Security.Claims;

namespace PAS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "ModuleLeader")]
public class AllocationsController : ControllerBase
{
    private readonly PASDbContext _context;

    public AllocationsController(PASDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult GetAllocations()
    {
        var allocations = _context.Matches
            .Include(m => m.Project)
            .Include(m => m.Supervisor).ThenInclude(s => s.User)
            .ToList();

        var result = allocations.Select(m => new AllocationResponseDto
        {
            Id = m.MatchId,
            SupervisorName = m.Supervisor?.User?.Name ?? "Unknown",
            StudentName = m.Project?.Title ?? "Unknown",
            ProjectName = m.Project?.Title ?? "Unknown",
            MatchDate = m.MatchDate,
            Status = "pending"
        }).ToList();

        return Ok(new { data = result });
    }

    [HttpGet("students-with-matches")]
    public IActionResult GetStudentsWithMatches()
    {
        var matched = _context.Matches
            .Include(m => m.Project)
            .Where(m => m.ProjectId > 0)
            .ToList()
            .Select(m => new
            {
                id = m.MatchId,
                name = m.Project?.Title ?? "Unknown",
                projectId = m.ProjectId,
                projectName = m.Project?.Title ?? "Unknown"
            })
            .ToList();

        return Ok(new { data = matched });
    }

    [HttpGet("available")]
    public IActionResult GetAvailableProjects()
    {
        var matchedProjectIds = _context.Matches.Select(m => m.ProjectId).ToHashSet();

        var projects = _context.Projects
            .Where(p => !matchedProjectIds.Contains(p.ProjectId))
            .Select(p => new
            {
                id = p.ProjectId,
                name = p.Title,
                status = "Available"
            })
            .ToList();

        return Ok(new { data = projects });
    }

    [HttpPost("reassign")]
    public IActionResult ReassignProject([FromBody] ReassignRequestDto request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var moduleLeaderId))
        {
            return Unauthorized(new { message = "Invalid user" });
        }

        var existingMatch = _context.Matches
            .FirstOrDefault(m => m.ProjectId == request.ProjectId);

        if (existingMatch != null)
        {
            _context.Matches.Remove(existingMatch);
        }

        var newMatch = new Match
        {
            ProjectId = request.ProjectId,
            SupervisorId = request.SupervisorId,
            MatchDate = DateTime.UtcNow
        };

        _context.Matches.Add(newMatch);
        _context.SaveChanges();

        return Ok(new { message = "Project reassigned successfully" });
    }

    [HttpPatch("{id}")]
    public IActionResult UpdateAllocationStatus(int id, [FromBody] UpdateStatusDto request)
    {
        var match = _context.Matches.Find(id);
        if (match == null)
        {
            return NotFound(new { message = "Allocation not found" });
        }

        return Ok(new { message = "Status update not available" });
    }
}

public class ReassignRequestDto
{
    public int StudentId { get; set; }
    public int ProjectId { get; set; }
    public int SupervisorId { get; set; }
}

public class UpdateStatusDto
{
    public string Status { get; set; } = string.Empty;
}