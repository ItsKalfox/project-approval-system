namespace PAS.API.Models;

public class Coursework
{
    public int CourseworkId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? Deadline { get; set; }
    public bool IsIndividual { get; set; } = true;
}