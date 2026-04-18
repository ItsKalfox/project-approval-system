using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PAS.API.DTOs.ResearchArea;
using PAS.API.Services;

namespace PAS.API.Controllers;

[ApiController]
[Route("api/research-areas")]
[Authorize(Policy = "ModuleLeaderOrAdmin")]
public class ResearchAreaController : ControllerBase
{
    private readonly IResearchAreaService _researchAreaService;

    public ResearchAreaController(IResearchAreaService researchAreaService)
    {
        _researchAreaService = researchAreaService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateResearchArea([FromBody] CreateResearchAreaDto dto)
    {
        try
        {
            var result = await _researchAreaService.CreateResearchAreaAsync(dto);
            return StatusCode(StatusCodes.Status201Created, new
            {
                message = "Research area created successfully.",
                data    = result
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
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

    [HttpGet]
    public async Task<IActionResult> GetAllResearchAreas()
    {
        try
        {
            var result = await _researchAreaService.GetAllResearchAreasAsync();
            return Ok(new
            {
                message = "Research areas retrieved successfully.",
                data    = result
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

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetResearchArea(int id)
    {
        try
        {
            var result = await _researchAreaService.GetResearchAreaAsync(id);
            return Ok(new
            {
                message = "Research area retrieved successfully.",
                data    = result
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
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

    [HttpPatch("{id:int}")]
    public async Task<IActionResult> UpdateResearchArea(int id, [FromBody] UpdateResearchAreaDto dto)
    {
        try
        {
            var result = await _researchAreaService.UpdateResearchAreaAsync(id, dto);
            return Ok(new
            {
                message = "Research area updated successfully.",
                data    = result
            });
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
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "An unexpected error occurs.",
                detail  = ex.Message
            });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteResearchArea(int id)
    {
        try
        {
            await _researchAreaService.DeleteResearchAreaAsync(id);
            return Ok(new { message = $"Research area '{id}' deleted successfully." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
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