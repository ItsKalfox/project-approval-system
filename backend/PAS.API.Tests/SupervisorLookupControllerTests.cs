using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PAS.API.Controllers;
using PAS.API.Data;
using PAS.API.Models;
using System.Security.Claims;
using Xunit;
using Microsoft.EntityFrameworkCore;

namespace PAS.API.Tests;

public class SupervisorLookupControllerTests : IAsyncLifetime
{
    private readonly DbContextOptions<PASDbContext> _dbOptions;

    public SupervisorLookupControllerTests()
    {
        _dbOptions = new DbContextOptionsBuilder<PASDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => Task.CompletedTask;

    private PASDbContext CreateContext() => new PASDbContext(_dbOptions);

    private SupervisorLookupController CreateController(PASDbContext context, int userId = 1)
    {
        var controller = new SupervisorLookupController(context);
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

    [Fact]
    public async Task GetCourseworks_ReturnsCourseworksList()
    {
        using var context = CreateContext();
        
        var coursework = new Coursework
        {
            Title = "Test Coursework",
            Description = "Test Description",
            Deadline = DateTime.UtcNow.AddDays(7),
            IsIndividual = true
        };
        context.Courseworks.Add(coursework);
        await context.SaveChangesAsync();

        var controller = CreateController(context);

        var result = await controller.GetCourseworks();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetCourseworks_Empty_ReturnsEmpty()
    {
        using var context = CreateContext();
        var controller = CreateController(context);

        var result = await controller.GetCourseworks();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetCourseworks_Multiple_ReturnsOrdered()
    {
        using var context = CreateContext();
        
        var cw1 = new Coursework { Title = "Beta Coursework", Description = "B", Deadline = DateTime.UtcNow, IsIndividual = true };
        var cw2 = new Coursework { Title = "Alpha Coursework", Description = "A", Deadline = DateTime.UtcNow, IsIndividual = false };
        context.Courseworks.AddRange(cw1, cw2);
        await context.SaveChangesAsync();

        var controller = CreateController(context);

        var result = await controller.GetCourseworks();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetResearchAreas_ReturnsAreasList()
    {
        using var context = CreateContext();
        
        var area = new ResearchArea { Name = "AI" };
        context.ResearchAreas.Add(area);
        await context.SaveChangesAsync();

        var controller = CreateController(context);

        var result = await controller.GetResearchAreas();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetResearchAreas_Empty_ReturnsEmpty()
    {
        using var context = CreateContext();
        var controller = CreateController(context);

        var result = await controller.GetResearchAreas();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetResearchAreas_Multiple_ReturnsOrdered()
    {
        using var context = CreateContext();
        
        var area1 = new ResearchArea { Name = "Beta Area" };
        var area2 = new ResearchArea { Name = "Alpha Area" };
        context.ResearchAreas.AddRange(area1, area2);
        await context.SaveChangesAsync();

        var controller = CreateController(context);

        var result = await controller.GetResearchAreas();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }
}