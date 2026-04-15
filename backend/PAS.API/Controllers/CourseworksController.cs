using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PAS.API.DTOs.Coursework;
using PAS.API.Services;

namespace PAS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CourseworksController : ControllerBase
{
    private readonly ICourseworkService _courseworkService;

    public CourseworksController(ICourseworkService courseworkService)
    {
        _courseworkService = courseworkService;
    }

    [HttpGet]
    [Authorize(Roles = "MODULE LEADER")]
    public async Task<ActionResult<IEnumerable<CourseworkResponseDto>>> GetAll()
    {
        var courseworks = await _courseworkService.GetAllAsync();
        return Ok(courseworks);
    }

    [HttpGet("active")]
    [Authorize(Roles = "MODULE LEADER,SUPERVISOR")]
    public async Task<ActionResult<IEnumerable<CourseworkResponseDto>>> GetActive()
    {
        var courseworks = await _courseworkService.GetActiveAsync();
        return Ok(courseworks);
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "MODULE LEADER,SUPERVISOR")]
    public async Task<ActionResult<CourseworkResponseDto>> GetById(int id)
    {
        var coursework = await _courseworkService.GetByIdAsync(id);
        if (coursework == null)
            return NotFound();
        return Ok(coursework);
    }

    [HttpPost]
    [Authorize(Roles = "MODULE LEADER")]
    public async Task<ActionResult<CourseworkResponseDto>> Create([FromBody] CreateCourseworkDto dto)
    {
        var userId = int.Parse(User.FindFirst("sub")?.Value ?? "0");
        var coursework = await _courseworkService.CreateAsync(dto, userId);
        return CreatedAtAction(nameof(GetById), new { id = coursework.CourseworkId }, coursework);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "MODULE LEADER")]
    public async Task<ActionResult<CourseworkResponseDto>> Update(int id, [FromBody] UpdateCourseworkDto dto)
    {
        var coursework = await _courseworkService.UpdateAsync(id, dto);
        if (coursework == null)
            return NotFound();
        return Ok(coursework);
    }

    [HttpPatch("{id}/toggle")]
    [Authorize(Roles = "MODULE LEADER")]
    public async Task<ActionResult> ToggleActive(int id)
    {
        var result = await _courseworkService.ToggleActiveAsync(id);
        if (!result)
            return NotFound();
        return Ok();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "MODULE LEADER")]
    public async Task<ActionResult> Delete(int id)
    {
        var result = await _courseworkService.DeleteAsync(id);
        if (!result)
            return NotFound();
        return NoContent();
    }
}