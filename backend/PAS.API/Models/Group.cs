namespace PAS.API.Models;

public class Group
{
    public int GroupId { get; set; }
    public int? LeaderId { get; set; }
    public int MaximumMembers { get; set; } = 5;

    public Student? Leader { get; set; }
    public ICollection<Project> Projects { get; set; } = new List<Project>();
}