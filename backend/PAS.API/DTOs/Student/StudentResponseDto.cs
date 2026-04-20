namespace PAS.API.DTOs.Student;

public class StudentResponseDto
{
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Batch { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
