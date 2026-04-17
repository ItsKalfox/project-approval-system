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
[Authorize(Roles = "MODULE LEADER")]
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
        try
        {
            var matches = _context.Matches
                .Include(m => m.Supervisor).ThenInclude(s => s.User)
                .Include(m => m.Project).ThenInclude(p => p.Group)
                .ToList();

            var result = new List<object>();
            foreach (var m in matches)
            {
                string studentName = "Unknown";
                string projectName = "Unknown";
                var project = m.Project;
                
                if (project != null)
                {
                    projectName = project.Title ?? "Unknown";
                    if (project.Group?.LeaderId != null)
                    {
                        var leaderId = project.Group.LeaderId ?? 0;
                        if (leaderId > 0)
                        {
                            var user = _context.Users.FirstOrDefault(u => u.UserId == leaderId);
                            studentName = user?.Name ?? $"Student {leaderId}";
                        }
                    }
                }

                result.Add(new {
                    id = m.MatchId,
                    supervisorName = m.Supervisor?.User?.Name ?? "Unknown",
                    studentName = studentName,
                    projectName = projectName,
                    matchDate = m.MatchDate,
                    status = "pending"
                });
            }

            return Ok(new { data = result });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet("debug")]
    public IActionResult DebugMatches()
    {
        var matches = _context.Matches.ToList();
        var projects = _context.Projects.Include(p => p.Group).ToList();
        var users = _context.Users.Take(20).ToList();
        
        return Ok(new { 
            matchCount = matches.Count,
            projectCount = projects.Count,
            userCount = users.Count,
            matches = matches.Take(5).Select(m => new { m.MatchId, m.ProjectId, m.SupervisorId, m.MatchDate }),
            projects = projects.Where(p => p.Group != null).Take(5).Select(p => new { p.ProjectId, p.Title, p.GroupId, p.Group?.LeaderId })
        });
    }

    [HttpGet("comprehensive")]
    public IActionResult GetComprehensiveAllocations()
    {
        try
        {
            var matches = _context.Matches
                .Include(m => m.Supervisor).ThenInclude(s => s.User)
                .Include(m => m.Project).ThenInclude(p => p.Group)
                .ToList();

            var result = new List<object>();
            foreach (var m in matches)
            {
                string studentName = "Unknown";
                string projectName = "Unknown";
                var project = m.Project;
                
                if (project != null)
                {
                    projectName = project.Title ?? "Unknown";
                    if (project.Group?.LeaderId != null)
                    {
                        var leaderId = project.Group.LeaderId ?? 0;
                        if (leaderId > 0)
                        {
                            var user = _context.Users.FirstOrDefault(u => u.UserId == leaderId);
                            studentName = user?.Name ?? $"Student {leaderId}";
                        }
                    }
                }

                result.Add(new {
                    matchId = m.MatchId,
                    supervisorId = m.SupervisorId,
                    supervisorName = m.Supervisor?.User?.Name ?? "Unknown",
                    studentId = project?.Group?.LeaderId ?? 0,
                    studentName = studentName,
                    projectId = m.ProjectId,
                    projectName = projectName,
                    matchDate = m.MatchDate,
                    status = "pending"
                });
            }

            return Ok(new { data = result });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
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

    [HttpPost("reassign-supervisor")]
    public IActionResult ReassignSupervisor([FromBody] ReassignSupervisorRequestDto request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var moduleLeaderId))
        {
            return Unauthorized(new { message = "Invalid user" });
        }

        var student = _context.Students
            .Include(s => s.User)
            .FirstOrDefault(s => s.UserId == request.StudentId);

        if (student == null)
        {
            return NotFound(new { message = "Student not found" });
        }

        var project = request.ProjectId.HasValue
            ? _context.Projects.Find(request.ProjectId)
            : _context.Projects
                .Include(p => p.Group)
                .FirstOrDefault(p => p.Group != null && p.Group.LeaderId == request.StudentId);

        if (project == null)
        {
            return BadRequest(new { message = "Student has no project submission" });
        }

        var existingMatch = _context.Matches
            .FirstOrDefault(m => m.ProjectId == project.ProjectId);

        if (existingMatch != null)
        {
            existingMatch.SupervisorId = request.SupervisorId;
            existingMatch.MatchDate = DateTime.UtcNow;
        }
        else
        {
            var newMatch = new Match
            {
                ProjectId = project.ProjectId,
                SupervisorId = request.SupervisorId,
                MatchDate = DateTime.UtcNow
            };
            _context.Matches.Add(newMatch);
        }

        _context.SaveChanges();
        return Ok(new { message = "Supervisor reassigned successfully" });
    }

    [HttpPatch("{id}")]
    public IActionResult UpdateAllocationStatus(int id, [FromBody] UpdateStatusDto request)
    {
        var match = _context.Matches.Find(id);
        if (match == null)
        {
            return NotFound(new { message = "Allocation not found" });
        }

        return Ok(new { message = "Status tracking not enabled" });
    }
}

public class ReassignRequestDto
{
    public int StudentId { get; set; }
    public int ProjectId { get; set; }
    public int SupervisorId { get; set; }
}

public class ReassignSupervisorRequestDto
{
    public int StudentId { get; set; }
    public int? ProjectId { get; set; }
    public int SupervisorId { get; set; }
}

public class UpdateStatusDto
{
    public string Status { get; set; } = string.Empty;
}

public class ComprehensiveAllocationDto
{
    public int MatchId { get; set; }
    public int SupervisorId { get; set; }
    public string SupervisorName { get; set; } = string.Empty;
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public DateTime MatchDate { get; set; }
    public string Status { get; set; } = "pending";
}