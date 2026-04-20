using Moq;
using Xunit;
using PAS.API.Data;
using PAS.API.DTOs.Supervisor;
using PAS.API.Models;
using PAS.API.Services;
using Microsoft.EntityFrameworkCore;
using MatchModel = PAS.API.Models.Match;

namespace PAS.API.Tests;

public class SupervisorDashboardServiceTests : IAsyncLifetime
{
    private readonly DbContextOptions<PASDbContext> _dbOptions;
    private Mock<IBlobStorageService> _mockBlobStorage = null!;

    public SupervisorDashboardServiceTests()
    {
        _dbOptions = new DbContextOptionsBuilder<PASDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => Task.CompletedTask;

    private SupervisorDashboardService CreateService(PASDbContext context)
    {
        _mockBlobStorage = new Mock<IBlobStorageService>();
        return new SupervisorDashboardService(context, _mockBlobStorage.Object);
    }

    private async Task<(User supervisorUser, Supervisor sup)> CreateSupervisorAsync(PASDbContext context, string email, string name)
    {
        var user = new User
        {
            Email = email,
            Name = name,
            Password = BCrypt.Net.BCrypt.HashPassword("password", 12),
            Role = "SUPERVISOR",
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var sup = new Supervisor { UserId = user.UserId };
        context.Supervisors.Add(sup);
        await context.SaveChangesAsync();

        return (user, sup);
    }

    private async Task<(User studentUser, Student student)> CreateStudentWithGroupAsync(PASDbContext context)
    {
        var user = new User
        {
            Email = "student@example.com",
            Name = "Test Student",
            Password = BCrypt.Net.BCrypt.HashPassword("password", 12),
            Role = "STUDENT",
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var student = new Student { UserId = user.UserId, Batch = "2024" };
        context.Students.Add(student);
        await context.SaveChangesAsync();

        var group = new Group { LeaderId = user.UserId };
        context.Groups.Add(group);
        await context.SaveChangesAsync();

        return (user, student);
    }

    private async Task<Project> CreateProjectAsync(PASDbContext context, int? groupId, int? researchAreaId = null, string status = "Submitted")
    {
        var project = new Project
        {
            Title = "Test Project",
            Abstract = "Abstract",
            Description = "Description",
            TechnicalStack = "C#",
            Status = status,
            IsDeleted = false,
            SubmittedAt = DateTime.UtcNow,
            GroupId = groupId,
            ResearchAreaId = researchAreaId
        };
        context.Projects.Add(project);
        await context.SaveChangesAsync();
        return project;
    }

    private async Task<Coursework> CreateCourseworkAsync(PASDbContext context)
    {
        var cw = new Coursework
        {
            Title = "Test Coursework",
            Description = "Description",
            Deadline = DateTime.UtcNow.AddDays(7),
            IsIndividual = true
        };
        context.Courseworks.Add(cw);
        await context.SaveChangesAsync();
        return cw;
    }

    #region GetAvailableProjectsAsync Tests

    [Fact]
    public async Task GetAvailableProjectsAsync_WithNoProjects_ReturnsEmptyList()
    {
        using var context = new PASDbContext(_dbOptions);
        var (supervisor, _) = await CreateSupervisorAsync(context, "sup@example.com", "Supervisor");
        var cw = await CreateCourseworkAsync(context);

        var service = CreateService(context);

        var result = await service.GetAvailableProjectsAsync(supervisor.UserId, cw.CourseworkId, null);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAvailableProjectsAsync_WithProjects_ReturnsProjects()
    {
        using var context = new PASDbContext(_dbOptions);
        var (supervisor, _) = await CreateSupervisorAsync(context, "sup@example.com", "Supervisor");
        var cw = await CreateCourseworkAsync(context);
        var project = await CreateProjectAsync(context, null);

        context.CourseworkProjects.Add(new CourseworkProject { CourseworkId = cw.CourseworkId, ProjectId = project.ProjectId });
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.GetAvailableProjectsAsync(supervisor.UserId, cw.CourseworkId, null);

        Assert.Single(result);
    }

    [Fact]
    public async Task GetAvailableProjectsAsync_FiltersByResearchArea()
    {
        using var context = new PASDbContext(_dbOptions);
        var (supervisor, _) = await CreateSupervisorAsync(context, "sup@example.com", "Supervisor");
        var cw = await CreateCourseworkAsync(context);
        var ra = new ResearchArea { Name = "AI" };
        context.ResearchAreas.Add(ra);
        await context.SaveChangesAsync();
        var project = await CreateProjectAsync(context, null, ra.Id);

        context.CourseworkProjects.Add(new CourseworkProject { CourseworkId = cw.CourseworkId, ProjectId = project.ProjectId });
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.GetAvailableProjectsAsync(supervisor.UserId, cw.CourseworkId, ra.Id);

        Assert.Single(result);
    }

    [Fact]
    public async Task GetAvailableProjectsAsync_MarksExpressedInterest()
    {
        using var context = new PASDbContext(_dbOptions);
        var (supervisor, _) = await CreateSupervisorAsync(context, "sup@example.com", "Supervisor");
        var cw = await CreateCourseworkAsync(context);
        var project = await CreateProjectAsync(context, null);

        context.CourseworkProjects.Add(new CourseworkProject { CourseworkId = cw.CourseworkId, ProjectId = project.ProjectId });
        context.Interests.Add(new Interest { SupervisorId = supervisor.UserId, ProjectId = project.ProjectId, Status = "Matched", CreatedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.GetAvailableProjectsAsync(supervisor.UserId, cw.CourseworkId, null);

        Assert.True(result.First().AlreadyExpressedInterest);
    }

    #endregion

    #region ExpressInterestAsync Tests

    [Fact]
    public async Task ExpressInterestAsync_WithValidProject_CreatesInterestAndMatch()
    {
        using var context = new PASDbContext(_dbOptions);
        var (supervisor, _) = await CreateSupervisorAsync(context, "sup@example.com", "Supervisor");
        var project = await CreateProjectAsync(context, null);

        var service = CreateService(context);

        await service.ExpressInterestAsync(supervisor.UserId, project.ProjectId);

        var interest = await context.Interests.FirstOrDefaultAsync(i => i.SupervisorId == supervisor.UserId && i.ProjectId == project.ProjectId);
        Assert.NotNull(interest);
        var match = await context.Matches.FirstOrDefaultAsync(m => m.SupervisorId == supervisor.UserId && m.ProjectId == project.ProjectId);
        Assert.NotNull(match);
    }

    [Fact]
    public async Task ExpressInterestAsync_WithInvalidProject_ThrowsKeyNotFoundException()
    {
        using var context = new PASDbContext(_dbOptions);
        var (supervisor, _) = await CreateSupervisorAsync(context, "sup@example.com", "Supervisor");

        var service = CreateService(context);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.ExpressInterestAsync(supervisor.UserId, 999));
    }

    [Fact]
    public async Task ExpressInterestAsync_WithNonSubmittedProject_ThrowsInvalidOperationException()
    {
        using var context = new PASDbContext(_dbOptions);
        var (supervisor, _) = await CreateSupervisorAsync(context, "sup@example.com", "Supervisor");
        var project = await CreateProjectAsync(context, null, null, "Draft");

        var service = CreateService(context);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ExpressInterestAsync(supervisor.UserId, project.ProjectId));
    }

    [Fact]
    public async Task ExpressInterestAsync_AlreadyExpressed_ThrowsInvalidOperationException()
    {
        using var context = new PASDbContext(_dbOptions);
        var (supervisor, _) = await CreateSupervisorAsync(context, "sup@example.com", "Supervisor");
        var project = await CreateProjectAsync(context, null);
        context.Interests.Add(new Interest { SupervisorId = supervisor.UserId, ProjectId = project.ProjectId, Status = "Matched", CreatedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();

        var service = CreateService(context);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ExpressInterestAsync(supervisor.UserId, project.ProjectId));
    }

    #endregion

    #region WithdrawInterestAsync Tests

    [Fact]
    public async Task WithdrawInterestAsync_WithValidInterest_RemovesInterestAndMatch()
    {
        using var context = new PASDbContext(_dbOptions);
        var (supervisor, _) = await CreateSupervisorAsync(context, "sup@example.com", "Supervisor");
        var project = await CreateProjectAsync(context, null);
        context.Interests.Add(new Interest { SupervisorId = supervisor.UserId, ProjectId = project.ProjectId, Status = "Matched", CreatedAt = DateTime.UtcNow });
        context.Matches.Add(new MatchModel { SupervisorId = supervisor.UserId, ProjectId = project.ProjectId, MatchDate = DateTime.UtcNow });
        await context.SaveChangesAsync();

        var service = CreateService(context);

        await service.WithdrawInterestAsync(supervisor.UserId, project.ProjectId);

        var interest = await context.Interests.FirstOrDefaultAsync(i => i.SupervisorId == supervisor.UserId && i.ProjectId == project.ProjectId);
        Assert.Null(interest);
        var match = await context.Matches.FirstOrDefaultAsync(m => m.SupervisorId == supervisor.UserId && m.ProjectId == project.ProjectId);
        Assert.Null(match);
    }

    [Fact]
    public async Task WithdrawInterestAsync_WithNoInterest_ThrowsKeyNotFoundException()
    {
        using var context = new PASDbContext(_dbOptions);
        var (supervisor, _) = await CreateSupervisorAsync(context, "sup@example.com", "Supervisor");

        var service = CreateService(context);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.WithdrawInterestAsync(supervisor.UserId, 999));
    }

    #endregion

    #region GetMatchedProjectsWithStudentInfoAsync Tests

    [Fact]
    public async Task GetMatchedProjectsWithStudentInfoAsync_WithMatches_ReturnsStudentInfo()
    {
        using var context = new PASDbContext(_dbOptions);
        var (supervisor, _) = await CreateSupervisorAsync(context, "sup@example.com", "Supervisor");
        var (student, _) = await CreateStudentWithGroupAsync(context);
        var project = await CreateProjectAsync(context, (await context.Groups.FirstAsync()).GroupId);

        context.Matches.Add(new MatchModel { SupervisorId = supervisor.UserId, ProjectId = project.ProjectId, MatchDate = DateTime.UtcNow });
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.GetMatchedProjectsWithStudentInfoAsync(supervisor.UserId);

        Assert.Single(result);
    }

    #endregion

    #region GetMatchHistoryAsync Tests

    [Fact]
    public async Task GetMatchHistoryAsync_WithMatches_ReturnsHistory()
    {
        using var context = new PASDbContext(_dbOptions);
        var (supervisor, _) = await CreateSupervisorAsync(context, "sup@example.com", "Supervisor");
        var (student, _) = await CreateStudentWithGroupAsync(context);
        var project = await CreateProjectAsync(context, (await context.Groups.FirstAsync()).GroupId);

        context.Matches.Add(new MatchModel { SupervisorId = supervisor.UserId, ProjectId = project.ProjectId, MatchDate = DateTime.UtcNow });
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.GetMatchHistoryAsync(supervisor.UserId);

        Assert.Single(result);
    }

    #endregion

    #region GetSubmissionsAsync Tests

    [Fact]
    public async Task GetSubmissionsAsync_WithMatchedProjects_ReturnsMatchedProjects()
    {
        using var context = new PASDbContext(_dbOptions);
        var (supervisor, _) = await CreateSupervisorAsync(context, "sup@example.com", "Supervisor");
        var project = await CreateProjectAsync(context, null, null, "Matched");

        context.Interests.Add(new Interest { SupervisorId = supervisor.UserId, ProjectId = project.ProjectId, Status = "Matched", CreatedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.GetSubmissionsAsync(supervisor.UserId, null);

        Assert.Single(result.MatchedProjects);
    }

    [Fact]
    public async Task GetSubmissionsAsync_FiltersByResearchAreaIds()
    {
        using var context = new PASDbContext(_dbOptions);
        var (supervisor, _) = await CreateSupervisorAsync(context, "sup@example.com", "Supervisor");
        var ra = new ResearchArea { Name = "AI" };
        context.ResearchAreas.Add(ra);
        await context.SaveChangesAsync();
        var project = await CreateProjectAsync(context, null, ra.Id);

        var service = CreateService(context);

        var result = await service.GetSubmissionsAsync(supervisor.UserId, new List<int> { ra.Id });

        Assert.Single(result.PendingReviews);
    }

    #endregion
}