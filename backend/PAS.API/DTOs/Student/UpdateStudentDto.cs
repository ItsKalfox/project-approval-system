namespace PAS.API.DTOs.Student;

public class UpdateStudentDto
{
    /// <summary>All fields optional — only supplied fields are updated.</summary>
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Batch { get; set; }
}
