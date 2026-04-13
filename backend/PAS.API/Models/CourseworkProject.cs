namespace PAS.API.Models;

public class CourseworkProject
{
    public int CourseworkId { get; set; }
    public Coursework Coursework { get; set; } = null!;

    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;
}