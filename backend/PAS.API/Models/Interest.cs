namespace PAS.API.Models;

public class Interest
{
    public int InterestId { get; set; }

    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public string SupervisorId { get; set; } = string.Empty;
    public Supervisor Supervisor { get; set; } = null!;

    public string Status { get; set; } = "pending";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}