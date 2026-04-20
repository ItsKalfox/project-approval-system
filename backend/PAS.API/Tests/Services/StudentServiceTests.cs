using Microsoft.EntityFrameworkCore;
using Moq;
using PAS.API.Data;
using PAS.API.DTOs.Student;
using PAS.API.Models;
using PAS.API.Services;
using Xunit;

namespace PAS.API.Tests.Services;

public class StudentServiceTests
{
    private readonly PASDbContext _dbContext;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly StudentService _studentService;

    public StudentServiceTests()
    {
        var options = new DbContextOptionsBuilder<PASDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        _dbContext = new PASDbContext(options);
        _mockEmailService = new Mock<IEmailService>();
        _studentService = new StudentService(_dbContext, _mockEmailService.Object);
    }

    [Fact]
    public async Task CreateStudentAsync_ValidData_ReturnsStudent()
    {
        var dto = new CreateStudentDto { Name = "John Doe", Email = "john@example.com", Batch = "2024" };

        var result = await _studentService.CreateStudentAsync(dto);

        Assert.NotNull(result.Student);
        Assert.Equal("John Doe", result.Student.Name);
        Assert.Equal("john@example.com", result.Student.Email);
        Assert.Equal("2024", result.Student.Batch);
    }

    [Fact]
    public async Task CreateStudentAsync_EmptyName_ThrowsArgumentException()
    {
        var dto = new CreateStudentDto { Name = "", Email = "john@example.com", Batch = "2024" };

        await Assert.ThrowsAsync<ArgumentException>(() => _studentService.CreateStudentAsync(dto));
    }

    [Fact]
    public async Task CreateStudentAsync_EmptyEmail_ThrowsArgumentException()
    {
        var dto = new CreateStudentDto { Name = "John Doe", Email = "", Batch = "2024" };

        await Assert.ThrowsAsync<ArgumentException>(() => _studentService.CreateStudentAsync(dto));
    }

    [Fact]
    public async Task CreateStudentAsync_EmptyBatch_ThrowsArgumentException()
    {
        var dto = new CreateStudentDto { Name = "John Doe", Email = "john@example.com", Batch = "" };

        await Assert.ThrowsAsync<ArgumentException>(() => _studentService.CreateStudentAsync(dto));
    }

    [Fact]
    public async Task CreateStudentAsync_DuplicateEmail_ThrowsInvalidOperationException()
    {
        var existingUser = new User { Name = "Existing", Email = "john@example.com", Password = "hash", Role = "STUDENT" };
        _dbContext.Users.Add(existingUser);
        await _dbContext.SaveChangesAsync();

        var dto = new CreateStudentDto { Name = "John Doe", Email = "john@example.com", Batch = "2024" };

        await Assert.ThrowsAsync<InvalidOperationException>(() => _studentService.CreateStudentAsync(dto));
    }

    [Fact]
    public async Task GetAllStudentsAsync_ReturnsPagedResults()
    {
        var user = new User { Name = "Test", Email = "test@test.com", Password = "hash", Role = "STUDENT" };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();
        _dbContext.Students.Add(new Student { UserId = user.UserId, Batch = "2024" });
        await _dbContext.SaveChangesAsync();

        var result = await _studentService.GetAllStudentsAsync(1, 10);

        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Data);
    }

    [Fact]
    public async Task GetAllStudentsAsync_EmptyDatabase_ReturnsEmptyList()
    {
        var result = await _studentService.GetAllStudentsAsync(1, 10);

        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Data);
    }

    [Fact]
    public async Task GetStudentAsync_ExistingId_ReturnsStudent()
    {
        var user = new User { Name = "Test", Email = "test@test.com", Password = "hash", Role = "STUDENT" };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();
        _dbContext.Students.Add(new Student { UserId = user.UserId, Batch = "2024" });
        await _dbContext.SaveChangesAsync();

        var result = await _studentService.GetStudentAsync(user.UserId);

        Assert.Equal("Test", result.Name);
        Assert.Equal("2024", result.Batch);
    }

    [Fact]
    public async Task GetStudentAsync_NonExistingId_ThrowsKeyNotFoundException()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _studentService.GetStudentAsync(999));
    }

    [Fact]
    public async Task UpdateStudentAsync_ValidUpdate_UpdatesStudent()
    {
        var user = new User { Name = "Test", Email = "test@test.com", Password = "hash", Role = "STUDENT" };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();
        _dbContext.Students.Add(new Student { UserId = user.UserId, Batch = "2024" });
        await _dbContext.SaveChangesAsync();

        var dto = new UpdateStudentDto { Name = "Updated Name", Batch = "2025" };
        var result = await _studentService.UpdateStudentAsync(user.UserId, dto);

        Assert.Equal("Updated Name", result.Name);
        Assert.Equal("2025", result.Batch);
    }

    [Fact]
    public async Task DeleteStudentAsync_ExistingId_DeletesStudent()
    {
        var user = new User { Name = "Test", Email = "test@test.com", Password = "hash", Role = "STUDENT" };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();
        _dbContext.Students.Add(new Student { UserId = user.UserId, Batch = "2024" });
        await _dbContext.SaveChangesAsync();

        await _studentService.DeleteStudentAsync(user.UserId);

        var deleted = await _dbContext.Users.FindAsync(user.UserId);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task ResetStudentPasswordAsync_ExistingId_ResetsPassword()
    {
        var user = new User { Name = "Test", Email = "test@test.com", Password = "hash", Role = "STUDENT" };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();
        _dbContext.Students.Add(new Student { UserId = user.UserId, Batch = "2024" });
        await _dbContext.SaveChangesAsync();

        var result = await _studentService.ResetStudentPasswordAsync(user.UserId);

        Assert.True(result.PasswordReset);
        var updatedUser = await _dbContext.Users.FindAsync(user.UserId);
        Assert.NotEqual("hash", updatedUser!.Password);
    }
}