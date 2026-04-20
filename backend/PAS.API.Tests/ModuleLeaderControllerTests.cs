using Microsoft.AspNetCore.Mvc;
using PAS.API.Controllers;
using PAS.API.DTOs.ModuleLeader;
using PAS.API.DTOs.Common;
using PAS.API.Services;
using Xunit;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Moq;

namespace PAS.API.Tests;

public class ModuleLeaderControllerTests : IAsyncLifetime
{
    private readonly Mock<IModuleLeaderService> _mockService;

    public ModuleLeaderControllerTests()
    {
        _mockService = new Mock<IModuleLeaderService>();
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => Task.CompletedTask;

    private ModuleLeaderController CreateController(int userId = 1)
    {
        var controller = new ModuleLeaderController(_mockService.Object);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Role, "ADMIN")
        };
        var identity = new ClaimsIdentity(claims);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
        return controller;
    }

    #region GetAllModuleLeaders Tests

    [Fact]
    public async Task GetAllModuleLeaders_WithNoModuleLeaders_ReturnsOk()
    {
        // Arrange
        _mockService.Setup(s => s.GetAllModuleLeadersAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new PagedResultDto<ModuleLeaderResponseDto>
            {
                Page = 1,
                PageSize = 10,
                TotalCount = 0,
                TotalPages = 0,
                Data = new List<ModuleLeaderResponseDto>()
            });

        var controller = CreateController();

        // Act
        var result = await controller.GetAllModuleLeaders();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetAllModuleLeaders_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        _mockService.Setup(s => s.GetAllModuleLeadersAsync(1, 3))
            .ReturnsAsync(new PagedResultDto<ModuleLeaderResponseDto>
            {
                Page = 1,
                PageSize = 3,
                TotalCount = 8,
                TotalPages = 3,
                Data = new List<ModuleLeaderResponseDto>
                {
                    new() { UserId = 1, Name = "ML 1", Email = "ml1@example.com" },
                    new() { UserId = 2, Name = "ML 2", Email = "ml2@example.com" },
                    new() { UserId = 3, Name = "ML 3", Email = "ml3@example.com" }
                }
            });

        var controller = CreateController();

        // Act
        var result = await controller.GetAllModuleLeaders(page: 1, pageSize: 3);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetAllModuleLeaders_WhenServiceThrows_Returns500()
    {
        // Arrange
        _mockService.Setup(s => s.GetAllModuleLeadersAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ThrowsAsync(new Exception("DB error"));

        var controller = CreateController();

        // Act
        var result = await controller.GetAllModuleLeaders();

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    #endregion

    #region GetModuleLeader Tests

    [Fact]
    public async Task GetModuleLeader_WithValidId_ReturnsModuleLeader()
    {
        // Arrange
        _mockService.Setup(s => s.GetModuleLeaderAsync(1))
            .ReturnsAsync(new ModuleLeaderResponseDto
            {
                UserId = 1,
                Name = "Dr. Johnson",
                Email = "dr.johnson@example.com"
            });

        var controller = CreateController();

        // Act
        var result = await controller.GetModuleLeader(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetModuleLeader_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        _mockService.Setup(s => s.GetModuleLeaderAsync(999))
            .ThrowsAsync(new KeyNotFoundException("Module leader not found."));

        var controller = CreateController();

        // Act
        var result = await controller.GetModuleLeader(999);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);
    }

    #endregion

    #region UpdateModuleLeader Tests

    [Fact]
    public async Task UpdateModuleLeader_WithValidId_ReturnsUpdatedModuleLeader()
    {
        // Arrange
        var dto = new UpdateModuleLeaderDto { Name = "Dr. Jane Johnson" };
        _mockService.Setup(s => s.UpdateModuleLeaderAsync(1, dto))
            .ReturnsAsync(new ModuleLeaderResponseDto
            {
                UserId = 1,
                Name = "Dr. Jane Johnson",
                Email = "dr.johnson@example.com"
            });

        var controller = CreateController();

        // Act
        var result = await controller.UpdateModuleLeader(1, dto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task UpdateModuleLeader_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var dto = new UpdateModuleLeaderDto { Name = "Dr. Jane Johnson" };
        _mockService.Setup(s => s.UpdateModuleLeaderAsync(999, dto))
            .ThrowsAsync(new KeyNotFoundException("Module leader not found."));

        var controller = CreateController();

        // Act
        var result = await controller.UpdateModuleLeader(999, dto);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);
    }

    [Fact]
    public async Task UpdateModuleLeader_WithDuplicateEmail_ReturnsConflict()
    {
        // Arrange
        var dto = new UpdateModuleLeaderDto { Email = "taken@example.com" };
        _mockService.Setup(s => s.UpdateModuleLeaderAsync(1, dto))
            .ThrowsAsync(new InvalidOperationException("Email already in use."));

        var controller = CreateController();

        // Act
        var result = await controller.UpdateModuleLeader(1, dto);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        Assert.NotNull(conflictResult.Value);
    }

    #endregion

    #region DeleteModuleLeader Tests

    [Fact]
    public async Task DeleteModuleLeader_WithValidId_ReturnsSuccess()
    {
        // Arrange
        _mockService.Setup(s => s.DeleteModuleLeaderAsync(1))
            .Returns(Task.CompletedTask);

        var controller = CreateController();

        // Act
        var result = await controller.DeleteModuleLeader(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task DeleteModuleLeader_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        _mockService.Setup(s => s.DeleteModuleLeaderAsync(999))
            .ThrowsAsync(new KeyNotFoundException("Module leader not found."));

        var controller = CreateController();

        // Act
        var result = await controller.DeleteModuleLeader(999);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);
    }

    #endregion

    #region DeactivateModuleLeader Tests

    [Fact]
    public async Task DeactivateModuleLeader_NotImplemented_ReturnsBadRequest()
    {
        // Arrange
        _mockService.Setup(s => s.DeactivateModuleLeaderAsync(1))
            .ThrowsAsync(new NotImplementedException("Deactivation is not supported."));

        var controller = CreateController();

        // Act
        var result = await controller.DeactivateModuleLeader(1);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
    }

    #endregion

    #region ReactivateModuleLeader Tests

    [Fact]
    public async Task ReactivateModuleLeader_NotImplemented_ReturnsBadRequest()
    {
        // Arrange
        _mockService.Setup(s => s.ReactivateModuleLeaderAsync(1))
            .ThrowsAsync(new NotImplementedException("Reactivation is not supported."));

        var controller = CreateController();

        // Act
        var result = await controller.ReactivateModuleLeader(1);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
    }

    #endregion

    #region ResetModuleLeaderPassword Tests

    [Fact]
    public async Task ResetModuleLeaderPassword_WithValidId_ReturnsSuccess()
    {
        // Arrange
        _mockService.Setup(s => s.ResetModuleLeaderPasswordAsync(1))
            .ReturnsAsync((true, true));

        var controller = CreateController();

        // Act
        var result = await controller.ResetModuleLeaderPassword(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task ResetModuleLeaderPassword_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        _mockService.Setup(s => s.ResetModuleLeaderPasswordAsync(999))
            .ThrowsAsync(new KeyNotFoundException("Module leader not found."));

        var controller = CreateController();

        // Act
        var result = await controller.ResetModuleLeaderPassword(999);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);
    }

    #endregion
}
