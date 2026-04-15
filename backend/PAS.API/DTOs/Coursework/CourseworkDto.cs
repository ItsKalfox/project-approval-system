namespace PAS.API.DTOs.Coursework;

public class CreateCourseworkDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? Deadline { get; set; }
    public bool IsIndividual { get; set; } = true;
}

public class UpdateCourseworkDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? Deadline { get; set; }
    public bool IsIndividual { get; set; } = true;
    public bool IsActive { get; set; } = true;
}

public class CourseworkResponseDto
{
    public int CourseworkId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? Deadline { get; set; }
    public bool IsIndividual { get; set; }
    public bool IsActive { get; set; }
    public int ModuleLeaderId { get; set; }
    public string ModuleLeaderName { get; set; } = string.Empty;
}