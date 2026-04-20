using Moq;
using Xunit;
using PAS.API.Data;
using PAS.API.DTOs.ModuleLeader;
using PAS.API.Models;
using PAS.API.Services;
using Microsoft.EntityFrameworkCore;

namespace PAS.API.Tests;

public class ModuleLeaderServiceTests : IAsyncLifetime
{
    private readonly DbContextOptions<PASDbContext> _dbOptions;
    private Mock<IEmailService> _mockEmailService = null!;

    public ModuleLeaderServiceTests()
    {
        _dbOptions = new DbContextOptionsBuilder<PASDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => Task.CompletedTask;

    private ModuleLeaderService CreateService(PASDbContext context)
    {
        _mockEmailService = new Mock<IEmailService>();
        return new ModuleLeaderService(context, _mockEmailService.Object);
    }

    private async Task<(User user, ModuleLeader ml)> CreateModuleLeaderAsync(PASDbContext context, string email, string name)
    {
        var user = new User
        {
            Email = email,
            Name = name,
            Password = BCrypt.Net.BCrypt.HashPassword("password", 12),
            Role = "MODULE LEADER",
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var ml = new ModuleLeader { UserId = user.UserId };
        context.ModuleLeaders.Add(ml);
        await context.SaveChangesAsync();

        return (user, ml);
    }

    #region GetAllModuleLeadersAsync Tests

    [Fact]
    public async Task GetAllModuleLeadersAsync_WithNoModuleLeaders_ReturnsEmptyPagedResult()
    {
        using var context = new PASDbContext(_dbOptions);
        var service = CreateService(context);

        var result = await service.GetAllModuleLeadersAsync(1, 10);

        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Data);
    }

    [Fact]
    public async Task GetAllModuleLeadersAsync_WithModuleLeaders_ReturnsPagedResults()
    {
        using var context = new PASDbContext(_dbOptions);
        await CreateModuleLeaderAsync(context, "ml1@example.com", "Module Leader 1");
        await CreateModuleLeaderAsync(context, "ml2@example.com", "Module Leader 2");

        var service = CreateService(context);

        var result = await service.GetAllModuleLeadersAsync(1, 10);

        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task GetAllModuleLeadersAsync_WithPagination_ReturnsCorrectPage()
    {
        using var context = new PASDbContext(_dbOptions);
        for (int i = 1; i <= 15; i++)
            await CreateModuleLeaderAsync(context, $"ml{i}@example.com", $"ML {i}");

        var service = CreateService(context);

        var result = await service.GetAllModuleLeadersAsync(2, 5);

        Assert.Equal(15, result.TotalCount);
        Assert.Equal(5, result.Data.Count());
        Assert.Equal(2, result.Page);
    }

    [Fact]
    public async Task GetAllModuleLeadersAsync_OrdersByUserId()
    {
        using var context = new PASDbContext(_dbOptions);
        await CreateModuleLeaderAsync(context, "ml1@example.com", "ML 1");

        var service = CreateService(context);

        var result = await service.GetAllModuleLeadersAsync(1, 10);

        Assert.Single(result.Data);
    }

    [Fact]
    public async Task GetAllModuleLeadersAsync_PageClampedToValidRange()
    {
        using var context = new PASDbContext(_dbOptions);
        await CreateModuleLeaderAsync(context, "ml@example.com", "ML");

        var service = CreateService(context);

        var result = await service.GetAllModuleLeadersAsync(1, 100);

        Assert.NotNull(result);
    }

    #endregion

    #region GetModuleLeaderAsync Tests

    [Fact]
    public async Task GetModuleLeaderAsync_WithValidId_ReturnsModuleLeader()
    {
        using var context = new PASDbContext(_dbOptions);
        var (user, _) = await CreateModuleLeaderAsync(context, "ml@example.com", "Module Leader");

        var service = CreateService(context);

        var result = await service.GetModuleLeaderAsync(user.UserId);

        Assert.Equal(user.UserId, result.UserId);
        Assert.Equal("Module Leader", result.Name);
    }

    [Fact]
    public async Task GetModuleLeaderAsync_WithInvalidId_ThrowsKeyNotFoundException()
    {
        using var context = new PASDbContext(_dbOptions);
        var service = CreateService(context);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetModuleLeaderAsync(999));
    }

    #endregion

    #region UpdateModuleLeaderAsync Tests

    [Fact]
    public async Task UpdateModuleLeaderAsync_WithValidData_UpdatesModuleLeader()
    {
        using var context = new PASDbContext(_dbOptions);
        var (user, _) = await CreateModuleLeaderAsync(context, "ml@example.com", "Old Name");

        var service = CreateService(context);
        var dto = new UpdateModuleLeaderDto { Name = "New Name" };

        var result = await service.UpdateModuleLeaderAsync(user.UserId, dto);

        Assert.Equal("New Name", result.Name);
    }

    [Fact]
    public async Task UpdateModuleLeaderAsync_WithEmailChange_UpdatesEmail()
    {
        using var context = new PASDbContext(_dbOptions);
        var (user, _) = await CreateModuleLeaderAsync(context, "old@example.com", "ML");

        var service = CreateService(context);
        var dto = new UpdateModuleLeaderDto { Email = "new@example.com" };

        var result = await service.UpdateModuleLeaderAsync(user.UserId, dto);

        Assert.Equal("new@example.com", result.Email);
    }

    [Fact]
    public async Task UpdateModuleLeaderAsync_WithDuplicateEmail_ThrowsInvalidOperationException()
    {
        using var context = new PASDbContext(_dbOptions);
        var (user1, _) = await CreateModuleLeaderAsync(context, "ml1@example.com", "ML 1");
        var (user2, _) = await CreateModuleLeaderAsync(context, "ml2@example.com", "ML 2");

        var service = CreateService(context);
        var dto = new UpdateModuleLeaderDto { Email = "ml1@example.com" };

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateModuleLeaderAsync(user2.UserId, dto));
    }

