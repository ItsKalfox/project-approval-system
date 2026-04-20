using PAS.API.DTOs.Supervisor;

namespace PAS.API.Services;

public interface ISupervisorDashboardService
{
    Task<IEnumerable<AnonymousProjectDto>> GetAvailableProjectsAsync(int supervisorUserId, int courseworkId, int? researchAreaId);
    Task<(Stream Stream, string ContentType, string FileName)> GetProposalPdfAsync(int projectId);
    Task ExpressInterestAsync(int supervisorUserId, int projectId);
    Task WithdrawInterestAsync(int supervisorUserId, int projectId);
    Task<SupervisorSubmissionsDto> GetSubmissionsAsync(int supervisorUserId, List<int>? researchAreaIds);
    Task<IEnumerable<RevealedProjectDto>> GetMatchedProjectsWithStudentInfoAsync(int supervisorUserId);
    Task<IEnumerable<MatchedHistoryDto>> GetMatchHistoryAsync(int supervisorUserId);
}