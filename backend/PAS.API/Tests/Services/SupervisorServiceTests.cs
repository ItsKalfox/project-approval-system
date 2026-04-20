using Microsoft.EntityFrameworkCore;
using Moq;
using PAS.API.Data;
using PAS.API.DTOs.Supervisor;
using PAS.API.Models;
using PAS.API.Services;
using Xunit;

namespace PAS.API.Tests.Services;

public class SupervisorServiceTests
{
    private readonly PASDbContext _dbContext;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly SupervisorService _supervisorService;

    public SupervisorServiceTests()
    {
        var options = new DbContextOptionsBuilder<PASDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        _dbContext = new PASDbContext(options);
        _mockEmailService = new Mock<IEmailService>();
        _supervisorService = new SupervisorService(_dbContext, _mockEmailService.Object);
    }

    [Fact]
    public async Task CreateSupervisorAsync_ValidData_ReturnsSupervisor()
    {
        var dto = new CreateSupervisorDto { Name = "Dr. Smith", Email = "smith@university.ac.uk", Expertise = "AI" };

        var result = await _supervisorService.CreateSupervisorAsync(dto);

        Assert.NotNull(result.Supervisor);
        Assert.Equal("Dr. Smith", result.Supervisor.Name);
        Assert.Equal("smith@university.ac.uk", result.Supervisor.Email);
    }

    [Fact]
    public async Task CreateSupervisorAsync_EmptyName_ThrowsArgumentException()
    {
        var dto = new CreateSupervisorDto { Name = "", Email = "test@test.com", Expertise = "AI" };

        await Assert.ThrowsAsync<ArgumentException>(() => _supervisorService.CreateSupervisorAsync(dto));
    }

    [Fact]
    public async Task CreateSupervisorAsync_EmptyEmail_ThrowsArgumentException()
    {
        var dto = new CreateSupervisorDto { Name = "Dr. Smith", Email = "", Expertise = "AI" };

        await Assert.ThrowsAsync<ArgumentException>(() => _supervisorService.CreateSupervisorAsync(dto));
    }

    [Fact]
    public async Task CreateSupervisorAsync_DuplicateEmail_ThrowsInvalidOperationException()
    {
        var existingUser = new User { Name = "Existing", Email = "test@test.com", Password = "hash", Role = "SUPERVISOR" };
        _dbContext.Users.Add(existingUser);
        await _dbContext.SaveChangesAsync();

        var dto = new CreateSupervisorDto { Name = "Dr. Smith", Email = "test@test.com", Expertise = "AI" };

        await Assert.ThrowsAsync<InvalidOperationException>(() => _supervisorService.CreateSupervisorAsync(dto));
    }

    [Fact]
    public async Task GetAllSupervisorsAsync_ReturnsPagedResults()
    {
        var user = new User { Name = "Test", Email = "test@test.com", Password = "hash", Role = "SUPERVISOR" };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();
        _dbContext.Supervisors.Add(new Supervisor { UserId = user.UserId });
        await _dbContext.SaveChangesAsync();

        var result = await _supervisorService.GetAllSupervisorsAsync(1, 10);

        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task GetSupervisorAsync_ExistingId_ReturnsSupervisor()
    {
        var user = new User { Name = "Test", Email = "test@test.com", Password = "hash", Role = "SUPERVISOR" };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();
        _dbContext.Supervisors.Add(new Supervisor { UserId = user.UserId });
        await _dbContext.SaveChangesAsync();

        var result = await _supervisorService.GetSupervisorAsync(user.UserId);

        Assert.Equal("Test", result.Name);
    }

    [Fact]
    public async Task GetSupervisorAsync_NonExistingId_ThrowsKeyNotFoundException()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _supervisorService.GetSupervisorAsync(999));
    }

    [Fact]
    public async Task UpdateSupervisorAsync_ValidUpdate_UpdatesSupervisor()
    {
        var user = new User { Name = "Test", Email = "test@test.com", Password = "hash", Role = "SUPERVISOR" };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();
        _dbContext.Supervisors.Add(new Supervisor { UserId = user.UserId });
        await _dbContext.SaveChangesAsync();

        var dto = new UpdateSupervisorDto { Name = "Updated Name" };
        var result = await _supervisorService.UpdateSupervisorAsync(user.UserId, dto);

        Assert.Equal("Updated Name", result.Name);
    }

    [Fact]
    public async Task DeleteSupervisorAsync_ExistingId_DeletesSupervisor()
    {
        var user = new User { Name = "Test", Email = "test@test.com", Password = "hash", Role = "SUPERVISOR" };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();
        _dbContext.Supervisors.Add(new Supervisor { UserId = user.UserId });
        await _dbContext.SaveChangesAsync();

        await _supervisorService.DeleteSupervisorAsync(user.UserId);

        var deleted = await _dbContext.Users.FindAsync(user.UserId);
        Assert.Null(deleted);
    }
}