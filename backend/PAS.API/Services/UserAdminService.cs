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
        new(StringComparer.OrdinalIgnoreCase) { "STUDENT", "SUPERVISOR", "MODULE LEADER", "ADMIN" };

    private readonly PASDbContext _db;
    private readonly IEmailService _emailService;

    public UserAdminService(PASDbContext db, IEmailService emailService)
    {
        _db = db;
        _emailService = emailService;
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
                $"Invalid role '{dto.Role}'. Must be one of: STUDENT, SUPERVISOR, MODULE LEADER, ADMIN.");

        // ── Validate batch for students ────────────────────────────────────
        if (normalizedRole == "STUDENT" && string.IsNullOrWhiteSpace(dto.Batch))
            throw new ArgumentException("'Batch' is required when creating a STUDENT.");

        // ── Validate required fields ───────────────────────────────────────
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ArgumentException("'Name' is required.");
        if (string.IsNullOrWhiteSpace(dto.Email))
            throw new ArgumentException("'Email' is required.");

        // ── Auto-generate password for MODULE LEADER ───────────────────────
        string plainPassword;
        if (normalizedRole == "MODULE LEADER" && string.IsNullOrWhiteSpace(dto.Password))
        {
            plainPassword = GeneratePassword();
        }
        else
        {
            if (string.IsNullOrWhiteSpace(dto.Password))
                throw new ArgumentException("'Password' is required.");
            plainPassword = dto.Password;
        }

        // ── Duplicate email check ──────────────────────────────────────────
        var emailExists = await _db.Users
            .AnyAsync(u => u.Email == dto.Email.Trim().ToLowerInvariant());
        if (emailExists)
            throw new InvalidOperationException(
                $"A user with email '{dto.Email}' already exists.");

        // ── Hash password ──────────────────────────────────────────────────
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(plainPassword, workFactor: 12);

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

            case "ADMIN":
                _db.Admins.Add(new Admin { UserId = user.UserId });
                break;
        }

        await _db.SaveChangesAsync();

        // ── Send welcome email for MODULE LEADER ───────────────────────────
        if (normalizedRole == "MODULE LEADER")
        {
            try
            {
                await _emailService.SendWelcomeEmailAsync(user.Email, user.Name, plainPassword);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EmailService] Failed to send welcome email to {user.Email}: {ex.Message}");
                // Don't throw - user is already created, just log the error
            }
        }

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

    private static string GeneratePassword()
    {
        const string upper   = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lower   = "abcdefghjkmnpqrstuvwxyz";
        const string digits  = "23456789";
        const string special = "!@#$%&*";
        const string all     = upper + lower + digits + special;

        var rng      = System.Security.Cryptography.RandomNumberGenerator.Create();
        var buffer   = new byte[12];
        rng.GetBytes(buffer);

        var chars = new char[12];
        chars[0] = upper  [buffer[0]  % upper.Length];
        chars[1] = lower  [buffer[1]  % lower.Length];
        chars[2] = digits [buffer[2]  % digits.Length];
        chars[3] = special[buffer[3]  % special.Length];

        for (int i = 4; i < 12; i++)
            chars[i] = all[buffer[i] % all.Length];

        rng.GetBytes(buffer);
        for (int i = 11; i > 0; i--)
        {
            int j    = buffer[i] % (i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }

        return new string(chars);
    }
}
