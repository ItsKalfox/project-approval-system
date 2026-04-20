using Moq;
using Xunit;
using PAS.API.Controllers;
using PAS.API.DTOs.ResearchArea;
using PAS.API.Models;
using PAS.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace PAS.API.Tests;

/// <summary>
/// Unit tests for ResearchAreaController using Moq to mock the service.
/// These tests verify controller behavior and error handling independently
/// of the service implementation.
/// </summary>
public class ResearchAreaControllerTests
{
    private readonly Mock<IResearchAreaService> _mockService = new();
    
    private ResearchAreaController CreateController()
    {
        return new ResearchAreaController(_mockService.Object);
    }

    #region CreateResearchArea Tests

    [Fact]
    public async Task CreateResearchArea_WithValidDto_Returns201Created()
    {
        // Arrange
        var dto = new CreateResearchAreaDto { Name = "AI" };
        var responseDto = new ResearchAreaResponseDto { Id = 1, Name = "AI" };
        
        _mockService
            .Setup(s => s.CreateResearchAreaAsync(It.IsAny<CreateResearchAreaDto>()))
            .ReturnsAsync(responseDto);

        var controller = CreateController();

        // Act
        var result = await controller.CreateResearchArea(dto);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status201Created, statusResult.StatusCode);
        
        _mockService.Verify(s => s.CreateResearchAreaAsync(dto), Times.Once);
    }

    [Fact]
    public async Task CreateResearchArea_WithEmptyName_ReturnsBadRequest()
    {
        // Arrange
        var dto = new CreateResearchAreaDto { Name = "" };
        
        _mockService
            .Setup(s => s.CreateResearchAreaAsync(It.IsAny<CreateResearchAreaDto>()))
            .ThrowsAsync(new ArgumentException("'Name' is required."));

        var controller = CreateController();

        // Act
        var result = await controller.CreateResearchArea(dto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task CreateResearchArea_WithDuplicateName_ReturnsConflict()
    {
        // Arrange
        var dto = new CreateResearchAreaDto { Name = "Existing" };
        
        _mockService
            .Setup(s => s.CreateResearchAreaAsync(It.IsAny<CreateResearchAreaDto>()))
            .ThrowsAsync(new InvalidOperationException("Research area 'Existing' already exists."));

        var controller = CreateController();

        // Act
        var result = await controller.CreateResearchArea(dto);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, conflictResult.StatusCode);
    }

    [Fact]
    public async Task CreateResearchArea_WithUnexpectedException_Returns500InternalServerError()
    {
        // Arrange
        var dto = new CreateResearchAreaDto { Name = "Test" };
        
        _mockService
            .Setup(s => s.CreateResearchAreaAsync(It.IsAny<CreateResearchAreaDto>()))
            .ThrowsAsync(new Exception("Unexpected error"));

        var controller = CreateController();

        // Act
        var result = await controller.CreateResearchArea(dto);

        // Assert
        var serverErrorResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, serverErrorResult.StatusCode);
    }

    #endregion

    #region GetAllResearchAreas Tests

    [Fact]
    public async Task GetAllResearchAreas_ReturnsOkWithData()
    {
        // Arrange
        var areas = new List<ResearchAreaResponseDto>
        {
            new() { Id = 1, Name = "ML" },
            new() { Id = 2, Name = "AI" }
        };

        _mockService
            .Setup(s => s.GetAllResearchAreasAsync())
            .ReturnsAsync(areas);

        var controller = CreateController();

        // Act
        var result = await controller.GetAllResearchAreas();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        
        _mockService.Verify(s => s.GetAllResearchAreasAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAllResearchAreas_WithException_Returns500InternalServerError()
    {
        // Arrange
        _mockService
            .Setup(s => s.GetAllResearchAreasAsync())
            .ThrowsAsync(new Exception("Database error"));

        var controller = CreateController();

        // Act
        var result = await controller.GetAllResearchAreas();

        // Assert
        var serverErrorResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, serverErrorResult.StatusCode);
    }

    #endregion

    #region GetResearchArea Tests

    [Fact]
    public async Task GetResearchArea_WithValidId_ReturnsOkWithData()
    {
        // Arrange
        var responseDto = new ResearchAreaResponseDto { Id = 1, Name = "Data Science" };
        
        _mockService
            .Setup(s => s.GetResearchAreaAsync(It.IsAny<int>()))
            .ReturnsAsync(responseDto);

        var controller = CreateController();

        // Act
        var result = await controller.GetResearchArea(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        
        _mockService.Verify(s => s.GetResearchAreaAsync(1), Times.Once);
    }

    [Fact]
    public async Task GetResearchArea_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        _mockService
            .Setup(s => s.GetResearchAreaAsync(It.IsAny<int>()))
            .ThrowsAsync(new KeyNotFoundException("Research area with ID '999' was not found."));

        var controller = CreateController();

        // Act
        var result = await controller.GetResearchArea(999);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
    }

    #endregion

    #region UpdateResearchArea Tests

    [Fact]
    public async Task UpdateResearchArea_WithValidData_ReturnsOkWithUpdatedData()
    {
        // Arrange
        var dto = new UpdateResearchAreaDto { Name = "Updated Name" };
        var responseDto = new ResearchAreaResponseDto { Id = 1, Name = "Updated Name" };
        
        _mockService
            .Setup(s => s.UpdateResearchAreaAsync(It.IsAny<int>(), It.IsAny<UpdateResearchAreaDto>()))
            .ReturnsAsync(responseDto);

        var controller = CreateController();

        // Act
        var result = await controller.UpdateResearchArea(1, dto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        
        _mockService.Verify(s => s.UpdateResearchAreaAsync(1, dto), Times.Once);
    }

    [Fact]
    public async Task UpdateResearchArea_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var dto = new UpdateResearchAreaDto { Name = "New Name" };
        
        _mockService
            .Setup(s => s.UpdateResearchAreaAsync(It.IsAny<int>(), It.IsAny<UpdateResearchAreaDto>()))
            .ThrowsAsync(new KeyNotFoundException("Research area with ID '999' was not found."));

        var controller = CreateController();

        // Act
        var result = await controller.UpdateResearchArea(999, dto);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
    }

    [Fact]
    public async Task UpdateResearchArea_WithDuplicateName_ReturnsConflict()
    {
        // Arrange
        var dto = new UpdateResearchAreaDto { Name = "Existing Name" };
        
        _mockService
            .Setup(s => s.UpdateResearchAreaAsync(It.IsAny<int>(), It.IsAny<UpdateResearchAreaDto>()))
            .ThrowsAsync(new InvalidOperationException("Research area 'Existing Name' already exists."));

        var controller = CreateController();

        // Act
        var result = await controller.UpdateResearchArea(1, dto);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, conflictResult.StatusCode);
    }

    #endregion

    #region DeleteResearchArea Tests

    [Fact]
    public async Task DeleteResearchArea_WithValidId_ReturnsOk()
    {
        // Arrange
        _mockService
            .Setup(s => s.DeleteResearchAreaAsync(It.IsAny<int>()))
            .Returns(Task.CompletedTask);

        var controller = CreateController();

        // Act
        var result = await controller.DeleteResearchArea(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        
        _mockService.Verify(s => s.DeleteResearchAreaAsync(1), Times.Once);
    }

    [Fact]
    public async Task DeleteResearchArea_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        _mockService
            .Setup(s => s.DeleteResearchAreaAsync(It.IsAny<int>()))
            .ThrowsAsync(new KeyNotFoundException("Research area with ID '999' was not found."));

        var controller = CreateController();

        // Act
        var result = await controller.DeleteResearchArea(999);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
    }

    [Fact]
    public async Task DeleteResearchArea_WithException_Returns500InternalServerError()
    {
        // Arrange
        _mockService
            .Setup(s => s.DeleteResearchAreaAsync(It.IsAny<int>()))
            .ThrowsAsync(new Exception("Database error"));

        var controller = CreateController();

        // Act
        var result = await controller.DeleteResearchArea(1);

        // Assert
        var serverErrorResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, serverErrorResult.StatusCode);
    }

    #endregion
}

