using Moq;
using Xunit;
using PAS.API.Data;
using PAS.API.DTOs.Common;
using PAS.API.DTOs.Student;
using PAS.API.Models;
using PAS.API.Services;
using Microsoft.EntityFrameworkCore;

namespace PAS.API.Tests;

/// <summary>
/// Unit tests for StudentService covering CRUD operations and password reset.
/// Uses in-memory database and mocked email service.
/// </summary>
public class StudentServiceTests : IAsyncLifetime
{
    private readonly DbContextOptions<PASDbContext> _dbOptions;
    private Mock<IEmailService> _mockEmailService = null!;

    public StudentServiceTests()
    {
        _dbOptions = new DbContextOptionsBuilder<PASDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => Task.CompletedTask;

    private StudentService CreateService(PASDbContext context)
    {
        _mockEmailService = new Mock<IEmailService>();
        return new StudentService(context, _mockEmailService.Object);
    }

    #region CreateStudentAsync Tests

    [Fact]
    public async Task CreateStudentAsync_WithValidData_CreatesStudentAndSendsEmail()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var service = CreateService(context);
        var dto = new CreateStudentDto
        {
            Name = "John Doe",
            Email = "john@example.com",
            Batch = "2024"
        };

        _mockEmailService
            .Setup(s => s.SendWelcomeEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var (result, emailSent) = await service.CreateStudentAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("John Doe", result.Name);
        Assert.Equal("john@example.com", result.Email);
        Assert.Equal("2024", result.Batch);
        Assert.True(emailSent);

        var user = await context.Users.FirstAsync(u => u.Email == "john@example.com");
        Assert.NotNull(user);
        Assert.Equal("STUDENT", user.Role);

        _mockEmailService.Verify(
            s => s.SendWelcomeEmailAsync("john@example.com", "John Doe", It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateStudentAsync_WithDuplicateEmail_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var existingUser = new User
        {
            Email = "existing@example.com",
            Name = "Existing User",
            Password = "hashed",
            Role = "STUDENT",
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(existingUser);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var dto = new CreateStudentDto
        {
            Name = "New User",
            Email = "existing@example.com",
            Batch = "2024"
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateStudentAsync(dto));
    }

    [Fact]
    public async Task CreateStudentAsync_WithEmptyName_ThrowsArgumentException()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var service = CreateService(context);
        var dto = new CreateStudentDto { Name = "", Email = "test@example.com", Batch = "2024" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateStudentAsync(dto));
    }

    [Fact]
    public async Task CreateStudentAsync_WithEmptyEmail_ThrowsArgumentException()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var service = CreateService(context);
        var dto = new CreateStudentDto { Name = "John", Email = "", Batch = "2024" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateStudentAsync(dto));
    }

    [Fact]
    public async Task CreateStudentAsync_WithEmptyBatch_ThrowsArgumentException()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var service = CreateService(context);
        var dto = new CreateStudentDto { Name = "John", Email = "john@example.com", Batch = "" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateStudentAsync(dto));
    }

    [Fact]
    public async Task CreateStudentAsync_TrimsAndLowercasesEmail()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var service = CreateService(context);
        var dto = new CreateStudentDto
        {
            Name = "John Doe",
            Email = "  JOHN@EXAMPLE.COM  ",
            Batch = "2024"
        };

        _mockEmailService.Setup(s => s.SendWelcomeEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

        // Act
        await service.CreateStudentAsync(dto);

        // Assert
        var user = await context.Users.FirstAsync(u => u.UserId > 0);
        Assert.Equal("john@example.com", user.Email);
    }

    [Fact]
    public async Task CreateStudentAsync_EmailServiceFails_StillReturnsSuccessWithEmailSentFalse()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var service = CreateService(context);
        var dto = new CreateStudentDto
        {
            Name = "John Doe",
            Email = "john@example.com",
            Batch = "2024"
        };

        _mockEmailService
            .Setup(s => s.SendWelcomeEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("Email service failed"));

        // Act
        var (result, emailSent) = await service.CreateStudentAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.False(emailSent); // Email was not sent
        
        var user = await context.Users.FirstAsync(u => u.Email == "john@example.com");
        Assert.NotNull(user); // But student was created
    }

    #endregion

    #region GetAllStudentsAsync Tests

    [Fact]
    public async Task GetAllStudentsAsync_ReturnsPagedResults()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        for (int i = 1; i <= 5; i++)
        {
            var user = new User
            {
                Email = $"student{i}@example.com",
                Name = $"Student {i}",
                Password = "hashed",
                Role = "STUDENT",
                CreatedAt = DateTime.UtcNow
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var student = new Student { UserId = user.UserId, Batch = "2024" };
            context.Students.Add(student);
        }
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.GetAllStudentsAsync(1, 10);

        // Assert
        Assert.Equal(5, result.TotalCount);
        Assert.Equal(5, result.Data.Count());
        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(1, result.TotalPages);
    }

    [Fact]
    public async Task GetAllStudentsAsync_WithPagination_ReturnsSinglePage()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        for (int i = 1; i <= 15; i++)
        {
            var user = new User
            {
                Email = $"student{i}@example.com",
                Name = $"Student {i}",
                Password = "hashed",
                Role = "STUDENT",
                CreatedAt = DateTime.UtcNow
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var student = new Student { UserId = user.UserId, Batch = "2024" };
            context.Students.Add(student);
        }
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.GetAllStudentsAsync(2, 5);

        // Assert
        Assert.Equal(15, result.TotalCount);
        Assert.Equal(5, result.Data.Count());
        Assert.Equal(2, result.Page);
        Assert.Equal(3, result.TotalPages);
    }

    #endregion

    #region GetStudentAsync Tests

