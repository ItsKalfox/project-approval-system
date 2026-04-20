using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PAS.API.Data;
using PAS.API.DTOs.Auth;

namespace PAS.API.Services;

public class AuthService : IAuthService
{
    private readonly PASDbContext _db;
    private readonly IConfiguration _config;

    public AuthService(PASDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto dto)
    {
        // ── Validate input ─────────────────────────────────────────────────
        if (string.IsNullOrWhiteSpace(dto.Email))
            throw new ArgumentException("Email is required.");
        if (string.IsNullOrWhiteSpace(dto.Password))
            throw new ArgumentException("Password is required.");

        // ── Look up user by email (include Student for Batch) ──────────────
        var user = await _db.Users
            .Include(u => u.Student)
            .FirstOrDefaultAsync(u => u.Email == dto.Email.Trim().ToLowerInvariant())
            ?? throw new UnauthorizedAccessException("Invalid email or password.");

        // ── Verify BCrypt password hash ────────────────────────────────────
        var passwordValid = BCrypt.Net.BCrypt.Verify(dto.Password, user.Password);
        if (!passwordValid)
            throw new UnauthorizedAccessException("Invalid email or password.");

        // ── Build JWT claims ───────────────────────────────────────────────
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub,   user.UserId.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Name,  user.Name),
            new(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
            new(ClaimTypes.Role,               user.Role),
            // Custom claims
            new("userId", user.UserId.ToString()),
            new("role",   user.Role),
        };

        // Include batch inside the token when user is a student
        if (user.Role == "STUDENT" && user.Student is not null)
            claims.Add(new Claim("batch", user.Student.Batch));

        // ── Sign & generate token ──────────────────────────────────────────
        var jwtSettings    = _config.GetSection("JwtSettings");
        var secretKey      = jwtSettings["SecretKey"]!;
        var issuer         = jwtSettings["Issuer"]!;
        var audience       = jwtSettings["Audience"]!;
        var expiryMinutes  = int.Parse(jwtSettings["ExpiryMinutes"] ?? "60");

        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);

        var token = new JwtSecurityToken(
            issuer:             issuer,
            audience:           audience,
            claims:             claims,
            notBefore:          DateTime.UtcNow,
            expires:            expiresAt,
            signingCredentials: creds
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return new LoginResponseDto
        {
            Token     = tokenString,
            TokenType = "Bearer",
            ExpiresAt = expiresAt,
            UserId    = user.UserId,
            Name      = user.Name,
            Email     = user.Email,
            Role      = user.Role,
            Batch     = user.Student?.Batch
        };
    }
}
