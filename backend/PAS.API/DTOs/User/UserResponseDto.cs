namespace PAS.API.DTOs.User;

public class UserResponseDto
{
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    /// <summary>Populated only when Role is "STUDENT".</summary>
    public string? Batch { get; set; }
}
