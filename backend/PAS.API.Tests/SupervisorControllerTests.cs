using Microsoft.AspNetCore.Mvc;
using PAS.API.Controllers;
using PAS.API.DTOs.Supervisor;
using PAS.API.Services;
using Xunit;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Moq;
using Microsoft.EntityFrameworkCore;
using PAS.API.Data;
using PAS.API.Models;

namespace PAS.API.Tests;

public class SupervisorControllerTests : IAsyncLifetime
{
    private readonly DbContextOptions<PASDbContext> _dbOptions;

    public SupervisorControllerTests()
    {
        _dbOptions = new DbContextOptionsBuilder<PASDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => Task.CompletedTask;

    private ISupervisorService CreateSupervisorService()
    {
        var context = new PASDbContext(_dbOptions);
        var emailSender = new Mock<IEmailService>();
        return new SupervisorService(context, emailSender.Object);
    }

    private SupervisorController CreateController(ISupervisorService supervisorService, int userId = 1)
    {
        var controller = new SupervisorController(supervisorService);
        var claims = new List<Claim>
        {
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

    #region CreateSupervisor Tests

    [Fact]
    public async Task CreateSupervisor_WithValidDto_ReturnsCreated()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var emailSender = new Mock<IEmailService>();
        var service = new SupervisorService(context, emailSender.Object);
        var controller = CreateController(service);
        var dto = new CreateSupervisorDto
        {
            Name = "Dr. Smith",
            Email = "dr.smith@example.com",
            Expertise = "AI"
        };

        // Act
        var result = await controller.CreateSupervisor(dto);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status201Created, objectResult.StatusCode);
    }

    [Fact]
    public async Task CreateSupervisor_WithDuplicateEmail_ReturnsConflict()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var emailSender = new Mock<IEmailService>();
        var service = new SupervisorService(context, emailSender.Object);
        var controller = CreateController(service);
        var dto = new CreateSupervisorDto
        {
            Name = "Dr. Smith",
            Email = "dr.smith2@example.com",
            Expertise = "AI"
        };

        // Act - first call succeeds
        await controller.CreateSupervisor(dto);
        // Second call with same email should fail
        var result = await controller.CreateSupervisor(dto);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        Assert.NotNull(conflictResult.Value);
    }

    [Fact]
    public async Task CreateSupervisor_WithMissingName_ReturnsBadRequest()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var emailSender = new Mock<IEmailService>();
        var service = new SupervisorService(context, emailSender.Object);
        var controller = CreateController(service);
        var dto = new CreateSupervisorDto
        {
            Name = "",
            Email = "dr.smith3@example.com",
            Expertise = "AI"
        };

        // Act
        var result = await controller.CreateSupervisor(dto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
    }

    #endregion

    #region GetAllSupervisors Tests

    [Fact]
    public async Task GetAllSupervisors_WithNoSupervisors_ReturnsEmptyList()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var emailSender = new Mock<IEmailService>();
        var service = new SupervisorService(context, emailSender.Object);
        var controller = CreateController(service);

        // Act
        var result = await controller.GetAllSupervisors();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetAllSupervisors_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var emailSender = new Mock<IEmailService>();
        var service = new SupervisorService(context, emailSender.Object);
        var controller = CreateController(service);

        // Add test supervisors
        for (int i = 1; i <= 10; i++)
        {
            await service.CreateSupervisorAsync(new CreateSupervisorDto
            {
                Name = $"Supervisor {i}",
                Email = $"supervisor{i}@example.com",
                Expertise = "AI"
            });
        }

        // Act
        var result = await controller.GetAllSupervisors(page: 1, pageSize: 3);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    #endregion

    #region GetSupervisor Tests

    [Fact]
    public async Task GetSupervisor_WithValidId_ReturnsSupervisor()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var emailSender = new Mock<IEmailService>();
        var service = new SupervisorService(context, emailSender.Object);
        var controller = CreateController(service);

        var (supervisor, _) = await service.CreateSupervisorAsync(new CreateSupervisorDto
        {
            Name = "Dr. Smith",
            Email = "dr.smith.get@example.com",
            Expertise = "ML"
        });

