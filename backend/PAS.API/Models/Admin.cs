namespace PAS.API.Models;

public class Admin
{
    public int UserId { get; set; }

    public User User { get; set; } = null!;
}