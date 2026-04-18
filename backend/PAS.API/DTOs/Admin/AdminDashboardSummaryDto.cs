namespace PAS.API.DTOs.Admin;

public class AdminDashboardSummaryDto
{
    public bool IsHealthy { get; set; } = true;
    public string? Message { get; set; }
    public DateTime LastUpdatedUtc { get; set; } = DateTime.UtcNow;
    public AdminAnalyticsDto SystemAnalytics { get; set; } = new();
    public List<AdminPasswordResetRequestDto> PasswordResetRequests { get; set; } = new();
}

public class AdminAnalyticsDto
{
    public int Supervisors { get; set; }
    public int Students { get; set; }
    public int IndividualProjectApprovals { get; set; }
    public int GroupProjectApprovals { get; set; }
}

public class AdminPasswordResetRequestDto
{
    public int Id { get; set; }
    public string StudentId { get; set; } = string.Empty;
    public DateTime RequestedAtUtc { get; set; }
    public string RequestedDate { get; set; } = string.Empty;
    public string ActionLabel { get; set; } = string.Empty;
    public string ActionKind { get; set; } = "primary";
    public string Status { get; set; } = string.Empty;
}