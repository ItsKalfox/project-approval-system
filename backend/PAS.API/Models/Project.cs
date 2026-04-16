namespace PAS.API.Models;

public class Project
{
    public int ProjectId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Abstract { get; set; }

    public string? TechnicalStack { get; set; }

    // NEW: optional full proposal description / body
    public string? Description { get; set; }

    // NEW: original uploaded PDF filename
    public string? ProposalFileName { get; set; }

    // NEW: Azure blob path / blob name
    public string? BlobFilePath { get; set; }

    // NEW: mime type for validation / downloads
    public string? ContentType { get; set; }

    // NEW: file size in bytes
    public long? FileSize { get; set; }

    // NEW: status workflow
    public string Status { get; set; } = "Draft";

    // NEW: timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public DateTime? SubmittedAt { get; set; }

    // NEW: soft delete
    public bool IsDeleted { get; set; } = false;

    public int? ResearchAreaId { get; set; }
    public ResearchArea? ResearchArea { get; set; }

    public int? GroupId { get; set; }
    public Group? Group { get; set; }

    public bool Interested { get; set; } = false;
}