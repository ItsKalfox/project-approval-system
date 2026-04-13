namespace PAS.API.DTOs.Student;

public class CreateStudentDto
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Batch { get; set; } = string.Empty;
}
