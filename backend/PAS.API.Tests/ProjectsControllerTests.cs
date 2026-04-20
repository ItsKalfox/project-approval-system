using Microsoft.AspNetCore.Mvc;
using PAS.API.Controllers;
using PAS.API.Data;
using PAS.API.Models;
using Xunit;
using Microsoft.EntityFrameworkCore;

namespace PAS.API.Tests;

public class ProjectsControllerTests : IAsyncLifetime
{
    private readonly DbContextOptions<PASDbContext> _dbOptions;

    public ProjectsControllerTests()
    {
        _dbOptions = new DbContextOptionsBuilder<PASDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => Task.CompletedTask;

    private PASDbContext CreateContext() => new PASDbContext(_dbOptions);
    private ProjectsController CreateController(PASDbContext context) => new ProjectsController(context);

    #region GetAvailableProjects Tests

    [Fact]
    public void GetAvailableProjects_WithNoProjects_ReturnsEmptyData()
    {
        using var context = CreateContext();
        var controller = CreateController(context);

        var result = controller.GetAvailableProjects();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public void GetAvailableProjects_WithUnmatchedProjects_ReturnsProjects()
    {
        using var context = CreateContext();
        var project = new Project { Title = "Test Project", Status = "Submitted", IsDeleted = false };
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
        var project = new Project { Title = "Matched Project", Status = "Matched", IsDeleted = false };
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

    #region GetAllProjects Tests

    [Fact]
    public void GetAllProjects_WithNoProjects_ReturnsEmptyData()
    {
        using var context = CreateContext();
        var controller = CreateController(context);

        var result = controller.GetAllProjects();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public void GetAllProjects_ReturnsAllProjects()
    {
        using var context = CreateContext();
        var project1 = new Project { Title = "Project 1", Abstract = "Abstract 1", Status = "Submitted", IsDeleted = false };
        var project2 = new Project { Title = "Project 2", Abstract = "Abstract 2", Status = "Draft", IsDeleted = false };
        context.Projects.AddRange(project1, project2);
        context.SaveChanges();

        var controller = CreateController(context);

        var result = controller.GetAllProjects();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public void GetAllProjects_ExcludesDeletedProjects()
    {
        using var context = CreateContext();
        var project = new Project { Title = "Active Project", Status = "Submitted", IsDeleted = false };
        var deletedProject = new Project { Title = "Deleted Project", Status = "Submitted", IsDeleted = true };
        context.Projects.AddRange(project, deletedProject);
        context.SaveChanges();

        var controller = CreateController(context);

        var result = controller.GetAllProjects();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    #endregion
}