        // Act
        var result = await controller.GetSupervisor(supervisor.UserId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetSupervisor_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var emailSender = new Mock<IEmailService>();
        var service = new SupervisorService(context, emailSender.Object);
        var controller = CreateController(service);

        // Act
        var result = await controller.GetSupervisor(999);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);
    }

    #endregion

    #region UpdateSupervisor Tests

    [Fact]
    public async Task UpdateSupervisor_WithValidId_ReturnsUpdatedSupervisor()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var emailSender = new Mock<IEmailService>();
        var service = new SupervisorService(context, emailSender.Object);
        var controller = CreateController(service);

        var (supervisor, _) = await service.CreateSupervisorAsync(new CreateSupervisorDto
        {
            Name = "Dr. Smith",
            Email = "dr.smith.upd@example.com",
            Expertise = "ML"
        });

        var dto = new UpdateSupervisorDto { Name = "Dr. John Smith" };

        // Act
        var result = await controller.UpdateSupervisor(supervisor.UserId, dto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task UpdateSupervisor_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var emailSender = new Mock<IEmailService>();
        var service = new SupervisorService(context, emailSender.Object);
        var controller = CreateController(service);

        var dto = new UpdateSupervisorDto { Name = "Dr. John Smith" };

        // Act
        var result = await controller.UpdateSupervisor(999, dto);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);
    }

    #endregion

    #region DeleteSupervisor Tests

    [Fact]
    public async Task DeleteSupervisor_WithValidId_ReturnsSuccess()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var emailSender = new Mock<IEmailService>();
        var service = new SupervisorService(context, emailSender.Object);
        var controller = CreateController(service);

        var (supervisor, _) = await service.CreateSupervisorAsync(new CreateSupervisorDto
        {
            Name = "Dr. Smith",
            Email = "dr.smith.del@example.com",
            Expertise = "ML"
        });

        // Act
        var result = await controller.DeleteSupervisor(supervisor.UserId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task DeleteSupervisor_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var emailSender = new Mock<IEmailService>();
        var service = new SupervisorService(context, emailSender.Object);
        var controller = CreateController(service);

        // Act
        var result = await controller.DeleteSupervisor(999);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);
    }

    #endregion

    #region DeactivateSupervisor Tests

    [Fact]
    public async Task DeactivateSupervisor_WithValidId_ReturnsSuccess()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var emailSender = new Mock<IEmailService>();
        var service = new SupervisorService(context, emailSender.Object);
        var controller = CreateController(service);

        var (supervisor, _) = await service.CreateSupervisorAsync(new CreateSupervisorDto
        {
            Name = "Dr. Smith",
            Email = "dr.smith.deact@example.com",
            Expertise = "ML"
        });

        // Act
        var result = await controller.DeactivateSupervisor(supervisor.UserId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task DeactivateSupervisor_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var emailSender = new Mock<IEmailService>();
        var service = new SupervisorService(context, emailSender.Object);
        var controller = CreateController(service);

        // Act
        var result = await controller.DeactivateSupervisor(999);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);
    }

    #endregion

    #region ReactivateSupervisor Tests

    [Fact]
    public async Task ReactivateSupervisor_WithDeactivatedId_ReturnsSuccess()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var emailSender = new Mock<IEmailService>();
        var service = new SupervisorService(context, emailSender.Object);
        var controller = CreateController(service);

        var (supervisor, _) = await service.CreateSupervisorAsync(new CreateSupervisorDto
        {
            Name = "Dr. Smith",
            Email = "dr.smith.react@example.com",
            Expertise = "ML"
        });

        // Deactivate first
        await service.DeactivateSupervisorAsync(supervisor.UserId);

        // Act
        var result = await controller.ReactivateSupervisor(supervisor.UserId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task ReactivateSupervisor_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var emailSender = new Mock<IEmailService>();
        var service = new SupervisorService(context, emailSender.Object);
        var controller = CreateController(service);

        // Act
        var result = await controller.ReactivateSupervisor(999);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);
    }

    #endregion

    #region ResetSupervisorPassword Tests

    [Fact]
    public async Task ResetSupervisorPassword_WithValidId_ReturnsSuccess()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var emailSender = new Mock<IEmailService>();
        var service = new SupervisorService(context, emailSender.Object);
        var controller = CreateController(service);

        var (supervisor, _) = await service.CreateSupervisorAsync(new CreateSupervisorDto
        {
            Name = "Dr. Smith",
            Email = "dr.smith.reset@example.com",
            Expertise = "ML"
        });

        // Act
        var result = await controller.ResetSupervisorPassword(supervisor.UserId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task ResetSupervisorPassword_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var emailSender = new Mock<IEmailService>();
        var service = new SupervisorService(context, emailSender.Object);
        var controller = CreateController(service);

        // Act
        var result = await controller.ResetSupervisorPassword(999);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);
    }

    #endregion
}
