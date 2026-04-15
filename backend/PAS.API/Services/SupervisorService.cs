using Microsoft.EntityFrameworkCore;
using PAS.API.Data;
using PAS.API.DTOs.Common;
using PAS.API.DTOs.Supervisor;
using PAS.API.Models;

namespace PAS.API.Services;

public class SupervisorService : ISupervisorService
{
    private readonly PASDbContext  _db;
    private readonly IEmailService _emailService;

    public SupervisorService(PASDbContext db, IEmailService emailService)
    {
        _db           = db;
        _emailService = emailService;
    }

    public async Task<(SupervisorResponseDto Supervisor, bool EmailSent)> CreateSupervisorAsync(CreateSupervisorDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ArgumentException("'Name' is required.");
        if (string.IsNullOrWhiteSpace(dto.Email))
            throw new ArgumentException("'Email' is required.");
        if (string.IsNullOrWhiteSpace(dto.Expertise))
            throw new ArgumentException("'Expertise' is required.");

        var email = dto.Email.Trim().ToLowerInvariant();

        var emailTaken = await _db.Users.AnyAsync(u => u.Email == email);
        if (emailTaken)
            throw new InvalidOperationException($"Email '{dto.Email}' is already in use.");

        var plainPassword = GeneratePassword();
        var passwordHash  = BCrypt.Net.BCrypt.HashPassword(plainPassword, workFactor: 12);

        var user = new User
        {
            Name      = dto.Name.Trim(),
            Email     = email,
            Password  = passwordHash,
            Role      = "SUPERVISOR",
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        _db.Supervisors.Add(new Supervisor { UserId = user.UserId });
        await _db.SaveChangesAsync();

        var emailSent = false;
        try
        {
            await _emailService.SendWelcomeEmailAsync(user.Email, user.Name, plainPassword);
            emailSent = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EmailService] Failed to send welcome email to {user.Email}: {ex.Message}");
        }

        var response = MapToResponse(user.UserId, user.Name, user.Email, user.CreatedAt, dto.Expertise.Trim());
        return (response, emailSent);
    }

    public async Task<PagedResultDto<SupervisorResponseDto>> GetAllSupervisorsAsync(int page, int pageSize)
    {
        page     = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.Supervisors
            .Include(s => s.User)
            .OrderBy(s => s.UserId)
            .AsQueryable();

        var totalCount = await query.CountAsync();

        var supervisors = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => MapToResponse(s.UserId, s.User.Name, s.User.Email, s.User.CreatedAt, ""))
            .ToListAsync();

        return new PagedResultDto<SupervisorResponseDto>
        {
            Page       = page,
            PageSize   = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
            Data       = supervisors
        };
    }

    public async Task<SupervisorResponseDto> GetSupervisorAsync(int userId)
    {
        var supervisor = await _db.Supervisors
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.UserId == userId)
            ?? throw new KeyNotFoundException($"Supervisor with ID '{userId}' was not found.");

        return MapToResponse(supervisor.UserId, supervisor.User.Name, supervisor.User.Email,
                             supervisor.User.CreatedAt, "");
    }

    public async Task<SupervisorResponseDto> UpdateSupervisorAsync(int userId, UpdateSupervisorDto dto)
    {
        var supervisor = await _db.Supervisors
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.UserId == userId)
            ?? throw new KeyNotFoundException($"Supervisor with ID '{userId}' was not found.");

        var user = supervisor.User;

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

        await _db.SaveChangesAsync();

        return MapToResponse(supervisor.UserId, supervisor.User.Name, supervisor.User.Email,
                             supervisor.User.CreatedAt, "");
    }

    public async Task DeleteSupervisorAsync(int userId)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.UserId == userId && u.Role == "SUPERVISOR")
            ?? throw new KeyNotFoundException($"Supervisor with ID '{userId}' was not found.");

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
    }

    public async Task DeactivateSupervisorAsync(int userId)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.UserId == userId && u.Role == "SUPERVISOR")
            ?? throw new KeyNotFoundException($"Supervisor with ID '{userId}' was not found.");

        user.Role = "SUPERVISOR_INACTIVE";
        await _db.SaveChangesAsync();
    }

    public async Task ReactivateSupervisorAsync(int userId)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.UserId == userId && u.Role == "SUPERVISOR_INACTIVE")
            ?? throw new KeyNotFoundException($"Supervisor with ID '{userId}' was not found.");

        user.Role = "SUPERVISOR";
        await _db.SaveChangesAsync();
    }

    public async Task<(bool PasswordReset, bool EmailSent)> ResetSupervisorPasswordAsync(int userId)
    {
        var supervisor = await _db.Supervisors
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.UserId == userId)
            ?? throw new KeyNotFoundException($"Supervisor with ID '{userId}' was not found.");

        var user = supervisor.User;

        var plainPassword = GeneratePassword();
        user.Password = BCrypt.Net.BCrypt.HashPassword(plainPassword, workFactor: 12);

        await _db.SaveChangesAsync();

        var emailSent = false;
        try
        {
            await _emailService.SendAdminPasswordResetEmailAsync(user.Email, user.Name, plainPassword);
            emailSent = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EmailService] Failed to send password reset email to {user.Email}: {ex.Message}");
        }

        return (true, emailSent);
    }

    private static SupervisorResponseDto MapToResponse(
        int userId, string name, string email, DateTime createdAt, string expertise)
    {
        return new SupervisorResponseDto
        {
            UserId    = userId,
            Name      = name,
            Email     = email,
            Expertise = expertise,
            CreatedAt = createdAt
        };
    }

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