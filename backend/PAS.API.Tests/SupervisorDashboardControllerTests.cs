using Microsoft.AspNetCore.Mvc;
using PAS.API.Controllers;
using PAS.API.DTOs.Supervisor;
using PAS.API.Services;
using Xunit;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Moq;

namespace PAS.API.Tests;

public class SupervisorDashboardControllerTests : IAsyncLifetime
{
    private readonly Mock<ISupervisorDashboardService> _mockService;

    public SupervisorDashboardControllerTests()
    {
        _mockService = new Mock<ISupervisorDashboardService>();
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => Task.CompletedTask;

    private SupervisorDashboardController CreateController(int userId = 1)
    {
        var controller = new SupervisorDashboardController(_mockService.Object);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Role, "SUPERVISOR")
        };
        var identity = new ClaimsIdentity(claims);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
        return controller;
    }

    #region GetAvailableProjects Tests

    [Fact]
    public async Task GetAvailableProjects_WithNoProjects_ReturnsOkWithEmptyList()
    {
        // Arrange
        _mockService.Setup(s => s.GetAvailableProjectsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int?>()))
            .ReturnsAsync(new List<AnonymousProjectDto>());

        var controller = CreateController();

        // Act
        var result = await controller.GetAvailableProjects(courseworkId: 1, researchAreaId: null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetAvailableProjects_WithProjects_ReturnsOkWithProjects()
    {
        // Arrange
        var projects = new List<AnonymousProjectDto>
        {
            new() { ProjectId = 1, Title = "Test Project", Status = "Submitted" }
        };
        _mockService.Setup(s => s.GetAvailableProjectsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int?>()))
            .ReturnsAsync(projects);

        var controller = CreateController();

        // Act
        var result = await controller.GetAvailableProjects(courseworkId: 1, researchAreaId: null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetAvailableProjects_WhenServiceThrows_Returns500()
    {
        // Arrange
        _mockService.Setup(s => s.GetAvailableProjectsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int?>()))
            .ThrowsAsync(new Exception("DB error"));

        var controller = CreateController();

        // Act
        var result = await controller.GetAvailableProjects(courseworkId: 1, researchAreaId: null);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    #endregion

    #region GetProposalPdf Tests

    [Fact]
    public async Task GetProposalPdf_WithValidProject_ReturnsFile()
    {
        // Arrange
        var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        _mockService.Setup(s => s.GetProposalPdfAsync(It.IsAny<int>()))
            .ReturnsAsync((stream, "application/pdf", "test.pdf"));

        var controller = CreateController();

        // Act
        var result = await controller.GetProposalPdf(1);

        // Assert
        var fileResult = Assert.IsType<FileStreamResult>(result);
        Assert.Equal("application/pdf", fileResult.ContentType);
        Assert.Equal("test.pdf", fileResult.FileDownloadName);
    }

    [Fact]
    public async Task GetProposalPdf_WithInvalidProject_ReturnsNotFound()
    {
        // Arrange
        _mockService.Setup(s => s.GetProposalPdfAsync(It.IsAny<int>()))
            .ThrowsAsync(new KeyNotFoundException("Project not found."));

        var controller = CreateController();

        // Act
        var result = await controller.GetProposalPdf(999);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);
    }

    [Fact]
    public async Task GetProposalPdf_WithNoPdf_ReturnsBadRequest()
    {
        // Arrange
        _mockService.Setup(s => s.GetProposalPdfAsync(It.IsAny<int>()))
            .ThrowsAsync(new InvalidOperationException("No PDF uploaded."));

        var controller = CreateController();

        // Act
        var result = await controller.GetProposalPdf(1);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
    }

    #endregion

    #region ExpressInterest Tests

    [Fact]
    public async Task ExpressInterest_WithValidProject_ReturnsOk()
    {
        // Arrange
        _mockService.Setup(s => s.ExpressInterestAsync(It.IsAny<int>(), It.IsAny<int>()))
            .Returns(Task.CompletedTask);

        var controller = CreateController();

        // Act
        var result = await controller.ExpressInterest(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task ExpressInterest_WithInvalidProject_ReturnsNotFound()
    {
        // Arrange
        _mockService.Setup(s => s.ExpressInterestAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ThrowsAsync(new KeyNotFoundException("Project not found."));

        var controller = CreateController();

        // Act
        var result = await controller.ExpressInterest(999);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);
    }

    [Fact]
    public async Task ExpressInterest_WithAlreadyInterestedProject_ReturnsConflict()
    {
        // Arrange
        _mockService.Setup(s => s.ExpressInterestAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ThrowsAsync(new InvalidOperationException("Already expressed interest."));

        var controller = CreateController();

        // Act
        var result = await controller.ExpressInterest(1);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        Assert.NotNull(conflictResult.Value);
    }

    #endregion

    #region WithdrawInterest Tests

    [Fact]
    public async Task WithdrawInterest_WithInterestedProject_ReturnsOk()
    {
        // Arrange
        _mockService.Setup(s => s.WithdrawInterestAsync(It.IsAny<int>(), It.IsAny<int>()))
            .Returns(Task.CompletedTask);

        var controller = CreateController();

        // Act
        var result = await controller.WithdrawInterest(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task WithdrawInterest_WithInvalidProject_ReturnsNotFound()
    {
        // Arrange
        _mockService.Setup(s => s.WithdrawInterestAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ThrowsAsync(new KeyNotFoundException("No interest record found."));

        var controller = CreateController();

        // Act
        var result = await controller.WithdrawInterest(1);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);
    }

    [Fact]
    public async Task WithdrawInterest_WhenServiceThrows_Returns500()
    {
        // Arrange
        _mockService.Setup(s => s.WithdrawInterestAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ThrowsAsync(new Exception("Unexpected error"));

        var controller = CreateController();

        // Act
        var result = await controller.WithdrawInterest(1);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    #endregion

    #region GetSubmissions Tests

    [Fact]
    public async Task GetSubmissions_ReturnsList()
    {
        // Arrange
        _mockService.Setup(s => s.GetSubmissionsAsync(It.IsAny<int>(), It.IsAny<List<int>?>()))
            .ReturnsAsync(new SupervisorSubmissionsDto
            {
                MatchedProjects = new List<AnonymousProjectDto>(),
                PendingReviews = new List<AnonymousProjectDto>()
            });

        var controller = CreateController();

        // Act
        var result = await controller.GetSubmissions(researchAreaIds: null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetSubmissions_WhenServiceThrows_Returns500()
    {
        // Arrange
        _mockService.Setup(s => s.GetSubmissionsAsync(It.IsAny<int>(), It.IsAny<List<int>?>()))
            .ThrowsAsync(new Exception("DB error"));

        var controller = CreateController();

        // Act
        var result = await controller.GetSubmissions(researchAreaIds: null);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    #endregion

    #region GetMatchedRevealed Tests

    [Fact]
    public async Task GetMatchedRevealed_ReturnsList()
    {
        // Arrange
        _mockService.Setup(s => s.GetMatchedProjectsWithStudentInfoAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<RevealedProjectDto>());

        var controller = CreateController();

        // Act
        var result = await controller.GetMatchedRevealed();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetMatchedRevealed_WhenServiceThrows_Returns500()
    {
        // Arrange
        _mockService.Setup(s => s.GetMatchedProjectsWithStudentInfoAsync(It.IsAny<int>()))
            .ThrowsAsync(new Exception("DB error"));

        var controller = CreateController();

        // Act
        var result = await controller.GetMatchedRevealed();

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    #endregion

    #region GetMatchHistory Tests

    [Fact]
    public async Task GetMatchHistory_ReturnsList()
    {
        // Arrange
        _mockService.Setup(s => s.GetMatchHistoryAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<MatchedHistoryDto>());

        var controller = CreateController();

        // Act
        var result = await controller.GetMatchHistory();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetMatchHistory_WhenServiceThrows_Returns500()
    {
        // Arrange
        _mockService.Setup(s => s.GetMatchHistoryAsync(It.IsAny<int>()))
            .ThrowsAsync(new Exception("DB error"));

        var controller = CreateController();

        // Act
        var result = await controller.GetMatchHistory();

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    #endregion
}
