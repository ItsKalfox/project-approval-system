namespace PAS.API.DTOs.Supervisor;

public class RevealedProjectDto : AnonymousProjectDto
{
    public string StudentName { get; set; } = string.Empty;
    public string StudentEmail { get; set; } = string.Empty;
    public string StudentBatch { get; set; } = string.Empty;
}
