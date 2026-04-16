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

    public async Task<IEnumerable<AnonymousProjectDto>> GetAvailableProjectsAsync(int supervisorUserId)
    {
        var interestIds = await _db.Interests
            .Where(i => i.SupervisorId == supervisorUserId)
            .Select(i => i.ProjectId)
            .ToListAsync();

        return await _db.Projects
            .Include(p => p.ResearchArea)
            .Where(p => p.Status == "Submitted" && !p.IsDeleted)
            .OrderByDescending(p => p.SubmittedAt)
            .Select(p => new AnonymousProjectDto
            {
                ProjectId                = p.ProjectId,
                Title                    = p.Title,
                Abstract                 = p.Abstract,
                TechnicalStack           = p.TechnicalStack,
                Description              = p.Description,
                ResearchAreaId           = p.ResearchAreaId ?? 0,
                ResearchAreaName         = p.ResearchArea != null ? p.ResearchArea.Name : string.Empty,
                Status                   = p.Status,
                HasProposalPdf           = p.BlobFilePath != null,
                SubmittedAt              = p.SubmittedAt,
                AlreadyExpressedInterest = interestIds.Contains(p.ProjectId)
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