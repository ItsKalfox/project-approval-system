namespace PAS.API.Models;

public class Project
{
    public int ProjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Abstract { get; set; }
    public string? TechnicalStack { get; set; }

    public int? ResearchAreaId { get; set; }
    public ResearchArea? ResearchArea { get; set; }

    public int? GroupId { get; set; }
    public Group? Group { get; set; }

    public bool Interested { get; set; } = false;
}