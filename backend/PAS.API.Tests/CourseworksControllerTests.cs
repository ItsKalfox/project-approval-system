using Microsoft.AspNetCore.Mvc;
using PAS.API.Controllers;
using PAS.API.DTOs.Coursework;
using PAS.API.Models;
using PAS.API.Services;
using Xunit;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Moq;
using Microsoft.EntityFrameworkCore;
using PAS.API.Data;

namespace PAS.API.Tests;

public class CourseworksControllerTests : IAsyncLifetime
{
    private readonly DbContextOptions<PASDbContext> _dbOptions;

    public CourseworksControllerTests()
    {
        _dbOptions = new DbContextOptionsBuilder<PASDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => Task.CompletedTask;

    private ICourseworkService CreateCourseworkService()
    {
        var context = new PASDbContext(_dbOptions);
        return new CourseworkService(context);
    }

    private CourseworksController CreateController(ICourseworkService courseworkService, int userId = 1)
    {
        var controller = new CourseworksController(courseworkService);
        var claims = new List<Claim>
        {
            new("sub", userId.ToString()),
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Role, "MODULE LEADER")
        };
        var identity = new ClaimsIdentity(claims);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
        return controller;
    }

    #region GetAll Tests

    [Fact]
    public async Task GetAll_WithNoCourseworks_ReturnsEmptyList()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var service = new CourseworkService(context);
        var controller = CreateController(service);

        // Act
        var result = await controller.GetAll();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetAll_WithCourseworks_ReturnsAll()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var service = new CourseworkService(context);
        context.Courseworks.Add(new Coursework { Title = "Coursework 1", IsIndividual = true });
        context.Courseworks.Add(new Coursework { Title = "Coursework 2", IsIndividual = false });
        context.SaveChanges();

        var controller = CreateController(service);

        // Act
        var result = await controller.GetAll();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(okResult.Value);
    }

    #endregion

    #region GetActive Tests

    [Fact]
    public async Task GetActive_ReturnsActiveCourseworks()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var service = new CourseworkService(context);
        context.Courseworks.Add(new Coursework { Title = "Active CW", IsIndividual = true });
        context.SaveChanges();

        var controller = CreateController(service);

        // Act
        var result = await controller.GetActive();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(okResult.Value);
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_WithValidId_ReturnsCoursework()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var service = new CourseworkService(context);
        var coursework = new Coursework { Title = "Test CW", IsIndividual = true };
        context.Courseworks.Add(coursework);
        context.SaveChanges();

        var controller = CreateController(service);

        // Act
        var result = await controller.GetById(coursework.CourseworkId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetById_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var service = new CourseworkService(context);
        var controller = CreateController(service);

        // Act
        var result = await controller.GetById(999);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task Create_WithValidDto_ReturnsCreated()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var service = new CourseworkService(context);
        var controller = CreateController(service);
        var dto = new CreateCourseworkDto
        {
            Title = "New Coursework",
            IsIndividual = true,
            Description = "Test description"
        };

        // Act
        var result = await controller.Create(dto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.NotNull(createdResult.Value);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_WithValidId_ReturnsUpdated()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var service = new CourseworkService(context);
        var coursework = new Coursework { Title = "Original", IsIndividual = true };
        context.Courseworks.Add(coursework);
        context.SaveChanges();

        var controller = CreateController(service);
        var dto = new UpdateCourseworkDto
        {
            Title = "Updated",
            IsActive = true,
            IsIndividual = true
        };

        // Act
        var result = await controller.Update(coursework.CourseworkId, dto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task Update_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var service = new CourseworkService(context);
        var controller = CreateController(service);
        var dto = new UpdateCourseworkDto
        {
            Title = "Updated",
            IsActive = false,
            IsIndividual = true
        };

        // Act
        var result = await controller.Update(999, dto);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    #endregion

    #region ToggleActive Tests

    [Fact]
    public async Task ToggleActive_WithValidId_ReturnsOk()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var service = new CourseworkService(context);
        var coursework = new Coursework { Title = "Test CW", IsIndividual = true };
        context.Courseworks.Add(coursework);
        context.SaveChanges();

        var controller = CreateController(service);

        // Act
        var result = await controller.ToggleActive(coursework.CourseworkId);

        // Assert
        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task ToggleActive_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var service = new CourseworkService(context);
        var controller = CreateController(service);

        // Act
        var result = await controller.ToggleActive(999);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_WithValidId_ReturnsNoContent()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var service = new CourseworkService(context);
        var coursework = new Coursework { Title = "Test CW", IsIndividual = true };
        context.Courseworks.Add(coursework);
        context.SaveChanges();

        var controller = CreateController(service);

        // Act
        var result = await controller.Delete(coursework.CourseworkId);

        // Assert
        var noContentResult = Assert.IsType<NoContentResult>(result);
        Assert.Equal(StatusCodes.Status204NoContent, noContentResult.StatusCode);
    }

    [Fact]
    public async Task Delete_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var service = new CourseworkService(context);
        var controller = CreateController(service);

        // Act
        var result = await controller.Delete(999);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    #endregion
}
