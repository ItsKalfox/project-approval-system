using Microsoft.EntityFrameworkCore;
using PAS.API.Data;
using PAS.API.DTOs.Auth;
using PAS.API.Models;

namespace PAS.API.Services;

public class PasswordResetService : IPasswordResetService
{
    private const int OtpValidMinutes = 10;

    private readonly PASDbContext _db;
    private readonly IEmailService _emailService;

    public PasswordResetService(PASDbContext db, IEmailService emailService)
    {
        _db           = db;
        _emailService = emailService;
    }

    // ─────────────────────────────────────────────────────────────────────
    // Step 1 — POST /api/auth/forgot-password
    // ─────────────────────────────────────────────────────────────────────
    public async Task SendOtpAsync(ForgotPasswordDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email))
            throw new ArgumentException("Email is required.");

        var email = dto.Email.Trim().ToLowerInvariant();

        // Look up user — we always respond with the same message regardless
        // of whether the email exists (prevents email enumeration attacks).
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user is null) return; // silently succeed

        // Invalidate any previous unused OTPs for this email
        var existing = await _db.PasswordResetOtps
            .Where(o => o.Email == email && !o.IsUsed)
            .ToListAsync();
        _db.PasswordResetOtps.RemoveRange(existing);

        // Generate cryptographically random 6-digit OTP
        var otp = GenerateOtp();

        _db.PasswordResetOtps.Add(new PasswordResetOtp
        {
            Email     = email,
            OtpCode   = otp,
            ExpiresAt = DateTime.UtcNow.AddMinutes(OtpValidMinutes),
            IsUsed    = false,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        // Send the HTML email
        await _emailService.SendOtpEmailAsync(email, user.Name, otp);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Step 2 — POST /api/auth/verify-otp
    // ─────────────────────────────────────────────────────────────────────
    public async Task VerifyOtpAsync(VerifyOtpDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email))
            throw new ArgumentException("Email is required.");
        if (string.IsNullOrWhiteSpace(dto.Otp))
            throw new ArgumentException("OTP is required.");

        var record = await GetValidOtpRecord(dto.Email.Trim().ToLowerInvariant(), dto.Otp.Trim());

        // Don't mark as used yet — user still needs to submit the new password
        _ = record; // validated successfully
    }

    // ─────────────────────────────────────────────────────────────────────
    // Step 3 — POST /api/auth/reset-password
    // ─────────────────────────────────────────────────────────────────────
    public async Task ResetPasswordAsync(ResetPasswordDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email))
            throw new ArgumentException("Email is required.");
        if (string.IsNullOrWhiteSpace(dto.Otp))
            throw new ArgumentException("OTP is required.");
        if (string.IsNullOrWhiteSpace(dto.NewPassword))
            throw new ArgumentException("New password is required.");

        var email = dto.Email.Trim().ToLowerInvariant();

        // Re-verify the OTP
        var record = await GetValidOtpRecord(email, dto.Otp.Trim());

        // Find & update the user's password
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email)
            ?? throw new KeyNotFoundException("User not found.");

        user.Password = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword, workFactor: 12);

        // Mark OTP as consumed so it can't be reused
        record.IsUsed = true;

        await _db.SaveChangesAsync();
    }

    // ─── Helpers ──────────────────────────────────────────────────────────

    private async Task<PasswordResetOtp> GetValidOtpRecord(string email, string otp)
    {
        var record = await _db.PasswordResetOtps
            .Where(o => o.Email   == email
                     && o.OtpCode == otp
                     && !o.IsUsed)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync();

        if (record is null)
            throw new UnauthorizedAccessException("Invalid OTP.");

        if (record.ExpiresAt < DateTime.UtcNow)
            throw new UnauthorizedAccessException("OTP has expired. Please request a new one.");

        return record;
    }

    private static string GenerateOtp()
    {
        // Use a cryptographically secure random number
        var bytes = new byte[4];
        System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
        var number = Math.Abs(BitConverter.ToInt32(bytes, 0)) % 1_000_000;
        return number.ToString("D6"); // zero-padded to always 6 digits
    }
}
