using PAS.API.DTOs.Common;
using PAS.API.DTOs.Supervisor;

namespace PAS.API.Services;

public interface ISupervisorService
{
    Task<SupervisorResponseDto> CreateSupervisorAsync(CreateSupervisorDto dto);
    Task<PagedResultDto<SupervisorResponseDto>> GetAllSupervisorsAsync(int page, int pageSize);
    Task<SupervisorResponseDto> GetSupervisorAsync(int userId);
    Task<SupervisorResponseDto> UpdateSupervisorAsync(int userId, UpdateSupervisorDto dto);
    Task DeleteSupervisorAsync(int userId);
    Task DeactivateSupervisorAsync(int userId);
    Task ReactivateSupervisorAsync(int userId);
    Task ResetSupervisorPasswordAsync(int userId);
}