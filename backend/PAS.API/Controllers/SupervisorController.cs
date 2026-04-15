using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PAS.API.DTOs.Supervisor;
using PAS.API.Services;

namespace PAS.API.Controllers;

[ApiController]
[Route("api/supervisors")]
[Authorize(Policy = "ModuleLeaderOnly")]
public class SupervisorController : ControllerBase
{
    private readonly ISupervisorService _supervisorService;

    public SupervisorController(ISupervisorService supervisorService)
    {
        _supervisorService = supervisorService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateSupervisor([FromBody] CreateSupervisorDto dto)
    {
        try
        {
            var result = await _supervisorService.CreateSupervisorAsync(dto);
            return StatusCode(StatusCodes.Status201Created, new
            {
                message = "Supervisor created successfully. Login credentials have been sent to their email.",
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
    public async Task<IActionResult> GetAllSupervisors(
        [FromQuery] int page     = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var result = await _supervisorService.GetAllSupervisorsAsync(page, pageSize);
            return Ok(new
            {
                message = "Supervisors retrieved successfully.",
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
    public async Task<IActionResult> GetSupervisor(int id)
    {
        try
        {
            var result = await _supervisorService.GetSupervisorAsync(id);
            return Ok(new
            {
                message = "Supervisor retrieved successfully.",
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
    public async Task<IActionResult> UpdateSupervisor(int id, [FromBody] UpdateSupervisorDto dto)
    {
        try
        {
            var result = await _supervisorService.UpdateSupervisorAsync(id, dto);
            return Ok(new
            {
                message = "Supervisor updated successfully.",
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
                message = "An unexpected error occurred.",
                detail  = ex.Message
            });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteSupervisor(int id)
    {
        try
        {
            await _supervisorService.DeleteSupervisorAsync(id);
            return Ok(new { message = $"Supervisor '{id}' deleted successfully." });
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

    [HttpPost("{id:int}/deactivate")]
    public async Task<IActionResult> DeactivateSupervisor(int id)
    {
        try
        {
            await _supervisorService.DeactivateSupervisorAsync(id);
            return Ok(new { message = $"Supervisor '{id}' has been deactivated." });
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

    [HttpPost("{id:int}/reactivate")]
    public async Task<IActionResult> ReactivateSupervisor(int id)
    {
        try
        {
            await _supervisorService.ReactivateSupervisorAsync(id);
            return Ok(new { message = $"Supervisor '{id}' has been reactivated." });
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

    [HttpPost("{id:int}/reset-password")]
    public async Task<IActionResult> ResetSupervisorPassword(int id)
    {
        try
        {
            await _supervisorService.ResetSupervisorPasswordAsync(id);
            return Ok(new
            {
                message = $"Password for supervisor '{id}' has been reset and sent to their email."
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
}