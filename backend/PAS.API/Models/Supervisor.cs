namespace PAS.API.Models;

public class Supervisor
{
    public string UserId { get; set; } = string.Empty;

    public User User { get; set; } = null!;
}