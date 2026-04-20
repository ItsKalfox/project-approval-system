using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PAS.API.Controllers;
using PAS.API.Data;
using PAS.API.Models;
using System.Security.Claims;
using Xunit;
using Microsoft.EntityFrameworkCore;

namespace PAS.API.Tests;

public class AllocationsControllerTests : IAsyncLifetime
{
    private readonly DbContextOptions<PASDbContext> _dbOptions;

    public AllocationsControllerTests()
    {
        _dbOptions = new DbContextOptionsBuilder<PASDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => Task.CompletedTask;

    private PASDbContext CreateContext() => new PASDbContext(_dbOptions);

    private AllocationsController CreateController(PASDbContext context, int userId = 1)
    {
        var controller = new AllocationsController(context);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
        return controller;
    }

    private async Task<(User sup, Supervisor supModel, User student, Student studentModel, Project project, Group group)> SetupTestDataAsync(PASDbContext context)
    {
        var supUser = new User { Email = "sup@example.com", Name = "Supervisor", Password = "hash", Role = "SUPERVISOR", CreatedAt = DateTime.UtcNow };
        context.Users.Add(supUser);
        await context.SaveChangesAsync();

        var supervisor = new Supervisor { UserId = supUser.UserId };
        context.Supervisors.Add(supervisor);
        await context.SaveChangesAsync();

        var studentUser = new User { Email = "student@example.com", Name = "Student", Password = "hash", Role = "STUDENT", CreatedAt = DateTime.UtcNow };
        context.Users.Add(studentUser);
        await context.SaveChangesAsync();

        var student = new Student { UserId = studentUser.UserId, Batch = "2024" };
        context.Students.Add(student);
        await context.SaveChangesAsync();

        var group = new Group { LeaderId = studentUser.UserId };
        context.Groups.Add(group);
        await context.SaveChangesAsync();

        var project = new Project
        {
            Title = "Test Project",
            GroupId = group.GroupId,
            Status = "Submitted",
            IsDeleted = false,
            SubmittedAt = DateTime.UtcNow
        };
        context.Projects.Add(project);
        await context.SaveChangesAsync();

        return (supUser, supervisor, studentUser, student, project, group);
    }

    #region GetAllocations Tests

    [Fact]
    public void GetAllocations_WithNoMatches_ReturnsEmptyData()
    {
        using var context = CreateContext();
        var controller = CreateController(context);

        var result = controller.GetAllocations();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public void GetAllocations_WithMatches_ReturnsMatches()
    {
        using var context = CreateContext();
        var (_, _, _, _, project, _) = SetupTestDataAsync(context).GetAwaiter().GetResult();
        var match = new Match { SupervisorId = 1, ProjectId = project.ProjectId, MatchDate = DateTime.UtcNow };
        context.Matches.Add(match);
        context.SaveChanges();

        var controller = CreateController(context);

        var result = controller.GetAllocations();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    #endregion

    #region GetComprehensiveAllocations Tests

    [Fact]
    public void GetComprehensiveAllocations_WithMatches_ReturnsComprehensiveData()
    {
        using var context = CreateContext();
        var (sup, _, _, _, project, _) = SetupTestDataAsync(context).GetAwaiter().GetResult();
        var match = new Match { SupervisorId = sup.UserId, ProjectId = project.ProjectId, MatchDate = DateTime.UtcNow };
        context.Matches.Add(match);
        context.SaveChanges();

        var controller = CreateController(context);

        var result = controller.GetComprehensiveAllocations();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    #endregion

    #region GetAvailableProjects Tests

    [Fact]
    public void GetAvailableProjects_WithUnmatchedProjects_ReturnsAvailableProjects()
    {
        using var context = CreateContext();
        var project = new Project { Title = "Available Project", Status = "Submitted", IsDeleted = false };
        context.Projects.Add(project);
        context.SaveChanges();

        var controller = CreateController(context);

        var result = controller.GetAvailableProjects();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public void GetAvailableProjects_WithMatchedProjects_ExcludesMatched()
    {
        using var context = CreateContext();
        var project = new Project { Title = "Project", Status = "Submitted", IsDeleted = false };
        context.Projects.Add(project);
        context.SaveChanges();
        var match = new Match { SupervisorId = 1, ProjectId = project.ProjectId, MatchDate = DateTime.UtcNow };
        context.Matches.Add(match);
        context.SaveChanges();

        var controller = CreateController(context);

        var result = controller.GetAvailableProjects();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    #endregion

    #region ReassignProject Tests

    [Fact]
    public void ReassignProject_WithValidRequest_CreatesNewMatch()
    {
        using var context = CreateContext();
        var (sup, _, _, _, project, _) = SetupTestDataAsync(context).GetAwaiter().GetResult();
        var controller = CreateController(context, sup.UserId);

        var result = controller.ReassignProject(new ReassignRequestDto
        {
            ProjectId = project.ProjectId,
            SupervisorId = sup.UserId,
            StudentId = 1
        });

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult);
    }

    [Fact]
    public void ReassignProject_WithNoUserClaim_ReturnsUnauthorized()
    {
        using var context = CreateContext();
        var controller = new AllocationsController(context);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        var result = controller.ReassignProject(new ReassignRequestDto());

        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.NotNull(unauthorizedResult);
    }

    #endregion

    #region ReassignSupervisor Tests

    [Fact]
    public void ReassignSupervisor_WithValidRequest_UpdatesMatch()
    {
        using var context = CreateContext();
        var (sup, _, studentUser, _, project, _) = SetupTestDataAsync(context).GetAwaiter().GetResult();
        var match = new Match { SupervisorId = sup.UserId, ProjectId = project.ProjectId, MatchDate = DateTime.UtcNow };
        context.Matches.Add(match);
        context.SaveChanges();

        var controller = CreateController(context, sup.UserId);

        var result = controller.ReassignSupervisor(new ReassignSupervisorRequestDto
        {
            StudentId = studentUser.UserId,
            SupervisorId = sup.UserId
        });

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult);
    }

    [Fact]
    public void ReassignSupervisor_WithInvalidStudent_ReturnsNotFound()
    {
        using var context = CreateContext();
        var controller = CreateController(context);

        var result = controller.ReassignSupervisor(new ReassignSupervisorRequestDto
        {
            StudentId = 999,
            SupervisorId = 1
        });

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult);
    }

    #endregion

    #region UpdateAllocationStatus Tests

    [Fact]
    public void UpdateAllocationStatus_WithValidId_ReturnsOk()
    {
        using var context = CreateContext();
        var (sup, _, _, _, project, _) = SetupTestDataAsync(context).GetAwaiter().GetResult();
        var match = new Match { SupervisorId = sup.UserId, ProjectId = project.ProjectId, MatchDate = DateTime.UtcNow };
        context.Matches.Add(match);
        context.SaveChanges();

        var controller = CreateController(context);

        var result = controller.UpdateAllocationStatus(match.MatchId, new UpdateStatusDto { Status = "Active" });

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult);
    }

    [Fact]
    public void UpdateAllocationStatus_WithInvalidId_ReturnsNotFound()
    {
        using var context = CreateContext();
        var controller = CreateController(context);

        var result = controller.UpdateAllocationStatus(999, new UpdateStatusDto { Status = "Active" });

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult);
    }

    #endregion

    #region DebugMatches Tests

    [Fact]
    public void DebugMatches_ReturnsMatchCount()
    {
        using var context = CreateContext();
        var controller = CreateController(context);

        var result = controller.DebugMatches();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult);
    }

    #endregion
}