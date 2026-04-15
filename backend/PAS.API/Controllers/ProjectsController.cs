using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PAS.API.Data;
using PAS.API.Models;

namespace PAS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "ModuleLeader")]
public class ProjectsController : ControllerBase
{
    private readonly PASDbContext _context;

    public ProjectsController(PASDbContext context)
    {
        _context = context;
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

    [HttpGet]
    public IActionResult GetAllProjects()
    {
        var projects = _context.Projects
            .Select(p => new
            {
                id = p.ProjectId,
                name = p.Title,
                description = p.Abstract,
                status = p.Interested ? "Available" : "Unavailable"
            })
            .ToList();

        return Ok(new { data = projects });
    }
}