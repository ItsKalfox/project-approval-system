namespace PAS.API.Models;

public class Match
{
    public int MatchId { get; set; }
    public DateTime MatchDate { get; set; }

    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public int SupervisorId { get; set; }
    public Supervisor Supervisor { get; set; } = null!;
}