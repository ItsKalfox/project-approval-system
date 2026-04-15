using Microsoft.EntityFrameworkCore;
using PAS.API.Data;
using PAS.API.DTOs.ResearchArea;
using PAS.API.Models;

namespace PAS.API.Services;

public class ResearchAreaService : IResearchAreaService
{
    private readonly PASDbContext _db;

    public ResearchAreaService(PASDbContext db)
    {
        _db = db;
    }

    public async Task<ResearchAreaResponseDto> CreateResearchAreaAsync(CreateResearchAreaDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ArgumentException("'Name' is required.");

        var name = dto.Name.Trim();

        var exists = await _db.ResearchAreas.AnyAsync(r => r.Name == name);
        if (exists)
            throw new InvalidOperationException($"Research area '{name}' already exists.");

        var researchArea = new ResearchArea { Name = name };
        _db.ResearchAreas.Add(researchArea);
        await _db.SaveChangesAsync();

        return MapToResponse(researchArea);
    }

    public async Task<List<ResearchAreaResponseDto>> GetAllResearchAreasAsync()
    {
        var areas = await _db.ResearchAreas
            .OrderBy(r => r.Name)
            .Select(r => MapToResponse(r))
            .ToListAsync();

        return areas;
    }

    public async Task<ResearchAreaResponseDto> GetResearchAreaAsync(int id)
    {
        var researchArea = await _db.ResearchAreas
            .FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new KeyNotFoundException($"Research area with ID '{id}' was not found.");

        return MapToResponse(researchArea);
    }

    public async Task<ResearchAreaResponseDto> UpdateResearchAreaAsync(int id, UpdateResearchAreaDto dto)
    {
        var researchArea = await _db.ResearchAreas
            .FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new KeyNotFoundException($"Research area with ID '{id}' was not found.");

        if (!string.IsNullOrWhiteSpace(dto.Name))
        {
            var newName = dto.Name.Trim();
            var taken = await _db.ResearchAreas
                .AnyAsync(r => r.Name == newName && r.Id != id);
            if (taken)
                throw new InvalidOperationException(
                    $"Research area '{newName}' already exists.");
            researchArea.Name = newName;
            await _db.SaveChangesAsync();
        }

        return MapToResponse(researchArea);
    }

    public async Task DeleteResearchAreaAsync(int id)
    {
        var researchArea = await _db.ResearchAreas
            .FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new KeyNotFoundException($"Research area with ID '{id}' was not found.");

        _db.ResearchAreas.Remove(researchArea);
        await _db.SaveChangesAsync();
    }

    private static ResearchAreaResponseDto MapToResponse(ResearchArea researchArea)
    {
        return new ResearchAreaResponseDto
        {
            Id   = researchArea.Id,
            Name = researchArea.Name
        };
    }
}