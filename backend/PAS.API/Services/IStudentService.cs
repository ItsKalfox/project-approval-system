using PAS.API.DTOs.Common;
using PAS.API.DTOs.Student;

namespace PAS.API.Services;

public interface IStudentService
{
    Task<StudentResponseDto> CreateStudentAsync(CreateStudentDto dto);
    Task<PagedResultDto<StudentResponseDto>> GetAllStudentsAsync(int page, int pageSize);
    Task<StudentResponseDto> GetStudentAsync(int userId);
    Task<StudentResponseDto> UpdateStudentAsync(int userId, UpdateStudentDto dto);
    Task DeleteStudentAsync(int userId);
    Task ResetStudentPasswordAsync(int userId);
}
