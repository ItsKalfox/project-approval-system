namespace PAS.API.Models;

public class Match
{
    public int MatchId { get; set; }
    public DateTime MatchDate { get; set; }

    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public string SupervisorId { get; set; } = string.Empty;
    public Supervisor Supervisor { get; set; } = null!;
}