    [Fact]
    public async Task GetStudentAsync_WithValidId_ReturnsStudent()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var user = new User
        {
            Email = "student@example.com",
            Name = "Test Student",
            Password = "hashed",
            Role = "STUDENT",
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var student = new Student { UserId = user.UserId, Batch = "2024" };
        context.Students.Add(student);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.GetStudentAsync(user.UserId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.UserId, result.UserId);
        Assert.Equal("Test Student", result.Name);
        Assert.Equal("2024", result.Batch);
    }

    [Fact]
    public async Task GetStudentAsync_WithInvalidId_ThrowsKeyNotFoundException()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var service = CreateService(context);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetStudentAsync(999));
    }

    #endregion

    #region UpdateStudentAsync Tests

    [Fact]
    public async Task UpdateStudentAsync_WithValidData_UpdatesStudent()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var user = new User
        {
            Email = "student@example.com",
            Name = "Old Name",
            Password = "hashed",
            Role = "STUDENT",
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var student = new Student { UserId = user.UserId, Batch = "2024" };
        context.Students.Add(student);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var dto = new UpdateStudentDto
        {
            Name = "New Name",
            Email = "newemail@example.com",
            Batch = "2025"
        };

        // Act
        var result = await service.UpdateStudentAsync(user.UserId, dto);

        // Assert
        Assert.Equal("New Name", result.Name);
        Assert.Equal("newemail@example.com", result.Email);
        Assert.Equal("2025", result.Batch);
    }

    [Fact]
    public async Task UpdateStudentAsync_WithDuplicateEmail_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var user1 = new User
        {
            Email = "student1@example.com",
            Name = "Student 1",
            Password = "hashed",
            Role = "STUDENT",
            CreatedAt = DateTime.UtcNow
        };
        var user2 = new User
        {
            Email = "student2@example.com",
            Name = "Student 2",
            Password = "hashed",
            Role = "STUDENT",
            CreatedAt = DateTime.UtcNow
        };
        context.Users.AddRange(user1, user2);
        await context.SaveChangesAsync();

        var student1 = new Student { UserId = user1.UserId, Batch = "2024" };
        var student2 = new Student { UserId = user2.UserId, Batch = "2024" };
        context.Students.AddRange(student1, student2);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var dto = new UpdateStudentDto { Email = "student1@example.com" };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateStudentAsync(user2.UserId, dto));
    }

    [Fact]
    public async Task UpdateStudentAsync_WithInvalidId_ThrowsKeyNotFoundException()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var service = CreateService(context);
        var dto = new UpdateStudentDto { Name = "New Name" };

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.UpdateStudentAsync(999, dto));
    }

    #endregion

    #region DeleteStudentAsync Tests

    [Fact]
    public async Task DeleteStudentAsync_WithValidId_DeletesStudent()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var user = new User
        {
            Email = "student@example.com",
            Name = "Test Student",
            Password = "hashed",
            Role = "STUDENT",
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var student = new Student { UserId = user.UserId, Batch = "2024" };
        context.Students.Add(student);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        await service.DeleteStudentAsync(user.UserId);

        // Assert
        var deletedUser = await context.Users.FirstOrDefaultAsync(u => u.UserId == user.UserId);
        Assert.Null(deletedUser);
    }

    [Fact]
    public async Task DeleteStudentAsync_WithInvalidId_ThrowsKeyNotFoundException()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var service = CreateService(context);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.DeleteStudentAsync(999));
    }

    #endregion

    #region ResetStudentPasswordAsync Tests

    [Fact]
    public async Task ResetStudentPasswordAsync_WithValidId_ResetsPasswordAndSendsEmail()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var oldPasswordHash = BCrypt.Net.BCrypt.HashPassword("oldpassword", 12);
        var user = new User
        {
            Email = "student@example.com",
            Name = "Test Student",
            Password = oldPasswordHash,
            Role = "STUDENT",
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var student = new Student { UserId = user.UserId, Batch = "2024" };
        context.Students.Add(student);
        await context.SaveChangesAsync();

        var userId = user.UserId;

        var service = CreateService(context);

        _mockEmailService
            .Setup(s => s.SendAdminPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var (passwordReset, emailSent) = await service.ResetStudentPasswordAsync(userId);

        // Assert
        Assert.True(passwordReset);
        Assert.True(emailSent);

        // Verify password was changed by fetching fresh from DB
        var updatedUser = await context.Users.FirstAsync(u => u.UserId == userId);
        Assert.NotEqual(oldPasswordHash, updatedUser.Password); // Password hash changed
        Assert.False(BCrypt.Net.BCrypt.Verify("oldpassword", updatedUser.Password)); // Old password doesn't work

        _mockEmailService.Verify(
            s => s.SendAdminPasswordResetEmailAsync("student@example.com", "Test Student", It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task ResetStudentPasswordAsync_WithInvalidId_ThrowsKeyNotFoundException()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var service = CreateService(context);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.ResetStudentPasswordAsync(999));
    }

    [Fact]
    public async Task ResetStudentPasswordAsync_EmailServiceFails_StillReturnsPasswordResetTrueWithEmailSentFalse()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var user = new User
        {
            Email = "student@example.com",
            Name = "Test Student",
            Password = BCrypt.Net.BCrypt.HashPassword("oldpassword", 12),
            Role = "STUDENT",
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var student = new Student { UserId = user.UserId, Batch = "2024" };
        context.Students.Add(student);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        _mockEmailService
            .Setup(s => s.SendAdminPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("Email service failed"));

        // Act
        var (passwordReset, emailSent) = await service.ResetStudentPasswordAsync(user.UserId);

        // Assert
        Assert.True(passwordReset);
        Assert.False(emailSent); // Email was not sent
    }

    #endregion
}
