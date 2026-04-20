using Moq;
using Xunit;
using PAS.API.Data;
using PAS.API.DTOs.ResearchArea;
using PAS.API.Models;
using PAS.API.Services;
using Microsoft.EntityFrameworkCore;

namespace PAS.API.Tests;

public class ResearchAreaServiceTests : IAsyncLifetime
{
    private readonly DbContextOptions<PASDbContext> _dbOptions;

    public ResearchAreaServiceTests()
    {
        // Create a unique in-memory database for each test
        _dbOptions = new DbContextOptionsBuilder<PASDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CreateResearchAreaAsync_WithValidName_CreatesAndReturnsResearchArea()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var service = new ResearchAreaService(context);
        var dto = new CreateResearchAreaDto { Name = "Artificial Intelligence" };

        // Act
        var result = await service.CreateResearchAreaAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Artificial Intelligence", result.Name);
        Assert.NotEqual(0, result.Id);
        
        // Verify it was saved
        var saved = await context.ResearchAreas.FirstOrDefaultAsync(r => r.Name == "Artificial Intelligence");
        Assert.NotNull(saved);
    }

    [Fact]
    public async Task CreateResearchAreaAsync_WithEmptyName_ThrowsArgumentException()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var service = new ResearchAreaService(context);
        var dto = new CreateResearchAreaDto { Name = "" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateResearchAreaAsync(dto));
    }

    [Fact]
    public async Task CreateResearchAreaAsync_WithDuplicateName_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var service = new ResearchAreaService(context);
        
        // Add existing area
        context.ResearchAreas.Add(new ResearchArea { Name = "Machine Learning" });
        await context.SaveChangesAsync();

        var dto = new CreateResearchAreaDto { Name = "Machine Learning" };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateResearchAreaAsync(dto));
    }

    [Fact]
    public async Task GetResearchAreaAsync_WithValidId_ReturnsResearchArea()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var service = new ResearchAreaService(context);
        
        var testArea = new ResearchArea { Name = "Data Science" };
        context.ResearchAreas.Add(testArea);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetResearchAreaAsync(testArea.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(testArea.Id, result.Id);
        Assert.Equal("Data Science", result.Name);
    }

    [Fact]
    public async Task GetResearchAreaAsync_WithInvalidId_ThrowsKeyNotFoundException()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var service = new ResearchAreaService(context);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetResearchAreaAsync(999));
    }

    [Fact]
    public async Task UpdateResearchAreaAsync_WithValidData_UpdatesResearchArea()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var service = new ResearchAreaService(context);
        
        var testArea = new ResearchArea { Name = "Old Name" };
        context.ResearchAreas.Add(testArea);
        await context.SaveChangesAsync();

        var dto = new UpdateResearchAreaDto { Name = "New Name" };

        // Act
        var result = await service.UpdateResearchAreaAsync(testArea.Id, dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("New Name", result.Name);
        
        // Verify persistence
        var updated = await context.ResearchAreas.FirstAsync(r => r.Id == testArea.Id);
        Assert.Equal("New Name", updated.Name);
    }

    [Fact]
    public async Task UpdateResearchAreaAsync_WithInvalidId_ThrowsKeyNotFoundException()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var service = new ResearchAreaService(context);
        var dto = new UpdateResearchAreaDto { Name = "New Name" };

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.UpdateResearchAreaAsync(999, dto));
    }

    [Fact]
    public async Task UpdateResearchAreaAsync_WithDuplicateName_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var service = new ResearchAreaService(context);
        
        var area1 = new ResearchArea { Name = "AI" };
        var area2 = new ResearchArea { Name = "ML" };
        context.ResearchAreas.AddRange(area1, area2);
        await context.SaveChangesAsync();

        var dto = new UpdateResearchAreaDto { Name = "AI" };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateResearchAreaAsync(area2.Id, dto));
    }

    [Fact]
    public async Task GetAllResearchAreasAsync_ReturnsAllAreasSorted()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var service = new ResearchAreaService(context);
        
        var areas = new List<ResearchArea>
        {
            new ResearchArea { Name = "Zebra" },
            new ResearchArea { Name = "Apple" },
            new ResearchArea { Name = "Banana" }
        };
        context.ResearchAreas.AddRange(areas);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetAllResearchAreasAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Equal("Apple", result[0].Name);
        Assert.Equal("Banana", result[1].Name);
        Assert.Equal("Zebra", result[2].Name);
    }

    [Fact]
    public async Task DeleteResearchAreaAsync_WithValidId_DeletesResearchArea()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var service = new ResearchAreaService(context);
        
        var testArea = new ResearchArea { Name = "To Delete" };
        context.ResearchAreas.Add(testArea);
        await context.SaveChangesAsync();

        var id = testArea.Id;

        // Act
        await service.DeleteResearchAreaAsync(id);

        // Assert
        var deleted = await context.ResearchAreas.FirstOrDefaultAsync(r => r.Id == id);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteResearchAreaAsync_WithInvalidId_ThrowsKeyNotFoundException()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var service = new ResearchAreaService(context);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.DeleteResearchAreaAsync(999));
    }

    [Fact]
    public async Task CreateResearchAreaAsync_WithWhitespaceOnlyName_ThrowsArgumentException()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var service = new ResearchAreaService(context);
        var dto = new CreateResearchAreaDto { Name = "   " };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateResearchAreaAsync(dto));
    }

    [Fact]
    public async Task CreateResearchAreaAsync_WithNameTrimming_CreatesWithTrimmedName()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var service = new ResearchAreaService(context);
        var dto = new CreateResearchAreaDto { Name = "  Quantum Computing  " };

        // Act
        var result = await service.CreateResearchAreaAsync(dto);

        // Assert
        Assert.Equal("Quantum Computing", result.Name);
    }
}
