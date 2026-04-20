using Xunit;
using PAS.API.Data;
using PAS.API.DTOs.Coursework;
using PAS.API.Models;
using PAS.API.Services;
using Microsoft.EntityFrameworkCore;

namespace PAS.API.Tests;

public class CourseworkServiceTests : IAsyncLifetime
{
    private readonly DbContextOptions<PASDbContext> _dbOptions;

    public CourseworkServiceTests()
    {
        _dbOptions = new DbContextOptionsBuilder<PASDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => Task.CompletedTask;

    private CourseworkService CreateService(PASDbContext context) => new(context);

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_WithNoCourseworks_ReturnsEmptyList()
    {
        using var context = new PASDbContext(_dbOptions);
        var service = CreateService(context);

        var result = await service.GetAllAsync();

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllAsync_WithCourseworks_ReturnsAllCourseworks()
    {
        using var context = new PASDbContext(_dbOptions);
        context.Courseworks.AddRange(
            new Coursework { Title = "Coursework 1", Description = "Desc 1", Deadline = DateTime.UtcNow.AddDays(7), IsIndividual = true },
            new Coursework { Title = "Coursework 2", Description = "Desc 2", Deadline = DateTime.UtcNow.AddDays(14), IsIndividual = false }
        );
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.GetAllAsync();

        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetAllAsync_ReturnsActiveStatus()
    {
        using var context = new PASDbContext(_dbOptions);
        var coursework = new Coursework { Title = "Test", Description = "Desc", Deadline = DateTime.UtcNow.AddDays(7), IsIndividual = true };
        context.Courseworks.Add(coursework);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.GetAllAsync();

        Assert.Single(result);
        Assert.True(result.First().IsActive);
    }

    #endregion

    #region GetActiveAsync Tests

    [Fact]
    public async Task GetActiveAsync_WithNoCourseworks_ReturnsEmptyList()
    {
        using var context = new PASDbContext(_dbOptions);
        var service = CreateService(context);

        var result = await service.GetActiveAsync();

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetActiveAsync_WithActiveCourseworks_ReturnsOnlyActive()
    {
        using var context = new PASDbContext(_dbOptions);
        context.Courseworks.Add(new Coursework { Title = "Active", Description = "Desc", Deadline = DateTime.UtcNow.AddDays(7), IsIndividual = true });
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.GetActiveAsync();

        Assert.Single(result);
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsCoursework()
    {
        using var context = new PASDbContext(_dbOptions);
        var coursework = new Coursework { Title = "Test", Description = "Desc", Deadline = DateTime.UtcNow.AddDays(7), IsIndividual = true };
        context.Courseworks.Add(coursework);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.GetByIdAsync(coursework.CourseworkId);

        Assert.NotNull(result);
        Assert.Equal("Test", result.Title);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
    {
        using var context = new PASDbContext(_dbOptions);
        var service = CreateService(context);

        var result = await service.GetByIdAsync(999);

        Assert.Null(result);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidData_CreatesCoursework()
    {
        using var context = new PASDbContext(_dbOptions);
        var service = CreateService(context);
        var dto = new CreateCourseworkDto
        {
            Title = "New Coursework",
            Description = "Description",
            Deadline = DateTime.UtcNow.AddDays(7),
            IsIndividual = true
        };

        var result = await service.CreateAsync(dto, 1);

        Assert.NotNull(result);
        Assert.Equal("New Coursework", result.Title);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task CreateAsync_SetsIsActiveToTrue()
    {
        using var context = new PASDbContext(_dbOptions);
        var service = CreateService(context);
        var dto = new CreateCourseworkDto
        {
            Title = "Test",
            Description = "Desc",
            Deadline = DateTime.UtcNow.AddDays(7),
            IsIndividual = true
        };

        var result = await service.CreateAsync(dto, 1);

        Assert.True(result.IsActive);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidId_UpdatesCoursework()
    {
        using var context = new PASDbContext(_dbOptions);
        var coursework = new Coursework { Title = "Old Title", Description = "Old Desc", Deadline = DateTime.UtcNow.AddDays(7), IsIndividual = true };
        context.Courseworks.Add(coursework);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var dto = new UpdateCourseworkDto
        {
            Title = "New Title",
            Description = "New Desc",
            Deadline = DateTime.UtcNow.AddDays(14),
            IsIndividual = false,
            IsActive = false
        };

        var result = await service.UpdateAsync(coursework.CourseworkId, dto);

        Assert.NotNull(result);
        Assert.Equal("New Title", result.Title);
    }

    [Fact]
    public async Task UpdateAsync_WithInvalidId_ReturnsNull()
    {
        using var context = new PASDbContext(_dbOptions);
        var service = CreateService(context);
        var dto = new UpdateCourseworkDto { Title = "Test" };

        var result = await service.UpdateAsync(999, dto);

        Assert.Null(result);
    }

    #endregion

    #region ToggleActiveAsync Tests

    [Fact]
    public async Task ToggleActiveAsync_WithValidId_TogglesStatus()
    {
        using var context = new PASDbContext(_dbOptions);
        var coursework = new Coursework { Title = "Test", Description = "Desc", Deadline = DateTime.UtcNow.AddDays(7), IsIndividual = true };
        context.Courseworks.Add(coursework);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.ToggleActiveAsync(coursework.CourseworkId);

        Assert.True(result);
    }

    [Fact]
    public async Task ToggleActiveAsync_WithInvalidId_ReturnsFalse()
    {
        using var context = new PASDbContext(_dbOptions);
        var service = CreateService(context);

        var result = await service.ToggleActiveAsync(999);

        Assert.False(result);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithValidId_DeletesCoursework()
    {
        using var context = new PASDbContext(_dbOptions);
        var coursework = new Coursework { Title = "Test", Description = "Desc", Deadline = DateTime.UtcNow.AddDays(7), IsIndividual = true };
        context.Courseworks.Add(coursework);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.DeleteAsync(coursework.CourseworkId);

        Assert.True(result);
        Assert.Null(await context.Courseworks.FindAsync(coursework.CourseworkId));
    }

    [Fact]
    public async Task DeleteAsync_WithInvalidId_ReturnsFalse()
    {
        using var context = new PASDbContext(_dbOptions);
        var service = CreateService(context);

        var result = await service.DeleteAsync(999);

        Assert.False(result);
    }

    #endregion
}