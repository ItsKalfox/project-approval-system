namespace PAS.API.DTOs.User;

public class CreateUserDto
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Accepted values: "STUDENT", "SUPERVISOR", "MODULE LEADER", "ADMIN"
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Required only when Role is "STUDENT".
    /// </summary>
    public string? Batch { get; set; }
}
