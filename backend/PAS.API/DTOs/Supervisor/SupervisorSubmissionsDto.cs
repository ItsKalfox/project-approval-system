namespace PAS.API.DTOs.Supervisor;

public class SupervisorSubmissionsDto
{
    public IEnumerable<AnonymousProjectDto> MatchedProjects { get; set; } = new List<AnonymousProjectDto>();
    public IEnumerable<AnonymousProjectDto> PendingReviews  { get; set; } = new List<AnonymousProjectDto>();
}