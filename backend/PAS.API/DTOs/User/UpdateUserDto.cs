namespace PAS.API.DTOs.User;

public class UpdateUserDto
{
    /// <summary>All fields are optional — only provided fields will be updated.</summary>
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }

    /// <summary>Only applicable when the user is a STUDENT.</summary>
    public string? Batch { get; set; }
}
