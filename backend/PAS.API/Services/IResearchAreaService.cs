using PAS.API.DTOs.ResearchArea;

namespace PAS.API.Services;

public interface IResearchAreaService
{
    Task<ResearchAreaResponseDto> CreateResearchAreaAsync(CreateResearchAreaDto dto);
    Task<List<ResearchAreaResponseDto>> GetAllResearchAreasAsync();
    Task<ResearchAreaResponseDto> GetResearchAreaAsync(int id);
    Task<ResearchAreaResponseDto> UpdateResearchAreaAsync(int id, UpdateResearchAreaDto dto);
    Task DeleteResearchAreaAsync(int id);
}