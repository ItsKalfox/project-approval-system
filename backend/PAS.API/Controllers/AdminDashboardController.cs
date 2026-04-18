using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PAS.API.Data;
using PAS.API.DTOs.Admin;

namespace PAS.API.Controllers;

[ApiController]
[Route("api/admin/dashboard")]
[Authorize(Policy = "SystemAdminOnly")]
public class AdminDashboardController : ControllerBase
{
    private readonly PASDbContext _db;

    public AdminDashboardController(PASDbContext db)
    {
        _db = db;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var users = await _db.Users.AsNoTracking().ToListAsync();
        var usersByEmail = users
            .GroupBy(u => u.Email, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        var passwordRequests = await _db.PasswordResetOtps
            .AsNoTracking()
            .OrderByDescending(o => o.CreatedAt)
            .Take(5)
            .ToListAsync();

        var summary = new AdminDashboardSummaryDto
        {
            IsHealthy = true,
            LastUpdatedUtc = DateTime.UtcNow,
            SystemAnalytics = new AdminAnalyticsDto
            {
                Supervisors = await _db.Supervisors.AsNoTracking().CountAsync(),
                Students = await _db.Students.AsNoTracking().CountAsync(),
                IndividualProjectApprovals = await _db.Projects
                    .AsNoTracking()
                    .CountAsync(p => !p.IsDeleted && p.Status == "Matched" && p.GroupId == null),
                GroupProjectApprovals = await _db.Projects
                    .AsNoTracking()
                    .CountAsync(p => !p.IsDeleted && p.Status == "Matched" && p.GroupId != null),
            },
            PasswordResetRequests = passwordRequests
                .Select(request =>
                {
                    var linkedUser = usersByEmail.TryGetValue(request.Email, out var user) ? user : null;
                    var status = request.IsUsed
                        ? "Changed"
                        : request.ExpiresAt < DateTime.UtcNow
                            ? "Expired"
                            : "Pending";

                    return new AdminPasswordResetRequestDto
                    {
                        Id = request.Id,
                        StudentId = linkedUser?.UserId.ToString() ?? request.Email,
                        RequestedAtUtc = request.CreatedAt,
                        RequestedDate = request.CreatedAt.ToString("MMMM dd, yyyy"),
                        ActionLabel = status == "Pending" ? "Send via an email" : "Not allowed",
                        ActionKind = status == "Pending" ? "primary" : "secondary",
                        Status = status,
                    };
                })
                .ToList()
        };

        return Ok(new { data = summary });
    }
}