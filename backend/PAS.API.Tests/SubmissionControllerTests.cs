using Microsoft.AspNetCore.Mvc;
using PAS.API.Controllers;
using PAS.API.DTOs.Submission;
using PAS.API.DTOs.ResearchArea;
using PAS.API.Services;
using Xunit;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Moq;
using System.Text;

namespace PAS.API.Tests;

public class SubmissionControllerTests : IAsyncLifetime
{
    private readonly Mock<ISubmissionService> _mockSubmissionService;
    private readonly Mock<IResearchAreaService> _mockResearchAreaService;

    public SubmissionControllerTests()
    {
        _mockSubmissionService = new Mock<ISubmissionService>();
        _mockResearchAreaService = new Mock<IResearchAreaService>();
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => Task.CompletedTask;

    private SubmissionController CreateController(int userId = 1)
    {
        var controller = new SubmissionController(_mockSubmissionService.Object, _mockResearchAreaService.Object);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Role, "STUDENT")
        };
        var identity = new ClaimsIdentity(claims);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
        return controller;
    }

    #region GetSubmissionPoints Tests

    [Fact]
    public async Task GetSubmissionPoints_ReturnsList()
    {
        // Arrange
        _mockSubmissionService.Setup(s => s.GetSubmissionPointsAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<SubmissionPointDto>());

        var controller = CreateController();

        // Act
        var result = await controller.GetSubmissionPoints();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetSubmissionPoints_WhenUnauthorized_ReturnsUnauthorized()
    {
        // Arrange
        _mockSubmissionService.Setup(s => s.GetSubmissionPointsAsync(It.IsAny<int>()))
            .ThrowsAsync(new UnauthorizedAccessException("Not authorized"));

        var controller = CreateController();

        // Act
        var result = await controller.GetSubmissionPoints();

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.NotNull(unauthorizedResult.Value);
    }

    #endregion

    #region GetMySubmissions Tests

    [Fact]
    public async Task GetMySubmissions_WithNoSubmissions_ReturnsEmptyList()
    {
        // Arrange
        _mockSubmissionService.Setup(s => s.GetStudentSubmissionsAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<SubmissionResponseDto>());

        var controller = CreateController();

        // Act
        var result = await controller.GetMySubmissions();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetMySubmissions_WithSubmissions_ReturnsSubmissions()
    {
        // Arrange
        _mockSubmissionService.Setup(s => s.GetStudentSubmissionsAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<SubmissionResponseDto>
            {
                new() { ProjectId = 1, Title = "Project 1", Status = "Submitted" }
            });

        var controller = CreateController();

        // Act
        var result = await controller.GetMySubmissions();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetMySubmissions_WhenServiceThrows_Returns500()
    {
        // Arrange
        _mockSubmissionService.Setup(s => s.GetStudentSubmissionsAsync(It.IsAny<int>()))
            .ThrowsAsync(new Exception("DB error"));

        var controller = CreateController();

        // Act
        var result = await controller.GetMySubmissions();

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    #endregion

    #region GetAvailableGroups Tests

    [Fact]
    public async Task GetAvailableGroups_WithValidCoursework_ReturnsGroups()
    {
        // Arrange
        _mockSubmissionService.Setup(s => s.GetAvailableGroupsAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<AvailableGroupDto>
            {
                new() { GroupId = 1, GroupName = "Group A", CurrentMembers = 2, MaxMembers = 5 }
            });

        var controller = CreateController();

        // Act
        var result = await controller.GetAvailableGroups(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetAvailableGroups_WithInvalidCoursework_ReturnsNotFound()
    {
        // Arrange
        _mockSubmissionService.Setup(s => s.GetAvailableGroupsAsync(It.IsAny<int>()))
            .ThrowsAsync(new KeyNotFoundException("Coursework not found"));

        var controller = CreateController();

        // Act
        var result = await controller.GetAvailableGroups(999);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);
    }

    #endregion

    #region GetResearchAreas Tests

    [Fact]
    public async Task GetResearchAreas_ReturnsList()
    {
        // Arrange
        _mockResearchAreaService.Setup(s => s.GetAllResearchAreasAsync())
            .ReturnsAsync(new List<ResearchAreaResponseDto>
            {
                new() { Id = 1, Name = "AI" }
            });

        var controller = CreateController();

        // Act
        var result = await controller.GetResearchAreas();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetResearchAreas_WhenServiceThrows_Returns500()
    {
        // Arrange
        _mockResearchAreaService.Setup(s => s.GetAllResearchAreasAsync())
            .ThrowsAsync(new Exception("DB error"));

        var controller = CreateController();

        // Act
        var result = await controller.GetResearchAreas();

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    #endregion

    #region GetMySubmission Tests

    [Fact]
    public async Task GetMySubmission_WithNoSubmission_ReturnsNotFound()
    {
        // Arrange
        _mockSubmissionService.Setup(s => s.GetMySubmissionAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync((SubmissionResponseDto?)null);

        var controller = CreateController();

        // Act
        var result = await controller.GetMySubmission(1);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);
    }

    [Fact]
    public async Task GetMySubmission_WithSubmission_ReturnsOk()
    {
        // Arrange
        _mockSubmissionService.Setup(s => s.GetMySubmissionAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new SubmissionResponseDto { ProjectId = 1, Title = "AI Project", Status = "Submitted" });

        var controller = CreateController();

        // Act
        var result = await controller.GetMySubmission(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetMySubmission_WhenCourseworkNotFound_ReturnsNotFound()
    {
        // Arrange
        _mockSubmissionService.Setup(s => s.GetMySubmissionAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ThrowsAsync(new KeyNotFoundException("Coursework not found."));

        var controller = CreateController();

        // Act
        var result = await controller.GetMySubmission(999);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);
    }

    #endregion

    #region CreateSubmission Tests

    [Fact]
    public async Task CreateSubmission_WithValidData_ReturnsCreated()
    {
        // Arrange
        _mockSubmissionService.Setup(s => s.CreateSubmissionAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CreateSubmissionDto>(), It.IsAny<IFormFile>()))
            .ReturnsAsync(new SubmissionResponseDto { ProjectId = 1, Title = "AI Project", Status = "Submitted" });

        var controller = CreateController();

        var dto = new CreateSubmissionDto
        {
            Title = "AI Project",
            Abstract = "Test abstract",
            Description = "Test description",
            ResearchAreaId = 1
        };
        var file = new FormFile(new MemoryStream(Encoding.UTF8.GetBytes("test")), 0, 4, "file", "test.pdf")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/pdf"
        };

        // Act
        var result = await controller.CreateSubmission(1, dto, file);

        // Assert
        var createdResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status201Created, createdResult.StatusCode);
    }

    [Fact]
    public async Task CreateSubmission_WithInvalidCoursework_ReturnsNotFound()
    {
        // Arrange
        _mockSubmissionService.Setup(s => s.CreateSubmissionAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CreateSubmissionDto>(), It.IsAny<IFormFile>()))
            .ThrowsAsync(new KeyNotFoundException("Coursework not found"));

        var controller = CreateController();

        var dto = new CreateSubmissionDto { Title = "AI", Abstract = "Test", Description = "Test", ResearchAreaId = 1 };
        var file = new FormFile(new MemoryStream(Encoding.UTF8.GetBytes("test")), 0, 4, "file", "test.pdf");

        // Act
        var result = await controller.CreateSubmission(999, dto, file);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);
    }

    [Fact]
    public async Task CreateSubmission_WithBadFile_ReturnsBadRequest()
    {
        // Arrange
        _mockSubmissionService.Setup(s => s.CreateSubmissionAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CreateSubmissionDto>(), It.IsAny<IFormFile>()))
            .ThrowsAsync(new ArgumentException("Only PDF files are allowed."));

        var controller = CreateController();

        var dto = new CreateSubmissionDto { Title = "AI", Abstract = "Test", Description = "Test", ResearchAreaId = 1 };
        var file = new FormFile(new MemoryStream(Encoding.UTF8.GetBytes("test")), 0, 4, "file", "test.txt");

        // Act
        var result = await controller.CreateSubmission(1, dto, file);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task CreateSubmission_AlreadySubmitted_ReturnsConflict()
    {
        // Arrange
        _mockSubmissionService.Setup(s => s.CreateSubmissionAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CreateSubmissionDto>(), It.IsAny<IFormFile>()))
            .ThrowsAsync(new InvalidOperationException("Already submitted."));

        var controller = CreateController();

        var dto = new CreateSubmissionDto { Title = "AI", Abstract = "Test", Description = "Test", ResearchAreaId = 1 };
        var file = new FormFile(new MemoryStream(Encoding.UTF8.GetBytes("test")), 0, 4, "file", "test.pdf");

        // Act
        var result = await controller.CreateSubmission(1, dto, file);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        Assert.NotNull(conflictResult.Value);
    }

    #endregion

    #region UpdateSubmission Tests

    [Fact]
    public async Task UpdateSubmission_WithValidData_ReturnsOk()
    {
        // Arrange
        _mockSubmissionService.Setup(s => s.UpdateSubmissionAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<UpdateSubmissionDto>(), It.IsAny<IFormFile?>()))
            .ReturnsAsync(new SubmissionResponseDto { ProjectId = 1, Title = "Updated Title", Status = "Submitted" });

        var controller = CreateController();

        var dto = new UpdateSubmissionDto { Title = "Updated Title", Abstract = "Updated abstract" };

        // Act
        var result = await controller.UpdateSubmission(1, dto, null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task UpdateSubmission_WithInvalidProject_ReturnsNotFound()
    {
        // Arrange
        _mockSubmissionService.Setup(s => s.UpdateSubmissionAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<UpdateSubmissionDto>(), It.IsAny<IFormFile?>()))
            .ThrowsAsync(new KeyNotFoundException("Submission not found"));

        var controller = CreateController();

        var dto = new UpdateSubmissionDto { Title = "Updated Title" };

        // Act
        var result = await controller.UpdateSubmission(999, dto, null);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);
    }

    [Fact]
    public async Task UpdateSubmission_DeadlinePassed_ReturnsConflict()
    {
        // Arrange
        _mockSubmissionService.Setup(s => s.UpdateSubmissionAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<UpdateSubmissionDto>(), It.IsAny<IFormFile?>()))
            .ThrowsAsync(new InvalidOperationException("The submission deadline has passed."));

        var controller = CreateController();

        var dto = new UpdateSubmissionDto { Title = "Updated Title" };

        // Act
        var result = await controller.UpdateSubmission(1, dto, null);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        Assert.NotNull(conflictResult.Value);
    }

    #endregion

    #region DeleteSubmission Tests

    [Fact]
    public async Task DeleteSubmission_WithValidProject_ReturnsOk()
    {
        // Arrange
        _mockSubmissionService.Setup(s => s.DeleteSubmissionAsync(It.IsAny<int>(), It.IsAny<int>()))
            .Returns(Task.CompletedTask);

        var controller = CreateController();

        // Act
        var result = await controller.DeleteSubmission(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task DeleteSubmission_WithInvalidProject_ReturnsNotFound()
    {
        // Arrange
        _mockSubmissionService.Setup(s => s.DeleteSubmissionAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ThrowsAsync(new KeyNotFoundException("Submission not found"));

        var controller = CreateController();

        // Act
        var result = await controller.DeleteSubmission(999);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);
    }

    [Fact]
    public async Task DeleteSubmission_DeadlinePassed_ReturnsConflict()
    {
        // Arrange
        _mockSubmissionService.Setup(s => s.DeleteSubmissionAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ThrowsAsync(new InvalidOperationException("Deadline passed."));

        var controller = CreateController();

        // Act
        var result = await controller.DeleteSubmission(1);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        Assert.NotNull(conflictResult.Value);
    }

    [Fact]
    public async Task DeleteSubmission_Unauthorized_ReturnsUnauthorized()
    {
        // Arrange
        _mockSubmissionService.Setup(s => s.DeleteSubmissionAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ThrowsAsync(new UnauthorizedAccessException("Not your submission."));

        var controller = CreateController();

        // Act
        var result = await controller.DeleteSubmission(1);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.NotNull(unauthorizedResult.Value);
    }

    #endregion

    #region GetSubmissionFile Tests

    [Fact]
    public async Task GetSubmissionFile_WithValidProject_ReturnsFile()
    {
        // Arrange
        var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        _mockSubmissionService.Setup(s => s.GetSubmissionFileAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync((stream, "application/pdf", "proposal.pdf"));

        var controller = CreateController();

        // Act
        var result = await controller.GetSubmissionFile(1);

        // Assert
        var fileResult = Assert.IsType<FileStreamResult>(result);
        Assert.Equal("application/pdf", fileResult.ContentType);
        Assert.Equal("proposal.pdf", fileResult.FileDownloadName);
    }

    [Fact]
    public async Task GetSubmissionFile_WithInvalidProject_ReturnsNotFound()
    {
        // Arrange
        _mockSubmissionService.Setup(s => s.GetSubmissionFileAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ThrowsAsync(new KeyNotFoundException("Submission not found"));

        var controller = CreateController();

        // Act
        var result = await controller.GetSubmissionFile(999);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);
    }

    [Fact]
    public async Task GetSubmissionFile_NoFileUploaded_ReturnsBadRequest()
    {
        // Arrange
        _mockSubmissionService.Setup(s => s.GetSubmissionFileAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ThrowsAsync(new InvalidOperationException("No file uploaded."));

        var controller = CreateController();

        // Act
        var result = await controller.GetSubmissionFile(1);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
    }

    #endregion
}
