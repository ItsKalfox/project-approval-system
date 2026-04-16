namespace PAS.API.DTOs.Supervisor;

public class AnonymousProjectDto
{
    public int ProjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Abstract { get; set; }
    public string? TechnicalStack { get; set; }
    public string? Description { get; set; }
    public int ResearchAreaId { get; set; }
    public string ResearchAreaName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool HasProposalPdf { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public bool AlreadyExpressedInterest { get; set; }
}