namespace PAS.API.Models;

public class Supervisor
{
    public int UserId { get; set; }

    public User User { get; set; } = null!;
}