using Microsoft.AspNetCore.Mvc;
using PAS.API.DTOs.Auth;
using PAS.API.Services;

namespace PAS.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    // ─────────────────────────────────────────────────────────────────
    // POST /api/auth/login
    // Authenticate with email + password → returns JWT + user details
    // ─────────────────────────────────────────────────────────────────
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
    {
        try
        {
            var result = await _authService.LoginAsync(dto);
            return Ok(new
            {
                message = "Login successful.",
                data    = result
            });
        }
        catch (ArgumentException ex)
        {
            // Missing email/password field
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            // Wrong credentials — intentionally vague message in prod
            return Unauthorized(new { message = ex.Message });
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
