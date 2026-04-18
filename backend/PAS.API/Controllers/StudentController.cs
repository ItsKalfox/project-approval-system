using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PAS.API.DTOs.Student;
using PAS.API.Services;

namespace PAS.API.Controllers;

/// <summary>
/// Student record management — accessible only by MODULE LEADER or ADMIN role.
/// All endpoints require a valid Bearer JWT token.
/// </summary>
[ApiController]
[Route("api/students")]
[Authorize(Policy = "ModuleLeaderOrAdmin")]
public class StudentController : ControllerBase
{
    private readonly IStudentService _studentService;

    public StudentController(IStudentService studentService)
    {
        _studentService = studentService;
    }

    // ───────────────────────────────────────────────────────────────
    // POST /api/students
    // Create a student record — password auto-generated and emailed
    // ───────────────────────────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> CreateStudent([FromBody] CreateStudentDto dto)
    {
        try
        {
            var (student, emailSent) = await _studentService.CreateStudentAsync(dto);
            var message = emailSent
                ? "Student created successfully. Login credentials have been sent to their email."
                : "Student created successfully, but failed to send credentials email.";
            return StatusCode(StatusCodes.Status201Created, new
            {
                message = message,
                data    = student
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

    // ─────────────────────────────────────────────────────────────────
    // GET /api/students?page=1&pageSize=10
    // List all students with pagination
    // ─────────────────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetAllStudents(
        [FromQuery] int page     = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var result = await _studentService.GetAllStudentsAsync(page, pageSize);
            return Ok(new
            {
                message = "Students retrieved successfully.",
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

    // ─────────────────────────────────────────────────────────────────
    // GET /api/students/{id}
    // Get a single student by userId
    // ─────────────────────────────────────────────────────────────────
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetStudent(int id)
    {
        try
        {
            var result = await _studentService.GetStudentAsync(id);
            return Ok(new
            {
                message = "Student retrieved successfully.",
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
    // PATCH /api/students/{id}
    // Partially update a student's record
    // ─────────────────────────────────────────────────────────────────
    [HttpPatch("{id:int}")]
    public async Task<IActionResult> UpdateStudent(int id, [FromBody] UpdateStudentDto dto)
    {
        try
        {
            var result = await _studentService.UpdateStudentAsync(id, dto);
            return Ok(new
            {
                message = "Student updated successfully.",
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

    // ─────────────────────────────────────────────────────────────────
    // DELETE /api/students/{id}
    // Delete a student and their user account (cascade)
    // ─────────────────────────────────────────────────────────────────
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteStudent(int id)
    {
        try
        {
            await _studentService.DeleteStudentAsync(id);
            return Ok(new { message = $"Student '{id}' deleted successfully." });
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
    // POST /api/students/{id}/reset-password
    // Generate a new random password and email it to the student
    // ─────────────────────────────────────────────────────────────────
    [HttpPost("{id:int}/reset-password")]
    public async Task<IActionResult> ResetStudentPassword(int id)
    {
        try
        {
            var (passwordReset, emailSent) = await _studentService.ResetStudentPasswordAsync(id);
            var message = emailSent
                ? $"Password for student '{id}' has been reset and sent to their email."
                : $"Password for student '{id}' has been reset, but failed to send email.";
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
