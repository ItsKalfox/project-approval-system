using PAS.API.DTOs.Common;
using PAS.API.DTOs.ModuleLeader;

namespace PAS.API.Services;

public interface IModuleLeaderService
{
    Task<PagedResultDto<ModuleLeaderResponseDto>> GetAllModuleLeadersAsync(int page, int pageSize);
    Task<ModuleLeaderResponseDto> GetModuleLeaderAsync(int userId);
    Task<ModuleLeaderResponseDto> UpdateModuleLeaderAsync(int userId, UpdateModuleLeaderDto dto);
    Task DeleteModuleLeaderAsync(int userId);
    Task DeactivateModuleLeaderAsync(int userId);
    Task ReactivateModuleLeaderAsync(int userId);
    Task<(bool PasswordReset, bool EmailSent)> ResetModuleLeaderPasswordAsync(int userId);
}