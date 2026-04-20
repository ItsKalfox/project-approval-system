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
    // GET  /api/submissions/my-submissions
    // Returns all submissions made by the student
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<IEnumerable<SubmissionResponseDto>> GetStudentSubmissionsAsync(int studentUserId)
    {
        // Fetch all projects where the student is the leader
        var courseworkProjects = await _db.CourseworkProjects
            .Include(cp => cp.Coursework)
            .Include(cp => cp.Project)
                .ThenInclude(p => p.ResearchArea)
            .Include(cp => cp.Project)
                .ThenInclude(p => p.Group)
                    .ThenInclude(g => g!.Leader)
                        .ThenInclude(s => s!.User)
            .Include(cp => cp.Project)
                .ThenInclude(p => p.Match)
                    .ThenInclude(m => m!.Supervisor)
                        .ThenInclude(s => s.User)
            .Where(cp => !cp.Project.IsDeleted
                         && cp.Project.Group != null
                         && cp.Project.Group.LeaderId == studentUserId)
            .OrderByDescending(cp => cp.Project.SubmittedAt)
            .ToListAsync();

        return courseworkProjects.Select(cp => MapToResponse(cp.Project, cp.Coursework));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET  /api/submissions/submission-points
    // Returns active courseworks with open deadlines for the student
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<IEnumerable<SubmissionPointDto>> GetSubmissionPointsAsync(int studentUserId)
    {
        // Get all courseworks (no deadline filter to ensure newly created ones show up)
        var courseworks = await _db.Courseworks
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
    // GET  /api/submissions/coursework/{courseworkId}/groups
    // Returns available groups for a group submission (A-Z)
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<IEnumerable<AvailableGroupDto>> GetAvailableGroupsAsync(int courseworkId)
    {
        var coursework = await _db.Courseworks.FindAsync(courseworkId);
        if (coursework == null)
            throw new KeyNotFoundException($"Coursework with ID '{courseworkId}' was not found.");

        if (coursework.IsIndividual)
            return Enumerable.Empty<AvailableGroupDto>();

        const int maxSize = 5;
        const string labels = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const int numGroups = 26; // Always show all 26 groups A-Z

        // Get existing group members for this coursework
        var groupsWithProjects = await _db.CourseworkProjects
            .Where(cp => cp.CourseworkId == courseworkId && !cp.Project.IsDeleted)
            .Include(cp => cp.Project)
                .ThenInclude(p => p.Group)
            .ToListAsync();

        var groupMemberCount = groupsWithProjects
            .Where(cp => cp.Project.Group != null)
            .GroupBy(cp => cp.Project.Group!.GroupId)
            .ToDictionary(g => g.Key, g => g.Count());

        // Build list of groups A-Z with member counts
        var availableGroups = new List<AvailableGroupDto>();
        for (int i = 0; i < numGroups; i++)
        {
            var groupId = i + 1;
            var currentMembers = groupMemberCount.TryGetValue(groupId, out var count) ? count : 0;
            
            availableGroups.Add(new AvailableGroupDto
            {
                GroupId = groupId,
                GroupName = $"Group {labels[i]}",
                CurrentMembers = currentMembers,
                MaxMembers = maxSize
            });
        }

        return availableGroups;
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
        if (coursework.Deadline.HasValue && coursework.Deadline.Value < DateTime.UtcNow)
            throw new InvalidOperationException("The submission deadline has passed.");

        // ── Validate student exists ─────────────────────────────────────────
        var student = await _db.Students
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.UserId == studentUserId);

        // For now, create a temporary student record if not exists (migration support)
        if (student == null)
        {
            student = new Student { UserId = studentUserId };
            _db.Students.Add(student);
            await _db.SaveChangesAsync();
            student = await _db.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.UserId == studentUserId);
        }

        Group? group = null;

        // ── Group submission handling ────────────────────────────────────────
        if (!coursework.IsIndividual)
        {
            if (!dto.GroupId.HasValue || dto.GroupId.Value <= 0 || dto.GroupId.Value > 26)
                throw new InvalidOperationException("Invalid group selection.");

            group = await _db.Groups.FindAsync(dto.GroupId.Value)
                ?? throw new InvalidOperationException("Selected group does not exist.");

            var isGroupMember = group.LeaderId == studentUserId
                || await _db.CourseworkProjects.AnyAsync(cp => cp.CourseworkId == courseworkId
                                                               && !cp.Project.IsDeleted
                                                               && cp.Project.GroupId == group.GroupId
                                                               && cp.Project.Group != null
                                                               && cp.Project.Group.LeaderId == studentUserId);

            if (!isGroupMember)
                throw new UnauthorizedAccessException("You are not part of the selected group.");

            var alreadySubmittedByGroup = await _db.CourseworkProjects
                .AnyAsync(cp => cp.CourseworkId == courseworkId
                                && !cp.Project.IsDeleted
                                && cp.Project.GroupId == group.GroupId);

            if (alreadySubmittedByGroup)
                throw new InvalidOperationException("This group has already submitted to this coursework.");
        }
        else
        {
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
        }

        // ── Validate research area ──────────────────────────────────────────
        var researchArea = await _db.ResearchAreas.FindAsync(dto.ResearchAreaId)
            ?? throw new KeyNotFoundException($"Research area with ID '{dto.ResearchAreaId}' was not found.");

        // ── Validate file ───────────────────────────────────────────────────
        ValidatePdfFile(file);

        // ── Get or create group for ownership ───────────────────────────────
        if (coursework.IsIndividual)
        {
            group = await _db.Groups
                .FirstOrDefaultAsync(g => g.LeaderId == studentUserId && g.MaximumMembers == 1);

            if (group == null)
            {
                // Create individual group for the student
                var maxGroupId = await _db.Groups.MaxAsync(g => (int?)g.GroupId) ?? 0;
                group = new Group
                {
                    GroupId = maxGroupId + 1,
                    LeaderId = studentUserId,
                    MaximumMembers = 1
                };
                _db.Groups.Add(group);
                await _db.SaveChangesAsync();
            }
        }
        else
        {
            const int maxSize = 5;

            if (group == null)
                throw new InvalidOperationException("Invalid group selection.");

            var currentCount = await _db.CourseworkProjects
                .CountAsync(cp => cp.CourseworkId == courseworkId
                                  && !cp.Project.IsDeleted
                                  && cp.Project.GroupId == group.GroupId);

            if (currentCount >= maxSize)
                throw new InvalidOperationException("This group is already full.");
        }

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
            IsIndividual     = coursework.IsIndividual,

            // Map supervisor if matched
            MatchedSupervisor = project.Match != null ? new MatchedSupervisorDto
            {
                UserId = project.Match.SupervisorId,
                Name   = project.Match.Supervisor?.User?.Name ?? "Matched Supervisor",
                Email  = project.Match.Supervisor?.User?.Email ?? string.Empty
            } : null,

            GroupId = project.GroupId,
            GroupName = !coursework.IsIndividual && project.GroupId.HasValue 
                ? $"Group {GetGroupLabel(project.GroupId.Value)}" 
                : null
        };
    }

    private static string GetGroupLabel(int groupId)
    {
        const string labels = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        if (groupId <= 0) return "A";
        var index = (groupId - 1) % labels.Length;
        return labels[index].ToString();
    }
}
