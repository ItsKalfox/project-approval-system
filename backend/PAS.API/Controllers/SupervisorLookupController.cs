using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PAS.API.Data;

namespace PAS.API.Controllers;

[ApiController]
[Route("api/supervisor/lookup")]
[Authorize(Policy = "SupervisorOnly")]
public class SupervisorLookupController : ControllerBase
{
    private readonly PASDbContext _db;

    public SupervisorLookupController(PASDbContext db)
    {
        _db = db;
    }

    [HttpGet("courseworks")]
    public async Task<IActionResult> GetCourseworks()
    {
        try
        {
            var courseworks = await _db.Courseworks
                .OrderBy(c => c.Title)
                .Select(c => new
                {
                    c.CourseworkId,
                    c.Title,
                    c.Description,
                    c.Deadline,
                    c.IsIndividual
                })
                .ToListAsync();

            return Ok(new { message = "Courseworks retrieved.", data = courseworks });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Unexpected error.", detail = ex.Message });
        }
    }

    [HttpGet("research-areas")]
    public async Task<IActionResult> GetResearchAreas()
    {
        try
        {
            var areas = await _db.ResearchAreas
                .OrderBy(r => r.Name)
                .Select(r => new
                {
                    r.Id,
                    r.Name
                })
                .ToListAsync();

            return Ok(new { message = "Research areas retrieved.", data = areas });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Unexpected error.", detail = ex.Message });
        }
    }
}