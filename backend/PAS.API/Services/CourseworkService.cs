using Microsoft.EntityFrameworkCore;
using PAS.API.Data;
using PAS.API.DTOs.Coursework;
using PAS.API.Models;

namespace PAS.API.Services;

public class CourseworkService : ICourseworkService
{
    private readonly PASDbContext _context;
    private static readonly Dictionary<int, bool> _activeStatus = new();

    public CourseworkService(PASDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<CourseworkResponseDto>> GetAllAsync()
    {
        var courseworks = await _context.Courseworks.ToListAsync();
        return courseworks.Select(c => MapToDto(c, GetActiveStatus(c.CourseworkId)));
    }

    public async Task<IEnumerable<CourseworkResponseDto>> GetActiveAsync()
    {
        var courseworks = await _context.Courseworks.ToListAsync();
        return courseworks
            .Where(c => GetActiveStatus(c.CourseworkId))
            .Select(c => MapToDto(c, true));
    }

    public async Task<CourseworkResponseDto?> GetByIdAsync(int id)
    {
        var coursework = await _context.Courseworks.FindAsync(id);
        return coursework == null ? null : MapToDto(coursework, GetActiveStatus(id));
    }

    public async Task<CourseworkResponseDto> CreateAsync(CreateCourseworkDto dto, int moduleLeaderId)
    {
        var coursework = new Coursework
        {
            Title = dto.Title,
            Description = dto.Description,
            Deadline = dto.Deadline,
            IsIndividual = dto.IsIndividual
        };

        _context.Courseworks.Add(coursework);
        await _context.SaveChangesAsync();

        _activeStatus[coursework.CourseworkId] = true;
        return MapToDto(coursework, true);
    }

    public async Task<CourseworkResponseDto?> UpdateAsync(int id, UpdateCourseworkDto dto)
    {
        var coursework = await _context.Courseworks.FindAsync(id);
        if (coursework == null) return null;

        coursework.Title = dto.Title;
        coursework.Description = dto.Description;
        coursework.Deadline = dto.Deadline;
        coursework.IsIndividual = dto.IsIndividual;

        await _context.SaveChangesAsync();
        
        if (_activeStatus.ContainsKey(id))
            _activeStatus[id] = dto.IsActive;
        
        return MapToDto(coursework, GetActiveStatus(id));
    }

    public async Task<bool> ToggleActiveAsync(int id)
    {
        var coursework = await _context.Courseworks.FindAsync(id);
        if (coursework == null) return false;

        var currentStatus = GetActiveStatus(id);
        _activeStatus[id] = !currentStatus;
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var coursework = await _context.Courseworks.FindAsync(id);
        if (coursework == null) return false;

        _context.Courseworks.Remove(coursework);
        await _context.SaveChangesAsync();
        
        _activeStatus.Remove(id);
        return true;
    }

    private bool GetActiveStatus(int id)
    {
        return _activeStatus.TryGetValue(id, out var isActive) ? isActive : true;
    }

    private static CourseworkResponseDto MapToDto(Coursework c, bool isActive)
    {
        return new CourseworkResponseDto
        {
            CourseworkId = c.CourseworkId,
            Title = c.Title,
            Description = c.Description,
            Deadline = c.Deadline,
            IsIndividual = c.IsIndividual,
            IsActive = isActive
        };
    }
}