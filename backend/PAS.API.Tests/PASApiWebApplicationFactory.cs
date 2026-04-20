using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PAS.API.Data;

namespace PAS.API.Tests;

/// <summary>
/// Custom WebApplicationFactory that sets up a test environment with:
/// - In-memory database for testing
/// - Test authentication scheme that bypasses JWT validation
/// </summary>
public class PASApiWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove ALL DbContext descriptors
            var descriptors = services.Where(d => 
                d.ServiceType == typeof(DbContextOptions<PASDbContext>) ||
                d.ServiceType == typeof(DbContextOptions)).ToList();
            
            foreach (var descriptor in descriptors)
                services.Remove(descriptor);

            // Add in-memory database for testing
            services.AddDbContext<PASDbContext>(options =>
            {
                options.UseInMemoryDatabase(Guid.NewGuid().ToString());
            });

            // Replace JWT authentication with a test scheme
            var authDescriptors = services.Where(d => 
                d.ServiceType == typeof(Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider)).ToList();
            foreach (var descriptor in authDescriptors)
                services.Remove(descriptor);

            services.AddAuthentication("TestScheme")
                .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>("TestScheme", null);
        });

        builder.UseEnvironment("Test");
    }
}

/// <summary>
/// Test authentication handler that simulates authenticated users with specified roles
/// </summary>
public class TestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string AuthorizationHeaderValue = "Bearer TestToken";

    public TestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : base(options, logger, encoder)
    {
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var token = Request.Headers.Authorization.FirstOrDefault();

        if (token == null || !token.StartsWith("Bearer "))
            return AuthenticateResult.Fail("Missing or invalid Authorization header");

        var roles = ExtractRolesFromToken(token.Substring("Bearer ".Length));
        
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "test-user-123"),
            new Claim(ClaimTypes.Name, "Test User")
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return await Task.FromResult(AuthenticateResult.Success(ticket));
    }

    private List<string> ExtractRolesFromToken(string token)
    {
        // Parse custom test token format: "TestToken-ROLE1-ROLE2"
        var parts = token.Split('-');
        if (parts.Length > 1 && parts[0] == "TestToken")
        {
            return parts.Skip(1).ToList();
        }

        // Default to MODULE LEADER role for backward compatibility
        return new List<string> { "MODULE LEADER" };
    }
}

/// <summary>
/// Helper class for creating HTTP clients with test authentication
/// </summary>
public static class HttpClientExtensions
{
    public static HttpClient CreateAuthenticatedClient(
        this WebApplicationFactory<Program> factory,
        params string[] roles)
    {
        var client = factory.CreateClient();
        
        var tokenValue = "TestToken";
        if (roles.Length > 0)
        {
            tokenValue = $"TestToken-{string.Join("-", roles)}";
        }

        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenValue);

        return client;
    }
}
