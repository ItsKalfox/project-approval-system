namespace PAS.API.Models;

public class Student
{
    public int UserId { get; set; }
    public string Batch { get; set; } = string.Empty;

    public User User { get; set; } = null!;
}