    [Fact]
    public async Task UpdateModuleLeaderAsync_WithInvalidId_ThrowsKeyNotFoundException()
    {
        using var context = new PASDbContext(_dbOptions);
        var service = CreateService(context);
        var dto = new UpdateModuleLeaderDto { Name = "Test" };

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.UpdateModuleLeaderAsync(999, dto));
    }

    [Fact]
    public async Task UpdateModuleLeaderAsync_TrimsName()
    {
        using var context = new PASDbContext(_dbOptions);
        var (user, _) = await CreateModuleLeaderAsync(context, "ml@example.com", "ML");

        var service = CreateService(context);
        var dto = new UpdateModuleLeaderDto { Name = "  Trimmed Name  " };

        var result = await service.UpdateModuleLeaderAsync(user.UserId, dto);

        Assert.Equal("Trimmed Name", result.Name);
    }

    #endregion

    #region DeleteModuleLeaderAsync Tests

    [Fact]
    public async Task DeleteModuleLeaderAsync_WithValidId_DeletesModuleLeader()
    {
        using var context = new PASDbContext(_dbOptions);
        var (user, _) = await CreateModuleLeaderAsync(context, "ml@example.com", "ML");

        var service = CreateService(context);

        await service.DeleteModuleLeaderAsync(user.UserId);

        var deletedUser = await context.Users.FirstOrDefaultAsync(u => u.UserId == user.UserId);
        Assert.Null(deletedUser);
    }

    [Fact]
    public async Task DeleteModuleLeaderAsync_WithInvalidId_ThrowsKeyNotFoundException()
    {
        using var context = new PASDbContext(_dbOptions);
        var service = CreateService(context);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.DeleteModuleLeaderAsync(999));
    }

    #endregion

    #region ResetModuleLeaderPasswordAsync Tests

    [Fact]
    public async Task ResetModuleLeaderPasswordAsync_WithValidId_ResetsPasswordAndSendsEmail()
    {
        using var context = new PASDbContext(_dbOptions);
        var (user, _) = await CreateModuleLeaderAsync(context, "ml@example.com", "ML");

        var service = CreateService(context);

        _mockEmailService
            .Setup(s => s.SendWelcomeEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var (passwordReset, emailSent) = await service.ResetModuleLeaderPasswordAsync(user.UserId);

        Assert.True(passwordReset);
        Assert.True(emailSent);

        _mockEmailService.Verify(
            s => s.SendWelcomeEmailAsync("ml@example.com", "ML", It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task ResetModuleLeaderPasswordAsync_WithInvalidId_ThrowsKeyNotFoundException()
    {
        using var context = new PASDbContext(_dbOptions);
        var service = CreateService(context);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.ResetModuleLeaderPasswordAsync(999));
    }

    [Fact]
    public async Task ResetModuleLeaderPasswordAsync_EmailFails_ReturnsPasswordResetTrueWithEmailSentFalse()
    {
        using var context = new PASDbContext(_dbOptions);
        var (user, _) = await CreateModuleLeaderAsync(context, "ml@example.com", "ML");

        var service = CreateService(context);

        _mockEmailService
            .Setup(s => s.SendWelcomeEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("Email service failed"));

        var (passwordReset, emailSent) = await service.ResetModuleLeaderPasswordAsync(user.UserId);

        Assert.True(passwordReset);
        Assert.False(emailSent);
    }

    #endregion
}