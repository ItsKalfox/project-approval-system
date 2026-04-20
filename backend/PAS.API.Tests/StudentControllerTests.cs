using Microsoft.AspNetCore.Mvc;
using PAS.API.Controllers;
using PAS.API.DTOs.Student;
using PAS.API.Services;
using Xunit;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Moq;
using Microsoft.EntityFrameworkCore;
using PAS.API.Data;

namespace PAS.API.Tests;

public class StudentControllerTests : IAsyncLifetime
{
    private readonly DbContextOptions<PASDbContext> _dbOptions;

    public StudentControllerTests()
    {
        _dbOptions = new DbContextOptionsBuilder<PASDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => Task.CompletedTask;

    private IStudentService CreateStudentService()
    {
        var context = new PASDbContext(_dbOptions);
        var emailSender = new Mock<IEmailService>();
        return new StudentService(context, emailSender.Object);
    }

    private StudentController CreateController(IStudentService studentService, int userId = 1)
    {
        var controller = new StudentController(studentService);
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

    #region CreateStudent Tests

    [Fact]
    public async Task CreateStudent_WithValidDto_ReturnsCreated()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var emailSender = new Mock<IEmailService>();
        var service = new StudentService(context, emailSender.Object);
        var controller = CreateController(service);
        var dto = new CreateStudentDto
        {
            Name = "John Doe",
            Email = "john@example.com",
            Batch = "2024"
        };

        // Act
        var result = await controller.CreateStudent(dto);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status201Created, objectResult.StatusCode);
    }

    [Fact]
    public async Task CreateStudent_WithDuplicateEmail_ReturnsConflict()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var emailSender = new Mock<IEmailService>();
        var service = new StudentService(context, emailSender.Object);
        var controller = CreateController(service);
        var dto = new CreateStudentDto
        {
            Name = "John Doe",
            Email = "john2@example.com",
            Batch = "2024"
        };

        // Act - first call succeeds
        await controller.CreateStudent(dto);
        // Second call with same email should fail
        var result = await controller.CreateStudent(dto);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        Assert.NotNull(conflictResult.Value);
    }

    [Fact]
    public async Task CreateStudent_WithMissingName_ReturnsBadRequest()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var emailSender = new Mock<IEmailService>();
        var service = new StudentService(context, emailSender.Object);
        var controller = CreateController(service);
        var dto = new CreateStudentDto
        {
            Name = "",
            Email = "john3@example.com",
            Batch = "2024"
        };

        // Act
        var result = await controller.CreateStudent(dto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
    }

    #endregion

    #region GetAllStudents Tests

    [Fact]
    public async Task GetAllStudents_WithNoStudents_ReturnsEmptyList()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var emailSender = new Mock<IEmailService>();
        var service = new StudentService(context, emailSender.Object);
        var controller = CreateController(service);

        // Act
        var result = await controller.GetAllStudents();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetAllStudents_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var emailSender = new Mock<IEmailService>();
        var service = new StudentService(context, emailSender.Object);
        var controller = CreateController(service);

        // Add test students
        for (int i = 1; i <= 15; i++)
        {
            await service.CreateStudentAsync(new CreateStudentDto
            {
                Name = $"Student {i}",
                Email = $"student{i}@example.com",
                Batch = "2024"
            });
        }

        // Act
        var result = await controller.GetAllStudents(page: 1, pageSize: 5);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    #endregion

    #region GetStudent Tests

    [Fact]
    public async Task GetStudent_WithValidId_ReturnsStudent()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var emailSender = new Mock<IEmailService>();
        var service = new StudentService(context, emailSender.Object);
        var controller = CreateController(service);

        var (student, _) = await service.CreateStudentAsync(new CreateStudentDto
        {
            Name = "John Doe",
            Email = "john.get@example.com",
            Batch = "2024"
        });

        // Act
        var result = await controller.GetStudent(student.UserId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetStudent_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var emailSender = new Mock<IEmailService>();
        var service = new StudentService(context, emailSender.Object);
        var controller = CreateController(service);

        // Act
        var result = await controller.GetStudent(999);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);
    }

    #endregion

    #region UpdateStudent Tests

    [Fact]
    public async Task UpdateStudent_WithValidId_ReturnsUpdatedStudent()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var emailSender = new Mock<IEmailService>();
        var service = new StudentService(context, emailSender.Object);
        var controller = CreateController(service);

        var (student, _) = await service.CreateStudentAsync(new CreateStudentDto
        {
            Name = "John Doe",
            Email = "john.upd@example.com",
            Batch = "2024"
        });

        var dto = new UpdateStudentDto { Batch = "2025" };

        // Act
        var result = await controller.UpdateStudent(student.UserId, dto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task UpdateStudent_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var emailSender = new Mock<IEmailService>();
        var service = new StudentService(context, emailSender.Object);
        var controller = CreateController(service);

        var dto = new UpdateStudentDto { Batch = "2025" };

        // Act
        var result = await controller.UpdateStudent(999, dto);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);
    }

    #endregion

    #region DeleteStudent Tests

    [Fact]
    public async Task DeleteStudent_WithValidId_ReturnsSuccess()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var emailSender = new Mock<IEmailService>();
        var service = new StudentService(context, emailSender.Object);
        var controller = CreateController(service);

        var (student, _) = await service.CreateStudentAsync(new CreateStudentDto
        {
            Name = "John Doe",
            Email = "john.del@example.com",
            Batch = "2024"
        });

        // Act
        var result = await controller.DeleteStudent(student.UserId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task DeleteStudent_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var emailSender = new Mock<IEmailService>();
        var service = new StudentService(context, emailSender.Object);
        var controller = CreateController(service);

        // Act
        var result = await controller.DeleteStudent(999);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);
    }

    #endregion

    #region ResetStudentPassword Tests

    [Fact]
    public async Task ResetStudentPassword_WithValidId_ReturnsSuccess()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var emailSender = new Mock<IEmailService>();
        var service = new StudentService(context, emailSender.Object);
        var controller = CreateController(service);

        var (student, _) = await service.CreateStudentAsync(new CreateStudentDto
        {
            Name = "John Doe",
            Email = "john.reset@example.com",
            Batch = "2024"
        });

        // Act
        var result = await controller.ResetStudentPassword(student.UserId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task ResetStudentPassword_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var emailSender = new Mock<IEmailService>();
        var service = new StudentService(context, emailSender.Object);
        var controller = CreateController(service);

        // Act
        var result = await controller.ResetStudentPassword(999);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);
    }

    #endregion
}
