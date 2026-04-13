namespace PAS.API.Models;

public class ModuleLeader
{
    public int UserId { get; set; }

    public User User { get; set; } = null!;
}