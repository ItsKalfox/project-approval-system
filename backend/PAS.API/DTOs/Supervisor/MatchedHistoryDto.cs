namespace PAS.API.DTOs.Supervisor;

public class MatchedHistoryDto
{
    public int ProjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ResearchAreaName { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string StudentEmail { get; set; } = string.Empty;
    public string StudentBatch { get; set; } = string.Empty;
    public DateTime MatchedAt { get; set; }
    public string Status { get; set; } = string.Empty;
}