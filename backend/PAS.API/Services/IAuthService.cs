using PAS.API.DTOs.Auth;

namespace PAS.API.Services;

public interface IAuthService
{
    Task<LoginResponseDto> LoginAsync(LoginRequestDto dto);
}
