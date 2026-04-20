namespace PAS.API.DTOs.Submission;

// ── Request DTOs ────────────────────────────────────────────────────────────

public class CreateSubmissionDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Abstract { get; set; } = string.Empty;
    public int ResearchAreaId { get; set; }
    public int? GroupId { get; set; }
}

public class UpdateSubmissionDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Abstract { get; set; }
    public int? ResearchAreaId { get; set; }
}

// ── Response DTOs ───────────────────────────────────────────────────────────

public class SubmissionResponseDto
{
    public int ProjectId { get; set; }
    public int CourseworkId { get; set; }
    public string CourseworkTitle { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Abstract { get; set; } = string.Empty;
    public int ResearchAreaId { get; set; }
    public string ResearchAreaName { get; set; } = string.Empty;
    public string? ProposalFileName { get; set; }
    public long? FileSize { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public int SubmittedByUserId { get; set; }
    public string SubmittedByName { get; set; } = string.Empty;
    public DateTime? Deadline { get; set; }
    public bool IsIndividual { get; set; }
    public int? GroupId { get; set; }
    public string? GroupName { get; set; }

    // Matching details
    public MatchedSupervisorDto? MatchedSupervisor { get; set; }
}

public class MatchedSupervisorDto
{
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class SubmissionPointDto
{
    public int CourseworkId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? Deadline { get; set; }
    public bool IsIndividual { get; set; }
    public bool HasSubmitted { get; set; }
    public int? ExistingProjectId { get; set; }
}

public class AvailableGroupDto
{
    public int GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public int CurrentMembers { get; set; }
    public int MaxMembers { get; set; }
}
