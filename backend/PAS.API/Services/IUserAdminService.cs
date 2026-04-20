using PAS.API.DTOs.User;

namespace PAS.API.Services;

public interface IUserAdminService
{
    Task<UserResponseDto> CreateUserAsync(CreateUserDto dto);
    Task<UserResponseDto> GetUserAsync(int userId);
    Task<UserResponseDto> UpdateUserAsync(int userId, UpdateUserDto dto);
    Task DeleteUserAsync(int userId);
}
