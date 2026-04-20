using Microsoft.AspNetCore.Mvc;
using PAS.API.DTOs.Auth;
using PAS.API.Services;

namespace PAS.API.Controllers;

[ApiController]
[Route("api/auth")]
public class PasswordResetController : ControllerBase
{
    private readonly IPasswordResetService _resetService;

    public PasswordResetController(IPasswordResetService resetService)
    {
        _resetService = resetService;
    }

    // ─────────────────────────────────────────────────────────────────
    // STEP 1 — POST /api/auth/forgot-password
    // Send OTP to email
    // ─────────────────────────────────────────────────────────────────
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
    {
        try
        {
            await _resetService.SendOtpAsync(dto);

            // Always return same message to prevent email enumeration
            return Ok(new
            {
                message = "If that email is registered, an OTP has been sent to it."
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "Failed to send OTP.",
                detail  = ex.Message
            });
        }
    }

    // ─────────────────────────────────────────────────────────────────
    // STEP 2 — POST /api/auth/verify-otp
    // Confirm the OTP before allowing the password change screen
    // ─────────────────────────────────────────────────────────────────
    [HttpPost("verify-otp")]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpDto dto)
    {
        try
        {
            await _resetService.VerifyOtpAsync(dto);
            return Ok(new
            {
                message = "OTP verified successfully. You may now reset your password."
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
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

    // ─────────────────────────────────────────────────────────────────
    // STEP 3 — POST /api/auth/reset-password
    // Verify OTP once more and apply the new password
    // ─────────────────────────────────────────────────────────────────
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        try
        {
            await _resetService.ResetPasswordAsync(dto);
            return Ok(new { message = "Password reset successfully. You can now log in." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
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
