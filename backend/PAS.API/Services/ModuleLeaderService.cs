using Microsoft.EntityFrameworkCore;
using PAS.API.Data;
using PAS.API.DTOs.Common;
using PAS.API.DTOs.ModuleLeader;
using PAS.API.Models;

namespace PAS.API.Services;

public class ModuleLeaderService : IModuleLeaderService
{
    private readonly PASDbContext  _db;
    private readonly IEmailService _emailService;

    public ModuleLeaderService(PASDbContext db, IEmailService emailService)
    {
        _db           = db;
        _emailService = emailService;
    }

    public async Task<PagedResultDto<ModuleLeaderResponseDto>> GetAllModuleLeadersAsync(int page, int pageSize)
    {
        page     = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.ModuleLeaders
            .Include(m => m.User)
            .OrderBy(m => m.UserId)
            .AsQueryable();

        var totalCount = await query.CountAsync();

        var moduleLeaders = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => MapToResponse(m.UserId, m.User.Name, m.User.Email, m.User.CreatedAt))
            .ToListAsync();

        return new PagedResultDto<ModuleLeaderResponseDto>
        {
            Page       = page,
            PageSize   = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
            Data       = moduleLeaders
        };
    }

    public async Task<ModuleLeaderResponseDto> GetModuleLeaderAsync(int userId)
    {
        var moduleLeader = await _db.ModuleLeaders
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.UserId == userId)
            ?? throw new KeyNotFoundException($"Module leader with ID '{userId}' was not found.");

        return MapToResponse(moduleLeader.UserId, moduleLeader.User.Name, moduleLeader.User.Email, moduleLeader.User.CreatedAt);
    }

    public async Task<ModuleLeaderResponseDto> UpdateModuleLeaderAsync(int userId, UpdateModuleLeaderDto dto)
    {
        var moduleLeader = await _db.ModuleLeaders
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.UserId == userId)
            ?? throw new KeyNotFoundException($"Module leader with ID '{userId}' was not found.");

        if (!string.IsNullOrWhiteSpace(dto.Name))
            moduleLeader.User.Name = dto.Name.Trim();

        if (!string.IsNullOrWhiteSpace(dto.Email))
        {
            var newEmail = dto.Email.Trim().ToLowerInvariant();
            var emailTaken = await _db.Users
                .AnyAsync(u => u.Email == newEmail && u.UserId != userId);
            if (emailTaken)
                throw new InvalidOperationException($"Email '{dto.Email}' is already in use.");

            moduleLeader.User.Email = newEmail;
        }

        await _db.SaveChangesAsync();

        return MapToResponse(moduleLeader.UserId, moduleLeader.User.Name, moduleLeader.User.Email, moduleLeader.User.CreatedAt);
    }

    public async Task DeleteModuleLeaderAsync(int userId)
    {
        var moduleLeader = await _db.ModuleLeaders
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.UserId == userId)
            ?? throw new KeyNotFoundException($"Module leader with ID '{userId}' was not found.");

        _db.Users.Remove(moduleLeader.User);
        await _db.SaveChangesAsync();
    }

    public async Task DeactivateModuleLeaderAsync(int userId)
    {
        throw new NotImplementedException("Deactivation is not supported for module leaders.");
    }

    public async Task ReactivateModuleLeaderAsync(int userId)
    {
        throw new NotImplementedException("Reactivation is not supported for module leaders.");
    }

    public async Task<(bool PasswordReset, bool EmailSent)> ResetModuleLeaderPasswordAsync(int userId)
    {
        var moduleLeader = await _db.ModuleLeaders
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.UserId == userId)
            ?? throw new KeyNotFoundException($"Module leader with ID '{userId}' was not found.");

        var plainPassword = GeneratePassword();
        moduleLeader.User.Password = BCrypt.Net.BCrypt.HashPassword(plainPassword, workFactor: 12);
        await _db.SaveChangesAsync();

        var emailSent = false;
        try
        {
            await _emailService.SendWelcomeEmailAsync(moduleLeader.User.Email, moduleLeader.User.Name, plainPassword);
            emailSent = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EmailService] Failed to send password reset email to {moduleLeader.User.Email}: {ex.Message}");
        }

        return (true, emailSent);
    }

    private static ModuleLeaderResponseDto MapToResponse(int userId, string name, string email, DateTime createdAt) => new()
    {
        UserId    = userId,
        Name      = name,
        Email     = email,
        CreatedAt = createdAt
    };

    private static string GeneratePassword()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 8).Select(s => s[random.Next(s.Length)]).ToArray());
    }
}