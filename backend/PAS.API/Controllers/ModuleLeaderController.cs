using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PAS.API.DTOs.ModuleLeader;
using PAS.API.Services;

namespace PAS.API.Controllers;

[ApiController]
[Route("api/module-leaders")]
[Authorize(Policy = "SystemAdminOnly")]
public class ModuleLeaderController : ControllerBase
{
    private readonly IModuleLeaderService _moduleLeaderService;

    public ModuleLeaderController(IModuleLeaderService moduleLeaderService)
    {
        _moduleLeaderService = moduleLeaderService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllModuleLeaders(
        [FromQuery] int page     = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var result = await _moduleLeaderService.GetAllModuleLeadersAsync(page, pageSize);
            return Ok(new
            {
                message = "Module leaders retrieved successfully.",
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
    public async Task<IActionResult> GetModuleLeader(int id)
    {
        try
        {
            var result = await _moduleLeaderService.GetModuleLeaderAsync(id);
            return Ok(new
            {
                message = "Module leader retrieved successfully.",
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
    public async Task<IActionResult> UpdateModuleLeader(int id, [FromBody] UpdateModuleLeaderDto dto)
    {
        try
        {
            var result = await _moduleLeaderService.UpdateModuleLeaderAsync(id, dto);
            return Ok(new
            {
                message = "Module leader updated successfully.",
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
    public async Task<IActionResult> DeleteModuleLeader(int id)
    {
        try
        {
            await _moduleLeaderService.DeleteModuleLeaderAsync(id);
            return Ok(new { message = $"Module leader '{id}' deleted successfully." });
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
    public async Task<IActionResult> DeactivateModuleLeader(int id)
    {
        try
        {
            await _moduleLeaderService.DeactivateModuleLeaderAsync(id);
            return Ok(new { message = $"Module leader '{id}' has been deactivated." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (NotImplementedException ex)
        {
            return BadRequest(new { message = ex.Message });
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
    public async Task<IActionResult> ReactivateModuleLeader(int id)
    {
        try
        {
            await _moduleLeaderService.ReactivateModuleLeaderAsync(id);
            return Ok(new { message = $"Module leader '{id}' has been reactivated." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (NotImplementedException ex)
        {
            return BadRequest(new { message = ex.Message });
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
    public async Task<IActionResult> ResetModuleLeaderPassword(int id)
    {
        try
        {
            var (passwordReset, emailSent) = await _moduleLeaderService.ResetModuleLeaderPasswordAsync(id);
            var message = emailSent
                ? $"Password for module leader '{id}' has been reset and sent to their email."
                : $"Password for module leader '{id}' has been reset, but failed to send email.";
            return Ok(new
            {
                message = message
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