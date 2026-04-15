namespace PAS.API.DTOs.Allocation;

public class AllocationResponseDto
{
    public int Id { get; set; }
    public string SupervisorName { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public DateTime MatchDate { get; set; }
    public string Status { get; set; } = "pending";
}