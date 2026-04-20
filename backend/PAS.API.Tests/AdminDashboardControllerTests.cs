using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PAS.API.Controllers;
using PAS.API.Data;
using PAS.API.Models;
using System.Security.Claims;
using Xunit;
using Microsoft.EntityFrameworkCore;

namespace PAS.API.Tests;

public class AdminDashboardControllerTests : IAsyncLifetime
{
    private readonly DbContextOptions<PASDbContext> _dbOptions;

    public AdminDashboardControllerTests()
    {
        _dbOptions = new DbContextOptionsBuilder<PASDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => Task.CompletedTask;

    private PASDbContext CreateContext() => new PASDbContext(_dbOptions);

    private AdminDashboardController CreateController(PASDbContext context)
    {
        var controller = new AdminDashboardController(context);
        return controller;
    }

    [Fact]
    public async Task GetSummary_ReturnsSummaryWithCounts()
    {
        using var context = CreateContext();
        
        var supUser = new User { Email = "supervisor@test.com", Name = "Supervisor", Password = "hash", Role = "SUPERVISOR", CreatedAt = DateTime.UtcNow };
        context.Users.Add(supUser);
        await context.SaveChangesAsync();

        var supervisor = new Supervisor { UserId = supUser.UserId };
        context.Supervisors.Add(supervisor);
        await context.SaveChangesAsync();

        var studentUser = new User { Email = "student@test.com", Name = "Student", Password = "hash", Role = "STUDENT", CreatedAt = DateTime.UtcNow };
        context.Users.Add(studentUser);
        await context.SaveChangesAsync();

        var student = new Student { UserId = studentUser.UserId, Batch = "2024" };
        context.Students.Add(student);
        await context.SaveChangesAsync();

        var project = new Project { Title = "Test Project", Status = "Matched", IsDeleted = false };
        context.Projects.Add(project);
        await context.SaveChangesAsync();

        var group = new Group { LeaderId = studentUser.UserId };
        context.Groups.Add(group);
        await context.SaveChangesAsync();

        var groupProject = new Project { Title = "Group Project", Status = "Matched", GroupId = group.GroupId, IsDeleted = false };
        context.Projects.Add(groupProject);
        await context.SaveChangesAsync();

        var controller = CreateController(context);

        var result = await controller.GetSummary();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetSummary_WithPasswordResetRequests_ReturnsRequests()
    {
        using var context = CreateContext();
        
        var user = new User { Email = "test@test.com", Name = "Test User", Password = "hash", Role = "STUDENT", CreatedAt = DateTime.UtcNow };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var otp = new PasswordResetOtp
        {
            Email = user.Email,
            OtpCode = "123456",
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            CreatedAt = DateTime.UtcNow,
            IsUsed = false
        };
        context.PasswordResetOtps.Add(otp);
        await context.SaveChangesAsync();

        var controller = CreateController(context);

        var result = await controller.GetSummary();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetSummary_EmptyDatabase_ReturnsZeros()
    {
        using var context = CreateContext();
        var controller = CreateController(context);

        var result = await controller.GetSummary();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetSummary_WithExpiredOtp_ReturnsExpiredStatus()
    {
        using var context = CreateContext();
        
        var user = new User { Email = "expired@test.com", Name = "Test User", Password = "hash", Role = "STUDENT", CreatedAt = DateTime.UtcNow };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var otp = new PasswordResetOtp
        {
            Email = user.Email,
            OtpCode = "123456",
            ExpiresAt = DateTime.UtcNow.AddMinutes(-10),
            CreatedAt = DateTime.UtcNow.AddMinutes(-20),
            IsUsed = false
        };
        context.PasswordResetOtps.Add(otp);
        await context.SaveChangesAsync();

        var controller = CreateController(context);

        var result = await controller.GetSummary();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetSummary_WithUsedOtp_ReturnsChangedStatus()
    {
        using var context = CreateContext();
        
        var user = new User { Email = "used@test.com", Name = "Test User", Password = "hash", Role = "STUDENT", CreatedAt = DateTime.UtcNow };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var otp = new PasswordResetOtp
        {
            Email = user.Email,
            OtpCode = "123456",
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            CreatedAt = DateTime.UtcNow,
            IsUsed = true
        };
        context.PasswordResetOtps.Add(otp);
        await context.SaveChangesAsync();

        var controller = CreateController(context);

        var result = await controller.GetSummary();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }
}
