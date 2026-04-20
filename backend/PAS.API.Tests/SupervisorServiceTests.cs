using Moq;
using Xunit;
using PAS.API.Data;
using PAS.API.DTOs.Supervisor;
using PAS.API.Models;
using PAS.API.Services;
using Microsoft.EntityFrameworkCore;

namespace PAS.API.Tests;

public class SupervisorServiceTests : IAsyncLifetime
{
    private readonly DbContextOptions<PASDbContext> _dbOptions;
    private Mock<IEmailService> _mockEmailService = null!;

    public SupervisorServiceTests()
    {
        _dbOptions = new DbContextOptionsBuilder<PASDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => Task.CompletedTask;

    private SupervisorService CreateService(PASDbContext context)
    {
        _mockEmailService = new Mock<IEmailService>();
        return new SupervisorService(context, _mockEmailService.Object);
    }

    #region CreateSupervisorAsync Tests

    [Fact]
    public async Task CreateSupervisorAsync_WithValidData_CreatesSupervisor()
    {
        using var context = new PASDbContext(_dbOptions);
        var service = CreateService(context);
        var dto = new CreateSupervisorDto
        {
            Name = "Dr. Smith",
            Email = "smith@example.com",
            Expertise = "Machine Learning"
        };

        _mockEmailService.Setup(s => s.SendWelcomeEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

        var (supervisor, emailSent) = await service.CreateSupervisorAsync(dto);

        Assert.NotNull(supervisor);
        Assert.Equal("Dr. Smith", supervisor.Name);
        Assert.True(emailSent);
    }

    [Fact]
    public async Task CreateSupervisorAsync_WithDuplicateEmail_ThrowsInvalidOperationException()
    {
        using var context = new PASDbContext(_dbOptions);
        var existingUser = new User { Name = "Existing", Email = "existing@example.com", Password = "hash", Role = "SUPERVISOR", CreatedAt = DateTime.UtcNow };
        context.Users.Add(existingUser);
        await context.SaveChangesAsync();
        context.Supervisors.Add(new Supervisor { UserId = existingUser.UserId });
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var dto = new CreateSupervisorDto { Name = "New", Email = "existing@example.com", Expertise = "ML" };

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateSupervisorAsync(dto));
    }

    #endregion

    #region GetAllSupervisorsAsync Tests

    [Fact]
    public async Task GetAllSupervisorsAsync_ReturnsPagedResults()
    {
        using var context = new PASDbContext(_dbOptions);
        for (int i = 1; i <= 5; i++)
        {
            var user = new User { Name = $"Supervisor {i}", Email = $"sup{i}@example.com", Password = "hash", Role = "SUPERVISOR", CreatedAt = DateTime.UtcNow };
            context.Users.Add(user);
            await context.SaveChangesAsync();
            context.Supervisors.Add(new Supervisor { UserId = user.UserId });
        }
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.GetAllSupervisorsAsync(1, 10);

        Assert.Equal(5, result.TotalCount);
        Assert.Equal(5, result.Data.Count());
    }

    [Fact]
    public async Task GetAllSupervisorsAsync_WithPagination_ReturnsCorrectPage()
    {
        using var context = new PASDbContext(_dbOptions);
        for (int i = 1; i <= 15; i++)
        {
            var user = new User { Name = $"Supervisor {i}", Email = $"sup{i}@example.com", Password = "hash", Role = "SUPERVISOR", CreatedAt = DateTime.UtcNow };
            context.Users.Add(user);
            await context.SaveChangesAsync();
            context.Supervisors.Add(new Supervisor { UserId = user.UserId });
        }
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.GetAllSupervisorsAsync(2, 5);

        Assert.Equal(15, result.TotalCount);
        Assert.Equal(5, result.Data.Count());
    }

    #endregion

    #region GetSupervisorAsync Tests

    [Fact]
    public async Task GetSupervisorAsync_WithValidId_ReturnsSupervisor()
    {
        using var context = new PASDbContext(_dbOptions);
        var user = new User { Name = "Supervisor", Email = "sup@example.com", Password = "hash", Role = "SUPERVISOR", CreatedAt = DateTime.UtcNow };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        context.Supervisors.Add(new Supervisor { UserId = user.UserId });
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.GetSupervisorAsync(user.UserId);

        Assert.Equal("Supervisor", result.Name);
    }

    [Fact]
    public async Task GetSupervisorAsync_WithInvalidId_ThrowsKeyNotFoundException()
    {
        using var context = new PASDbContext(_dbOptions);
        var service = CreateService(context);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetSupervisorAsync(999));
    }

    #endregion

    #region UpdateSupervisorAsync Tests

    [Fact]
    public async Task UpdateSupervisorAsync_UpdatesSupervisor()
    {
        using var context = new PASDbContext(_dbOptions);
        var user = new User { Name = "Old Name", Email = "sup@example.com", Password = "hash", Role = "SUPERVISOR", CreatedAt = DateTime.UtcNow };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        context.Supervisors.Add(new Supervisor { UserId = user.UserId });
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var dto = new UpdateSupervisorDto { Name = "New Name" };

        var result = await service.UpdateSupervisorAsync(user.UserId, dto);

        Assert.Equal("New Name", result.Name);
    }

    #endregion

    #region DeleteSupervisorAsync Tests

    [Fact]
    public async Task DeleteSupervisorAsync_DeletesSupervisor()
    {
        using var context = new PASDbContext(_dbOptions);
        var user = new User { Name = "Supervisor", Email = "sup@example.com", Password = "hash", Role = "SUPERVISOR", CreatedAt = DateTime.UtcNow };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        context.Supervisors.Add(new Supervisor { UserId = user.UserId });
        await context.SaveChangesAsync();

        var service = CreateService(context);

        await service.DeleteSupervisorAsync(user.UserId);

        var deleted = await context.Users.FirstOrDefaultAsync(u => u.UserId == user.UserId);
        Assert.Null(deleted);
    }

    #endregion

    #region ResetSupervisorPasswordAsync Tests

    [Fact]
    public async Task ResetSupervisorPasswordAsync_ResetsPasswordAndSendsEmail()
    {
        using var context = new PASDbContext(_dbOptions);
        var user = new User { Name = "Supervisor", Email = "sup@example.com", Password = "hash", Role = "SUPERVISOR", CreatedAt = DateTime.UtcNow };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        context.Supervisors.Add(new Supervisor { UserId = user.UserId });
        await context.SaveChangesAsync();

        var service = CreateService(context);

        _mockEmailService.Setup(s => s.SendWelcomeEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

        var (passwordReset, emailSent) = await service.ResetSupervisorPasswordAsync(user.UserId);

        Assert.True(passwordReset);
        Assert.True(emailSent);
    }

    #endregion
}