using Microsoft.EntityFrameworkCore;
using PAS.API.Data;
using PAS.API.DTOs.Supervisor;
using PAS.API.Models;

namespace PAS.API.Services;

public class SupervisorDashboardService : ISupervisorDashboardService
{
    private readonly PASDbContext _db;
    private readonly IBlobStorageService _blob;

    public SupervisorDashboardService(PASDbContext db, IBlobStorageService blob)
    {
        _db = db;
        _blob = blob;
    }

    public async Task<IEnumerable<AnonymousProjectDto>> GetAvailableProjectsAsync(int supervisorUserId, int courseworkId, int? researchAreaId)
{
    var interestIds = await _db.Interests
        .Where(i => i.SupervisorId == supervisorUserId)
        .Select(i => i.ProjectId)
        .ToListAsync();

    var query = _db.CourseworkProjects
        .Where(cp => cp.CourseworkId == courseworkId)
        .Include(cp => cp.Project)
            .ThenInclude(p => p.ResearchArea)
        .Where(cp => cp.Project.Status == "Submitted" && !cp.Project.IsDeleted);

    if (researchAreaId.HasValue)
        query = query.Where(cp => cp.Project.ResearchAreaId == researchAreaId.Value);

    return await query
        .OrderByDescending(cp => cp.Project.SubmittedAt)
        .Select(cp => new AnonymousProjectDto
        {
            ProjectId                = cp.Project.ProjectId,
            Title                    = cp.Project.Title,
            Abstract                 = cp.Project.Abstract,
            TechnicalStack           = cp.Project.TechnicalStack,
            Description              = cp.Project.Description,
            ResearchAreaId           = cp.Project.ResearchAreaId ?? 0,
            ResearchAreaName         = cp.Project.ResearchArea != null ? cp.Project.ResearchArea.Name : string.Empty,
            Status                   = cp.Project.Status,
            HasProposalPdf           = cp.Project.BlobFilePath != null,
            SubmittedAt              = cp.Project.SubmittedAt,
            AlreadyExpressedInterest = interestIds.Contains(cp.Project.ProjectId)
        })
        .ToListAsync();
}

    public async Task<(Stream Stream, string ContentType, string FileName)> GetProposalPdfAsync(int projectId)
    {
        var project = await _db.Projects
            .Where(p => p.ProjectId == projectId && !p.IsDeleted)
            .Select(p => new { p.BlobFilePath, p.ContentType, p.ProposalFileName })
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException($"Project {projectId} not found.");

        if (string.IsNullOrEmpty(project.BlobFilePath))
            throw new InvalidOperationException("This project has no uploaded proposal PDF.");

        var stream      = await _blob.DownloadAsync(project.BlobFilePath);
        var contentType = project.ContentType ?? "application/pdf";
        var fileName    = project.ProposalFileName ?? "proposal.pdf";

        return (stream, contentType, fileName);
    }

    public async Task ExpressInterestAsync(int supervisorUserId, int projectId)
{
    var project = await _db.Projects
        .FirstOrDefaultAsync(p => p.ProjectId == projectId && !p.IsDeleted)
        ?? throw new KeyNotFoundException($"Project {projectId} not found.");

    if (project.Status != "Submitted")
        throw new InvalidOperationException("Interest can only be expressed on submitted projects.");

    var alreadyExists = await _db.Interests
        .AnyAsync(i => i.SupervisorId == supervisorUserId && i.ProjectId == projectId);

    if (alreadyExists)
        throw new InvalidOperationException("You have already expressed interest in this project.");

    _db.Interests.Add(new Interest
    {
        SupervisorId = supervisorUserId,
        ProjectId    = projectId,
        Status       = "Matched",
        CreatedAt    = DateTime.UtcNow
    });

    project.Status = "Matched";

    await _db.SaveChangesAsync();
    
}
public async Task WithdrawInterestAsync(int supervisorUserId, int projectId)
{
    var interest = await _db.Interests
        .FirstOrDefaultAsync(i => i.SupervisorId == supervisorUserId && i.ProjectId == projectId)
        ?? throw new KeyNotFoundException("No interest record found for this project.");

    var project = await _db.Projects
        .FirstOrDefaultAsync(p => p.ProjectId == projectId && !p.IsDeleted)
        ?? throw new KeyNotFoundException($"Project {projectId} not found.");

    _db.Interests.Remove(interest);
    project.Status = "Submitted";

    await _db.SaveChangesAsync();
}

}