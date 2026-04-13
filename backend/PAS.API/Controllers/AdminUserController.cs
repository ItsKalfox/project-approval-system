using Microsoft.AspNetCore.Mvc;
using PAS.API.DTOs.User;
using PAS.API.Services;

namespace PAS.API.Controllers;

/// <summary>
/// Developer-only admin endpoints for user management.
/// Not exposed to the web application — for internal/dev use only.
/// </summary>
[ApiController]
[Route("api/admin/users")]
public class AdminUserController : ControllerBase
{
    private readonly IUserAdminService _userService;

    public AdminUserController(IUserAdminService userService)
    {
        _userService = userService;
    }

    // ─────────────────────────────────────────────────────────────────
    // POST /api/admin/users
    // Create a new user (STUDENT | SUPERVISOR | MODULE LEADER)
    // ─────────────────────────────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
    {
        try
        {
            var result = await _userService.CreateUserAsync(dto);
            return StatusCode(StatusCodes.Status201Created, new
            {
                message = "User created successfully.",
                data    = result
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            // Duplicate email → 409 Conflict
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

    // ─────────────────────────────────────────────────────────────────
    // GET /api/admin/users/{id}
    // Retrieve details of a specific user
    // ─────────────────────────────────────────────────────────────────
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetUser(int id)
    {
        try
        {
            var result = await _userService.GetUserAsync(id);
            return Ok(new
            {
                message = "User retrieved successfully.",
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

    // ─────────────────────────────────────────────────────────────────
    // PATCH /api/admin/users/{id}
    // Partially update a user's details
    // ─────────────────────────────────────────────────────────────────
    [HttpPatch("{id:int}")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserDto dto)
    {
        try
        {
            var result = await _userService.UpdateUserAsync(id, dto);
            return Ok(new
            {
                message = "User updated successfully.",
                data    = result
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
            // Duplicate email → 409 Conflict
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

    // ─────────────────────────────────────────────────────────────────
    // DELETE /api/admin/users/{id}
    // Permanently delete a user and all linked records (cascade)
    // ─────────────────────────────────────────────────────────────────
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        try
        {
            await _userService.DeleteUserAsync(id);
            return Ok(new { message = $"User '{id}' deleted successfully." });
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
