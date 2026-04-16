using Microsoft.EntityFrameworkCore;
using PAS.API.Data;
using PAS.API.DTOs.Submission;
using PAS.API.Models;

namespace PAS.API.Services;

public class SubmissionService : ISubmissionService
{
    private readonly PASDbContext _db;
    private readonly IBlobStorageService _blobService;

    public SubmissionService(PASDbContext db, IBlobStorageService blobService)
    {
        _db          = db;
        _blobService = blobService;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET  /api/submissions/submission-points
    // Returns active courseworks with open deadlines for the student
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<IEnumerable<SubmissionPointDto>> GetSubmissionPointsAsync(int studentUserId)
    {
        // Get all courseworks that have a future deadline (or no deadline = always open)
        var courseworks = await _db.Courseworks
            .Where(c => c.Deadline == null || c.Deadline > DateTime.UtcNow)
            .OrderByDescending(c => c.Deadline)
            .ToListAsync();

        // Find which courseworks this student already submitted to
        // A student owns a project through Group.LeaderId
        var studentProjectIds = await _db.Projects
            .Where(p => !p.IsDeleted
                        && p.Group != null
                        && p.Group.LeaderId == studentUserId)
            .Select(p => p.ProjectId)
            .ToListAsync();

        var submittedCourseworks = await _db.CourseworkProjects
            .Where(cp => studentProjectIds.Contains(cp.ProjectId))
            .Select(cp => new { cp.CourseworkId, cp.ProjectId })
            .ToListAsync();

        return courseworks.Select(c =>
        {
            var existing = submittedCourseworks.FirstOrDefault(sc => sc.CourseworkId == c.CourseworkId);
            return new SubmissionPointDto
            {
                CourseworkId     = c.CourseworkId,
                Title            = c.Title,
                Description      = c.Description,
                Deadline         = c.Deadline,
                IsIndividual     = c.IsIndividual,
                HasSubmitted     = existing != null,
                ExistingProjectId = existing?.ProjectId
            };
        });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET  /api/submissions/coursework/{courseworkId}
    // Returns the student's submission for a specific coursework
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<SubmissionResponseDto?> GetMySubmissionAsync(int studentUserId, int courseworkId)
    {
        var coursework = await _db.Courseworks.FindAsync(courseworkId);
        if (coursework == null)
            throw new KeyNotFoundException($"Coursework with ID '{courseworkId}' was not found.");

        // Find the student's project linked to this coursework
        var courseworkProject = await _db.CourseworkProjects
            .Include(cp => cp.Project)
                .ThenInclude(p => p.ResearchArea)
            .Include(cp => cp.Project)
                .ThenInclude(p => p.Group)
                    .ThenInclude(g => g!.Leader)
                        .ThenInclude(s => s!.User)
            .Where(cp => cp.CourseworkId == courseworkId
                         && !cp.Project.IsDeleted
                         && cp.Project.Group != null
                         && cp.Project.Group.LeaderId == studentUserId)
            .FirstOrDefaultAsync();

        if (courseworkProject == null)
            return null;

        return MapToResponse(courseworkProject.Project, coursework);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST /api/submissions/coursework/{courseworkId}
    // Creates a new submission
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<SubmissionResponseDto> CreateSubmissionAsync(
        int studentUserId, int courseworkId,
        CreateSubmissionDto dto, IFormFile file)
    {
        // ── Validate coursework exists ──────────────────────────────────────
        var coursework = await _db.Courseworks.FindAsync(courseworkId)
            ?? throw new KeyNotFoundException($"Coursework with ID '{courseworkId}' was not found.");

        // ── Check deadline ──────────────────────────────────────────────────
        if (coursework.Deadline.HasValue && coursework.Deadline.Value <= DateTime.UtcNow)
            throw new InvalidOperationException("The submission deadline has passed.");

        // ── Validate student exists ─────────────────────────────────────────
        var student = await _db.Students
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.UserId == studentUserId)
            ?? throw new KeyNotFoundException($"Student with ID '{studentUserId}' was not found.");

        // ── Check if student already submitted to this coursework ───────────
        var alreadySubmitted = await _db.CourseworkProjects
            .Include(cp => cp.Project)
                .ThenInclude(p => p.Group)
            .AnyAsync(cp => cp.CourseworkId == courseworkId
                            && !cp.Project.IsDeleted
                            && cp.Project.Group != null
                            && cp.Project.Group.LeaderId == studentUserId);

        if (alreadySubmitted)
            throw new InvalidOperationException("You have already submitted to this coursework.");

        // ── Validate research area ──────────────────────────────────────────
        var researchArea = await _db.ResearchAreas.FindAsync(dto.ResearchAreaId)
            ?? throw new KeyNotFoundException($"Research area with ID '{dto.ResearchAreaId}' was not found.");

        // ── Validate file ───────────────────────────────────────────────────
        ValidatePdfFile(file);

        // ── Create group for ownership ──────────────────────────────────────
        var group = new Group
        {
            LeaderId       = studentUserId,
            MaximumMembers = coursework.IsIndividual ? 1 : 5
        };
        _db.Groups.Add(group);
        await _db.SaveChangesAsync(); // Get GroupId

        // ── Upload file to Azure Blob ───────────────────────────────────────
        var blobPath = $"submissions/{courseworkId}/{group.GroupId}/{Guid.NewGuid()}_{file.FileName}";
        using var stream = file.OpenReadStream();
        await _blobService.UploadAsync(stream, blobPath, file.ContentType);

        // ── Create the Project record ───────────────────────────────────────
        var project = new Project
        {
            Title            = dto.Title.Trim(),
            Description      = dto.Description.Trim(),
            Abstract         = dto.Abstract.Trim(),
            ResearchAreaId   = dto.ResearchAreaId,
            GroupId          = group.GroupId,
            ProposalFileName = file.FileName,
            BlobFilePath     = blobPath,
            ContentType      = file.ContentType,
            FileSize         = file.Length,
            Status           = "Submitted",
            CreatedAt        = DateTime.UtcNow,
            SubmittedAt      = DateTime.UtcNow,
            IsDeleted        = false
        };
        _db.Projects.Add(project);
        await _db.SaveChangesAsync();

        // ── Link project to coursework ──────────────────────────────────────
        _db.CourseworkProjects.Add(new CourseworkProject
        {
            CourseworkId = courseworkId,
            ProjectId    = project.ProjectId
        });
        await _db.SaveChangesAsync();

        return MapToResponse(project, coursework, researchArea.Name, student);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PUT  /api/submissions/{projectId}
    // Updates an existing submission before the deadline
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<SubmissionResponseDto> UpdateSubmissionAsync(
        int studentUserId, int projectId,
        UpdateSubmissionDto dto, IFormFile? file)
    {
        // ── Load project with relationships ─────────────────────────────────
        var project = await GetOwnedProjectAsync(studentUserId, projectId);

        // ── Get the coursework to check deadline ────────────────────────────
        var courseworkProject = await _db.CourseworkProjects
            .Include(cp => cp.Coursework)
            .FirstOrDefaultAsync(cp => cp.ProjectId == projectId)
            ?? throw new InvalidOperationException("This project is not linked to any coursework.");

        var coursework = courseworkProject.Coursework;

        // ── Check deadline ──────────────────────────────────────────────────
        if (coursework.Deadline.HasValue && coursework.Deadline.Value <= DateTime.UtcNow)
            throw new InvalidOperationException("The submission deadline has passed. You cannot edit this submission.");

        // ── Update text fields (only if provided) ───────────────────────────
        if (!string.IsNullOrWhiteSpace(dto.Title))
            project.Title = dto.Title.Trim();

        if (!string.IsNullOrWhiteSpace(dto.Description))
            project.Description = dto.Description.Trim();

        if (!string.IsNullOrWhiteSpace(dto.Abstract))
            project.Abstract = dto.Abstract.Trim();

        if (dto.ResearchAreaId.HasValue)
        {
            var researchArea = await _db.ResearchAreas.FindAsync(dto.ResearchAreaId.Value)
                ?? throw new KeyNotFoundException($"Research area with ID '{dto.ResearchAreaId}' was not found.");
            project.ResearchAreaId = dto.ResearchAreaId.Value;
        }

        // ── Replace PDF if a new file is provided ───────────────────────────
        if (file != null)
        {
            ValidatePdfFile(file);

            // Delete old blob
            if (!string.IsNullOrEmpty(project.BlobFilePath))
                await _blobService.DeleteAsync(project.BlobFilePath);

            // Upload new blob
            var blobPath = $"submissions/{coursework.CourseworkId}/{project.GroupId}/{Guid.NewGuid()}_{file.FileName}";
            using var stream = file.OpenReadStream();
            await _blobService.UploadAsync(stream, blobPath, file.ContentType);

            project.ProposalFileName = file.FileName;
            project.BlobFilePath     = blobPath;
            project.ContentType      = file.ContentType;
            project.FileSize         = file.Length;
        }

        project.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        // Reload research area name
        await _db.Entry(project).Reference(p => p.ResearchArea).LoadAsync();

        return MapToResponse(project, coursework);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // DELETE /api/submissions/{projectId}
    // Soft-deletes the submission and removes the blob
    // ─────────────────────────────────────────────────────────────────────────
    public async Task DeleteSubmissionAsync(int studentUserId, int projectId)
    {
        var project = await GetOwnedProjectAsync(studentUserId, projectId);

        // Get the coursework to check deadline
        var courseworkProject = await _db.CourseworkProjects
            .Include(cp => cp.Coursework)
            .FirstOrDefaultAsync(cp => cp.ProjectId == projectId)
            ?? throw new InvalidOperationException("This project is not linked to any coursework.");

        var coursework = courseworkProject.Coursework;

        if (coursework.Deadline.HasValue && coursework.Deadline.Value <= DateTime.UtcNow)
            throw new InvalidOperationException("The submission deadline has passed. You cannot delete this submission.");

        // Delete blob from Azure
        if (!string.IsNullOrEmpty(project.BlobFilePath))
            await _blobService.DeleteAsync(project.BlobFilePath);

        // Soft-delete the project
        project.IsDeleted = true;
        project.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET  /api/submissions/{projectId}/file
    // Securely streams the PDF file for viewing/downloading
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<(Stream FileStream, string ContentType, string FileName)> GetSubmissionFileAsync(
        int studentUserId, int projectId)
    {
        var project = await GetOwnedProjectAsync(studentUserId, projectId);

        if (string.IsNullOrEmpty(project.BlobFilePath))
            throw new InvalidOperationException("No file has been uploaded for this submission.");

        var fileStream = await _blobService.DownloadAsync(project.BlobFilePath);

        return (
            fileStream,
            project.ContentType ?? "application/pdf",
            project.ProposalFileName ?? "proposal.pdf"
        );
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  Private helpers
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Loads and verifies that the student owns the given project (is the group leader).
    /// </summary>
    private async Task<Project> GetOwnedProjectAsync(int studentUserId, int projectId)
    {
        var project = await _db.Projects
            .Include(p => p.Group)
                .ThenInclude(g => g!.Leader)
                    .ThenInclude(s => s!.User)
            .Include(p => p.ResearchArea)
            .FirstOrDefaultAsync(p => p.ProjectId == projectId && !p.IsDeleted)
            ?? throw new KeyNotFoundException($"Submission with ID '{projectId}' was not found.");

        if (project.Group == null || project.Group.LeaderId != studentUserId)
            throw new UnauthorizedAccessException("You do not have permission to access this submission.");

        return project;
    }

    /// <summary>
    /// Validates that the uploaded file is a PDF and within size limits (max 10 MB).
    /// </summary>
    private static void ValidatePdfFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("A PDF file is required.");

        if (file.ContentType != "application/pdf")
            throw new ArgumentException("Only PDF files are allowed.");

        var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
        if (extension != ".pdf")
            throw new ArgumentException("Only PDF files (.pdf) are allowed.");

        const long maxSize = 10 * 1024 * 1024; // 10 MB
        if (file.Length > maxSize)
            throw new ArgumentException("File size exceeds the maximum limit of 10 MB.");
    }

    /// <summary>
    /// Maps a Project + Coursework to the response DTO (uses navigation properties).
    /// </summary>
    private static SubmissionResponseDto MapToResponse(Project project, Coursework coursework,
        string? researchAreaName = null, Student? student = null)
    {
        return new SubmissionResponseDto
        {
            ProjectId        = project.ProjectId,
            CourseworkId     = coursework.CourseworkId,
            CourseworkTitle  = coursework.Title,
            Title            = project.Title,
            Description      = project.Description ?? string.Empty,
            Abstract         = project.Abstract ?? string.Empty,
            ResearchAreaId   = project.ResearchAreaId ?? 0,
            ResearchAreaName = researchAreaName
                               ?? project.ResearchArea?.Name
                               ?? string.Empty,
            ProposalFileName = project.ProposalFileName,
            FileSize         = project.FileSize,
            Status           = project.Status,
            CreatedAt        = project.CreatedAt,
            UpdatedAt        = project.UpdatedAt,
            SubmittedAt      = project.SubmittedAt,
            SubmittedByUserId = project.Group?.LeaderId ?? 0,
            SubmittedByName  = student?.User.Name
                               ?? project.Group?.Leader?.User.Name
                               ?? string.Empty,
            Deadline         = coursework.Deadline,
            IsIndividual     = coursework.IsIndividual
        };
    }
}
