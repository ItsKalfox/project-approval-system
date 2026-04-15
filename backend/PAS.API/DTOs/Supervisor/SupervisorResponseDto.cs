namespace PAS.API.DTOs.Supervisor;

public class SupervisorResponseDto
{
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Expertise { get; set; } = string.Empty;
    public string Institution { get; set; } = "NSBM";
    public DateTime CreatedAt { get; set; }
}