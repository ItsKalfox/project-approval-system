using PAS.API.DTOs.Supervisor;

namespace PAS.API.Services;

public interface ISupervisorDashboardService
{
    Task<IEnumerable<AnonymousProjectDto>> GetAvailableProjectsAsync(int supervisorUserId);
    Task<(Stream Stream, string ContentType, string FileName)> GetProposalPdfAsync(int projectId);
    Task ExpressInterestAsync(int supervisorUserId, int projectId);
    Task WithdrawInterestAsync(int supervisorUserId, int projectId);
}