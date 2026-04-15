using PAS.API.DTOs.Coursework;

namespace PAS.API.Services;

public interface ICourseworkService
{
    Task<IEnumerable<CourseworkResponseDto>> GetAllAsync();
    Task<IEnumerable<CourseworkResponseDto>> GetActiveAsync();
    Task<CourseworkResponseDto?> GetByIdAsync(int id);
    Task<CourseworkResponseDto> CreateAsync(CreateCourseworkDto dto, int moduleLeaderId);
    Task<CourseworkResponseDto?> UpdateAsync(int id, UpdateCourseworkDto dto);
    Task<bool> ToggleActiveAsync(int id);
    Task<bool> DeleteAsync(int id);
}