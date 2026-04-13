using PAS.API.DTOs.Auth;

namespace PAS.API.Services;

public interface IPasswordResetService
{
    /// <summary>Generate OTP, store it, and email it to the user.</summary>
    Task SendOtpAsync(ForgotPasswordDto dto);

    /// <summary>Verify that OTP is correct and not expired. Does NOT consume the OTP.</summary>
    Task VerifyOtpAsync(VerifyOtpDto dto);

    /// <summary>Verify OTP one final time, then hash and save the new password.</summary>
    Task ResetPasswordAsync(ResetPasswordDto dto);
}
