using Microsoft.EntityFrameworkCore;
using PAS.API.Data;
using PAS.API.DTOs.User;
using PAS.API.Models;

namespace PAS.API.Services;

/// <summary>
/// Developer-only service for CRUD operations on users.
/// Passwords are hashed with BCrypt before persistence.
/// </summary>
public class UserAdminService : IUserAdminService
{
    private static readonly HashSet<string> ValidRoles =
        new(StringComparer.OrdinalIgnoreCase) { "STUDENT", "SUPERVISOR", "MODULE LEADER" };

    private readonly PASDbContext _db;

    public UserAdminService(PASDbContext db)
    {
        _db = db;
    }

    // ─────────────────────────────────────────────
    // POST  /api/admin/users
    // ─────────────────────────────────────────────
    public async Task<UserResponseDto> CreateUserAsync(CreateUserDto dto)
    {
        // ── Validate role ──────────────────────────────────────────────────
        var normalizedRole = dto.Role.Trim().ToUpperInvariant();
        if (!ValidRoles.Contains(normalizedRole))
            throw new ArgumentException(
                $"Invalid role '{dto.Role}'. Must be one of: STUDENT, SUPERVISOR, MODULE LEADER.");

        // ── Validate batch for students ────────────────────────────────────
        if (normalizedRole == "STUDENT" && string.IsNullOrWhiteSpace(dto.Batch))
            throw new ArgumentException("'Batch' is required when creating a STUDENT.");

        // ── Validate required fields ───────────────────────────────────────
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ArgumentException("'Name' is required.");
        if (string.IsNullOrWhiteSpace(dto.Email))
            throw new ArgumentException("'Email' is required.");
        if (string.IsNullOrWhiteSpace(dto.Password))
            throw new ArgumentException("'Password' is required.");

        // ── Duplicate email check ──────────────────────────────────────────
        var emailExists = await _db.Users
            .AnyAsync(u => u.Email == dto.Email.Trim().ToLowerInvariant());
        if (emailExists)
            throw new InvalidOperationException(
                $"A user with email '{dto.Email}' already exists.");

        // ── Hash password ──────────────────────────────────────────────────
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password, workFactor: 12);

        // ── Build User entity (UserId is auto-incremented by DB) ───────────
        var user = new User
        {
            Name      = dto.Name.Trim(),
            Email     = dto.Email.Trim().ToLowerInvariant(),
            Password  = passwordHash,
            Role      = normalizedRole,
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(); // DB assigns UserId now

        // ── Create role-specific child row ─────────────────────────────────
        string? batch = null;

        switch (normalizedRole)
        {
            case "STUDENT":
                batch = dto.Batch!.Trim();
                _db.Students.Add(new Student { UserId = user.UserId, Batch = batch });
                break;

            case "SUPERVISOR":
                _db.Supervisors.Add(new Supervisor { UserId = user.UserId });
                break;

            case "MODULE LEADER":
                _db.ModuleLeaders.Add(new ModuleLeader { UserId = user.UserId });
                break;
        }

        await _db.SaveChangesAsync();

        return MapToResponse(user, batch);
    }

    // ─────────────────────────────────────────────
    // GET  /api/admin/users/{id}
    // ─────────────────────────────────────────────
    public async Task<UserResponseDto> GetUserAsync(int userId)
    {
        var user = await _db.Users
            .Include(u => u.Student)
            .FirstOrDefaultAsync(u => u.UserId == userId)
            ?? throw new KeyNotFoundException($"User with ID '{userId}' was not found.");

        return MapToResponse(user, user.Student?.Batch);
    }

    // ─────────────────────────────────────────────
    // PATCH  /api/admin/users/{id}
    // ─────────────────────────────────────────────
    public async Task<UserResponseDto> UpdateUserAsync(int userId, UpdateUserDto dto)
    {
        var user = await _db.Users
            .Include(u => u.Student)
            .FirstOrDefaultAsync(u => u.UserId == userId)
            ?? throw new KeyNotFoundException($"User with ID '{userId}' was not found.");

        // ── Apply partial updates ──────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(dto.Name))
            user.Name = dto.Name.Trim();

        if (!string.IsNullOrWhiteSpace(dto.Email))
        {
            var newEmail = dto.Email.Trim().ToLowerInvariant();
            var emailTaken = await _db.Users
                .AnyAsync(u => u.Email == newEmail && u.UserId != userId);
            if (emailTaken)
                throw new InvalidOperationException(
                    $"A user with email '{dto.Email}' already exists.");

            user.Email = newEmail;
        }

        if (!string.IsNullOrWhiteSpace(dto.Password))
            user.Password = BCrypt.Net.BCrypt.HashPassword(dto.Password, workFactor: 12);

        if (!string.IsNullOrWhiteSpace(dto.Batch))
        {
            if (user.Student is null)
                throw new ArgumentException(
                    "'Batch' can only be updated for users with the STUDENT role.");

            user.Student.Batch = dto.Batch.Trim();
        }

        await _db.SaveChangesAsync();

        return MapToResponse(user, user.Student?.Batch);
    }

    // ─────────────────────────────────────────────
    // DELETE  /api/admin/users/{id}
    // ─────────────────────────────────────────────
    public async Task DeleteUserAsync(int userId)
    {
        var user = await _db.Users.FindAsync(userId)
            ?? throw new KeyNotFoundException($"User with ID '{userId}' was not found.");

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
    }

    // ─────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────
    private static UserResponseDto MapToResponse(User user, string? batch) => new()
    {
        UserId    = user.UserId,
        Name      = user.Name,
        Email     = user.Email,
        Role      = user.Role,
        CreatedAt = user.CreatedAt,
        Batch     = batch
    };
}
