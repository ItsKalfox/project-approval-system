using Microsoft.EntityFrameworkCore;
using PAS.API.Data;
using PAS.API.DTOs.Common;
using PAS.API.DTOs.Student;
using PAS.API.Models;

namespace PAS.API.Services;

public class StudentService : IStudentService
{
    private readonly PASDbContext  _db;
    private readonly IEmailService _emailService;

    public StudentService(PASDbContext db, IEmailService emailService)
    {
        _db           = db;
        _emailService = emailService;
    }

    // ─────────────────────────────────────────────────────────────────────
    // POST /api/students — Create student + auto-generate password
    // ─────────────────────────────────────────────────────────────────────
    public async Task<StudentResponseDto> CreateStudentAsync(CreateStudentDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ArgumentException("'Name' is required.");
        if (string.IsNullOrWhiteSpace(dto.Email))
            throw new ArgumentException("'Email' is required.");
        if (string.IsNullOrWhiteSpace(dto.Batch))
            throw new ArgumentException("'Batch' is required.");

        var email = dto.Email.Trim().ToLowerInvariant();

        var emailTaken = await _db.Users.AnyAsync(u => u.Email == email);
        if (emailTaken)
            throw new InvalidOperationException($"Email '{dto.Email}' is already in use.");

        // Generate a secure random password and hash it
        var plainPassword = GeneratePassword();
        var passwordHash  = BCrypt.Net.BCrypt.HashPassword(plainPassword, workFactor: 12);

        var user = new User
        {
            Name      = dto.Name.Trim(),
            Email     = email,
            Password  = passwordHash,
            Role      = "STUDENT",
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(); // get the auto-incremented UserId

        _db.Students.Add(new Student { UserId = user.UserId, Batch = dto.Batch.Trim() });
        await _db.SaveChangesAsync();

        // Email the plain-text password to the student
        await _emailService.SendWelcomeEmailAsync(user.Email, user.Name, plainPassword);

        return MapToResponse(user.UserId, user.Name, user.Email,
                             user.CreatedAt, dto.Batch.Trim());
    }

    // ─────────────────────────────────────────────────────────────────────
    // GET /api/students  (paginated)
    // ─────────────────────────────────────────────────────────────────────
    public async Task<PagedResultDto<StudentResponseDto>> GetAllStudentsAsync(int page, int pageSize)
    {
        // Clamp inputs to safe ranges
        page     = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.Students
            .Include(s => s.User)
            .OrderBy(s => s.UserId)   // deterministic ordering for pagination
            .AsQueryable();

        var totalCount = await query.CountAsync();

        var students = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => MapToResponse(s.User.UserId, s.User.Name, s.User.Email,
                                       s.User.CreatedAt, s.Batch))
            .ToListAsync();

        return new PagedResultDto<StudentResponseDto>
        {
            Page       = page,
            PageSize   = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
            Data       = students
        };
    }

    // ─────────────────────────────────────────────────────────────────────
    // GET /api/students/{id}
    // ─────────────────────────────────────────────────────────────────────
    public async Task<StudentResponseDto> GetStudentAsync(int userId)
    {
        var student = await _db.Students
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.UserId == userId)
            ?? throw new KeyNotFoundException($"Student with ID '{userId}' was not found.");

        return MapToResponse(student.User.UserId, student.User.Name,
                             student.User.Email, student.User.CreatedAt, student.Batch);
    }

    // ─────────────────────────────────────────────────────────────────────
    // PATCH /api/students/{id}
    // ─────────────────────────────────────────────────────────────────────
    public async Task<StudentResponseDto> UpdateStudentAsync(int userId, UpdateStudentDto dto)
    {
        var student = await _db.Students
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.UserId == userId)
            ?? throw new KeyNotFoundException($"Student with ID '{userId}' was not found.");

        var user = student.User;

        if (!string.IsNullOrWhiteSpace(dto.Name))
            user.Name = dto.Name.Trim();

        if (!string.IsNullOrWhiteSpace(dto.Email))
        {
            var newEmail = dto.Email.Trim().ToLowerInvariant();
            var taken = await _db.Users
                .AnyAsync(u => u.Email == newEmail && u.UserId != userId);
            if (taken)
                throw new InvalidOperationException(
                    $"Email '{dto.Email}' is already in use by another user.");
            user.Email = newEmail;
        }

        if (!string.IsNullOrWhiteSpace(dto.Batch))
            student.Batch = dto.Batch.Trim();

        await _db.SaveChangesAsync();

        return MapToResponse(user.UserId, user.Name, user.Email,
                             user.CreatedAt, student.Batch);
    }

    // ─────────────────────────────────────────────────────────────────────
    // DELETE /api/students/{id}
    // ─────────────────────────────────────────────────────────────────────
    public async Task DeleteStudentAsync(int userId)
    {
        // Deleting the User cascades to the Student row automatically
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.UserId == userId && u.Role == "STUDENT")
            ?? throw new KeyNotFoundException($"Student with ID '{userId}' was not found.");

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
    }

    // ─────────────────────────────────────────────────────────────────────
    // POST /api/students/{id}/reset-password
    // ─────────────────────────────────────────────────────────────────────
    public async Task ResetStudentPasswordAsync(int userId)
    {
        var student = await _db.Students
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.UserId == userId)
            ?? throw new KeyNotFoundException($"Student with ID '{userId}' was not found.");

        var user = student.User;

        // Generate and hash new password
        var plainPassword = GeneratePassword();
        user.Password = BCrypt.Net.BCrypt.HashPassword(plainPassword, workFactor: 12);

        await _db.SaveChangesAsync();

        // Send new password to student's email
        await _emailService.SendAdminPasswordResetEmailAsync(user.Email, user.Name, plainPassword);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────
    private static StudentResponseDto MapToResponse(
        int userId, string name, string email, DateTime createdAt, string batch) => new()
    {
        UserId    = userId,
        Name      = name,
        Email     = email,
        Batch     = batch,
        CreatedAt = createdAt
    };

    /// <summary>
    /// Generates a 12-character cryptographically random password containing
    /// uppercase, lowercase, digits, and special characters.
    /// </summary>
    private static string GeneratePassword()
    {
        const string upper   = "ABCDEFGHJKLMNPQRSTUVWXYZ";   // no I/O to avoid confusion
        const string lower   = "abcdefghjkmnpqrstuvwxyz";    // no l to avoid confusion
        const string digits  = "23456789";                    // no 0/1 to avoid confusion
        const string special = "!@#$%&*";
        const string all     = upper + lower + digits + special;

        var rng      = System.Security.Cryptography.RandomNumberGenerator.Create();
        var buffer   = new byte[12];
        rng.GetBytes(buffer);

        // Guarantee at least one character from each category
        var chars = new char[12];
        chars[0] = upper  [buffer[0]  % upper.Length];
        chars[1] = lower  [buffer[1]  % lower.Length];
        chars[2] = digits [buffer[2]  % digits.Length];
        chars[3] = special[buffer[3]  % special.Length];

        for (int i = 4; i < 12; i++)
            chars[i] = all[buffer[i] % all.Length];

        // Shuffle using Fisher-Yates
        rng.GetBytes(buffer);
        for (int i = 11; i > 0; i--)
        {
            int j    = buffer[i] % (i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }

        return new string(chars);
    }
}
