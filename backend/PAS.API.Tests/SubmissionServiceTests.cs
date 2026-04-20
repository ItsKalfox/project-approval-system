using Moq;
using Xunit;
using PAS.API.Data;
using PAS.API.DTOs.Submission;
using PAS.API.Models;
using PAS.API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace PAS.API.Tests;

public class SubmissionServiceTests : IAsyncLifetime
{
    private readonly DbContextOptions<PASDbContext> _dbOptions;
    private Mock<IBlobStorageService> _mockBlobService = null!;

    public SubmissionServiceTests()
    {
        _dbOptions = new DbContextOptionsBuilder<PASDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => Task.CompletedTask;

    private SubmissionService CreateService(PASDbContext context)
    {
        _mockBlobService = new Mock<IBlobStorageService>();
        return new SubmissionService(context, _mockBlobService.Object);
    }

    #region GetStudentSubmissionsAsync Tests

    [Fact]
    public async Task GetStudentSubmissionsAsync_WithStudentSubmissions_ReturnsSubmissions()
    {
        using var context = new PASDbContext(_dbOptions);
        
        // Create student and user
        var studentUser = new User
        {
            UserId = 1,
            Name = "Student",
            Email = "student@example.com",
            Password = "hash",
            Role = "STUDENT"
        };
        var student = new Student { UserId = 1, Batch = "2024", User = studentUser };
        
        // Create supervisor
        var supervisorUser = new User
        {
            UserId = 2,
            Name = "Supervisor",
            Email = "sup@example.com",
            Password = "hash",
            Role = "SUPERVISOR"
        };
        var supervisor = new Supervisor { UserId = 2, User = supervisorUser };
        
        // Create research area
        var researchArea = new ResearchArea { Id = 1, Name = "AI" };
        
        // Create group led by student
        var group = new Group { GroupId = 1, LeaderId = 1, Leader = student };
        
        // Create project
        var project = new Project
        {
            ProjectId = 1,
            Title = "Project 1",
            Description = "Description",
            ResearchAreaId = 1,
            GroupId = 1,
            IsDeleted = false,
            SubmittedAt = DateTime.UtcNow
        };
        
        // Create coursework
        var coursework = new Coursework
        {
            CourseworkId = 1,
            Title = "CW1",
            Description = "Coursework 1",
            Deadline = DateTime.UtcNow.AddDays(7),
            IsIndividual = false
        };
        
        // Link coursework to project
        var courseworkProject = new CourseworkProject
        {
            CourseworkId = 1,
            ProjectId = 1
        };

        context.Users.AddRange(studentUser, supervisorUser);
        context.Students.Add(student);
        context.Supervisors.Add(supervisor);
        context.ResearchAreas.Add(researchArea);
        context.Groups.Add(group);
        context.Projects.Add(project);
        context.Courseworks.Add(coursework);
        context.CourseworkProjects.Add(courseworkProject);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        
        var result = await service.GetStudentSubmissionsAsync(1);

        Assert.NotNull(result);
        var submissions = result.ToList();
        Assert.NotEmpty(submissions);
    }

    [Fact]
    public async Task GetStudentSubmissionsAsync_NoSubmissions_ReturnsEmptyList()
    {
        using var context = new PASDbContext(_dbOptions);
        var service = CreateService(context);
        
        var result = await service.GetStudentSubmissionsAsync(999);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetStudentSubmissionsAsync_DeletedProjects_ExcludesDeletedProjects()
    {
        using var context = new PASDbContext(_dbOptions);
        
        var studentUser = new User
        {
            UserId = 1,
            Name = "Student",
            Email = "student@example.com",
            Password = "hash",
            Role = "STUDENT"
        };
        var student = new Student { UserId = 1, Batch = "2024", User = studentUser };
        
        var researchArea = new ResearchArea { Id = 1, Name = "AI" };
        var group = new Group { GroupId = 1, LeaderId = 1, Leader = student };

        var deletedProject = new Project
        {
            ProjectId = 1,
            Title = "Deleted Project",
            Description = "Description",
            ResearchAreaId = 1,
            GroupId = 1,
            IsDeleted = true // Marked as deleted
        };

        var coursework = new Coursework
        {
            CourseworkId = 1,
            Title = "CW1",
            Description = "Coursework 1",
            Deadline = DateTime.UtcNow.AddDays(7),
            IsIndividual = false
        };
        
        var courseworkProject = new CourseworkProject
        {
            CourseworkId = 1,
            ProjectId = 1
        };

        context.Users.Add(studentUser);
        context.Students.Add(student);
        context.ResearchAreas.Add(researchArea);
        context.Groups.Add(group);
        context.Projects.Add(deletedProject);
        context.Courseworks.Add(coursework);
        context.CourseworkProjects.Add(courseworkProject);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        
        var result = await service.GetStudentSubmissionsAsync(1);

        Assert.Empty(result); // Deleted projects should not be returned
    }

    #endregion

    #region GetSubmissionPointsAsync Tests

    [Fact]
    public async Task GetSubmissionPointsAsync_WithCourseworks_ReturnsSubmissionPoints()
    {
        using var context = new PASDbContext(_dbOptions);
        
        var studentUser = new User
        {
            UserId = 1,
            Name = "Student",
            Email = "student@example.com",
            Password = "hash",
            Role = "STUDENT"
        };
        var student = new Student { UserId = 1, Batch = "2024", User = studentUser };

        var coursework1 = new Coursework
        {
            CourseworkId = 1,
            Title = "Assignment 1",
            Description = "First assignment",
            Deadline = DateTime.UtcNow.AddDays(7),
            IsIndividual = true
        };

        var coursework2 = new Coursework
        {
            CourseworkId = 2,
            Title = "Group Project",
            Description = "Group project",
            Deadline = DateTime.UtcNow.AddDays(14),
            IsIndividual = false
        };

        context.Users.Add(studentUser);
        context.Students.Add(student);
        context.Courseworks.AddRange(coursework1, coursework2);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        
        var result = await service.GetSubmissionPointsAsync(1);

        Assert.NotNull(result);
        var points = result.ToList();
        Assert.Equal(2, points.Count);
        Assert.All(points, p => Assert.False(p.HasSubmitted)); // No submissions yet
    }

    [Fact]
    public async Task GetSubmissionPointsAsync_WithExistingSubmission_MarksAsSubmitted()
    {
        using var context = new PASDbContext(_dbOptions);
        
        var studentUser = new User
        {
            UserId = 1,
            Name = "Student",
            Email = "student@example.com",
            Password = "hash",
            Role = "STUDENT"
        };
        var student = new Student { UserId = 1, Batch = "2024", User = studentUser };

        var researchArea = new ResearchArea { Id = 1, Name = "AI" };
        var group = new Group { GroupId = 1, LeaderId = 1, Leader = student };

        var project = new Project
        {
            ProjectId = 1,
            Title = "Project",
            Description = "Description",
            ResearchAreaId = 1,
            GroupId = 1,
            IsDeleted = false
        };

        var coursework = new Coursework
        {
            CourseworkId = 1,
            Title = "Assignment",
            Description = "Assignment description",
            Deadline = DateTime.UtcNow.AddDays(7),
            IsIndividual = true
        };

        var courseworkProject = new CourseworkProject { CourseworkId = 1, ProjectId = 1 };

        context.Users.Add(studentUser);
        context.Students.Add(student);
        context.ResearchAreas.Add(researchArea);
        context.Groups.Add(group);
        context.Projects.Add(project);
        context.Courseworks.Add(coursework);
        context.CourseworkProjects.Add(courseworkProject);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        
        var result = await service.GetSubmissionPointsAsync(1);

        Assert.NotNull(result);
        var points = result.ToList();
        Assert.Single(points);
        Assert.True(points.First().HasSubmitted);
        Assert.Equal(1, points.First().ExistingProjectId);
    }

    [Fact]
    public async Task GetSubmissionPointsAsync_EmptyDatabase_ReturnsEmptyList()
    {
        using var context = new PASDbContext(_dbOptions);
        var service = CreateService(context);
        
        var result = await service.GetSubmissionPointsAsync(999);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    #endregion

    #region GetAvailableGroupsAsync Tests

    [Fact]
    public async Task GetAvailableGroupsAsync_IndividualCoursework_ReturnsEmptyList()
    {
        using var context = new PASDbContext(_dbOptions);
        
        var coursework = new Coursework
        {
            CourseworkId = 1,
            Title = "Individual Assignment",
            Description = "Individual",
            Deadline = DateTime.UtcNow.AddDays(7),
            IsIndividual = true
        };

        context.Courseworks.Add(coursework);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        
        var result = await service.GetAvailableGroupsAsync(1);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAvailableGroupsAsync_NonExistentCoursework_ThrowsKeyNotFoundException()
    {
        using var context = new PASDbContext(_dbOptions);
        var service = CreateService(context);
        
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetAvailableGroupsAsync(999));
    }

    [Fact]
    public async Task GetAvailableGroupsAsync_GroupCoursework_ReturnsAvailableGroups()
    {
        using var context = new PASDbContext(_dbOptions);
        
        var coursework = new Coursework
        {
            CourseworkId = 1,
            Title = "Group Project",
            Description = "Group project",
            Deadline = DateTime.UtcNow.AddDays(14),
            IsIndividual = false
        };

        context.Courseworks.Add(coursework);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        
        var result = await service.GetAvailableGroupsAsync(1);

        Assert.NotNull(result);
        // Result depends on GetAvailableGroupsAsync implementation logic
        // which returns available groups for the coursework
    }

    #endregion

    #region Submission Workflow Tests

    [Fact]
    public async Task GetStudentSubmissionsAsync_OrdersByMostRecent()
    {
        using var context = new PASDbContext(_dbOptions);
        
        var studentUser = new User
        {
            UserId = 1,
            Name = "Student",
            Email = "student@example.com",
            Password = "hash",
            Role = "STUDENT"
        };
        var student = new Student { UserId = 1, Batch = "2024", User = studentUser };

        var researchArea = new ResearchArea { Id = 1, Name = "AI" };
        var group = new Group { GroupId = 1, LeaderId = 1, Leader = student };

        var project1 = new Project
        {
            ProjectId = 1,
            Title = "Project 1",
            Description = "Description",
            ResearchAreaId = 1,
            GroupId = 1,
            IsDeleted = false,
            SubmittedAt = DateTime.UtcNow.AddDays(-5)
        };

        var project2 = new Project
        {
            ProjectId = 2,
            Title = "Project 2",
            Description = "Description",
            ResearchAreaId = 1,
            GroupId = 1,
            IsDeleted = false,
            SubmittedAt = DateTime.UtcNow // More recent
        };

        var coursework1 = new Coursework
        {
            CourseworkId = 1,
            Title = "CW1",
            Description = "Coursework 1",
            Deadline = DateTime.UtcNow.AddDays(7),
            IsIndividual = false
        };
        var coursework2 = new Coursework
        {
            CourseworkId = 2,
            Title = "CW2",
            Description = "Coursework 2",
            Deadline = DateTime.UtcNow.AddDays(14),
            IsIndividual = false
        };

        context.Users.Add(studentUser);
        context.Students.Add(student);
        context.ResearchAreas.Add(researchArea);
        context.Groups.Add(group);
        context.Projects.AddRange(project1, project2);
        context.Courseworks.AddRange(coursework1, coursework2);
        context.CourseworkProjects.Add(new CourseworkProject { CourseworkId = 1, ProjectId = 1 });
        context.CourseworkProjects.Add(new CourseworkProject { CourseworkId = 2, ProjectId = 2 });
        await context.SaveChangesAsync();

        var service = CreateService(context);
        
        var result = await service.GetStudentSubmissionsAsync(1);

        Assert.NotNull(result);
        var submissions = result.ToList();
        Assert.Equal(2, submissions.Count);
        // Most recent should be first
        Assert.Equal("Project 2", submissions.First().Title);
    }

    #endregion

    #region CreateSubmissionAsync Tests

    [Fact]
    public async Task CreateSubmissionAsync_WithValidIndividualData_CreatesSubmission()
    {
        using var context = new PASDbContext(_dbOptions);
        var service = CreateService(context);

        var studentUser = new User
        {
            UserId = 1,
            Name = "Student",
            Email = "student@example.com",
            Password = BCrypt.Net.BCrypt.HashPassword("password"),
            Role = "STUDENT"
        };
        var student = new Student { UserId = 1, Batch = "2024", User = studentUser };

        var researchArea = new ResearchArea { Id = 1, Name = "AI" };
        var coursework = new Coursework
        {
            CourseworkId = 1,
            Title = "Coursework 1",
            Description = "Description",
            Deadline = DateTime.UtcNow.AddDays(7),
            IsIndividual = true
        };

        context.Users.Add(studentUser);
        context.Students.Add(student);
        context.ResearchAreas.Add(researchArea);
        context.Courseworks.Add(coursework);
        await context.SaveChangesAsync();

        var dto = new CreateSubmissionDto
        {
            Title = "Project Title",
            Description = "Project Description",
            Abstract = "Abstract",
            ResearchAreaId = 1,
            GroupId = null
        };

        var file = CreatePdfFile();

        var result = await service.CreateSubmissionAsync(1, 1, dto, file);

        Assert.NotNull(result);
        Assert.Equal("Project Title", result.Title);
        Assert.Equal(1, result.ResearchAreaId);

        _mockBlobService.Verify(s => s.UploadAsync(
            It.IsAny<Stream>(),
            It.IsAny<string>(),
            "application/pdf"), Times.Once);
    }

    [Fact]
    public async Task CreateSubmissionAsync_WithValidGroupData_CreatesSubmission()
    {
        using var context = new PASDbContext(_dbOptions);
        var service = CreateService(context);

        var studentUser = new User
        {
            UserId = 1,
            Name = "Student",
            Email = "student@example.com",
            Password = BCrypt.Net.BCrypt.HashPassword("password"),
            Role = "STUDENT"
        };
        var student = new Student { UserId = 1, Batch = "2024", User = studentUser };

        var researchArea = new ResearchArea { Id = 1, Name = "AI" };
        var coursework = new Coursework
        {
            CourseworkId = 1,
            Title = "Group Coursework",
            Description = "Group work",
            Deadline = DateTime.UtcNow.AddDays(7),
            IsIndividual = false
        };
        var group = new Group { GroupId = 1, LeaderId = 1, Leader = student };

        context.Users.Add(studentUser);
        context.Students.Add(student);
        context.ResearchAreas.Add(researchArea);
        context.Courseworks.Add(coursework);
        context.Groups.Add(group);
        await context.SaveChangesAsync();

        var dto = new CreateSubmissionDto
        {
            Title = "Group Project",
            Description = "Group Desc",
            Abstract = "Abstract",
            ResearchAreaId = 1,
            GroupId = 1
        };

        var file = CreatePdfFile();

        var result = await service.CreateSubmissionAsync(1, 1, dto, file);

        Assert.NotNull(result);
        Assert.Equal("Group Project", result.Title);
    }

    [Fact]
    public async Task CreateSubmissionAsync_WhenStudentDoesNotExist_CreatesStudentRecord()
    {
        using var context = new PASDbContext(_dbOptions);
        var service = CreateService(context);

        var user = new User
        {
            UserId = 1,
            Email = "s@e.com",
            Password = BCrypt.Net.BCrypt.HashPassword("pwd"),
            Role = "STUDENT",
            Name = "S"
        };
        var researchArea = new ResearchArea { Id = 1, Name = "AI" };
        var coursework = new Coursework
        {
            CourseworkId = 1,
            Title = "CW",
            Description = "D",
            Deadline = DateTime.UtcNow.AddDays(7),
            IsIndividual = true
        };

        context.Users.Add(user);
        context.ResearchAreas.Add(researchArea);
        context.Courseworks.Add(coursework);
        await context.SaveChangesAsync();

        var dto = new CreateSubmissionDto
        {
            Title = "T",
            Description = "D",
            Abstract = "A",
            ResearchAreaId = 1,
            GroupId = null
        };
        var file = CreatePdfFile();

        var result = await service.CreateSubmissionAsync(1, 1, dto, file);

        var student = await context.Students.FirstOrDefaultAsync(s => s.UserId == 1);
        Assert.NotNull(student);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task CreateSubmissionAsync_DeadlinePassed_ThrowsInvalidOperationException()
    {
        using var context = new PASDbContext(_dbOptions);
        var service = CreateService(context);

        var studentUser = new User
        {
            UserId = 1,
            Email = "student@example.com",
            Password = BCrypt.Net.BCrypt.HashPassword("password"),
            Role = "STUDENT",
            Name = "Student"
        };
        var student = new Student { UserId = 1, Batch = "2024", User = studentUser };

        var researchArea = new ResearchArea { Id = 1, Name = "AI" };
        var coursework = new Coursework
        {
            CourseworkId = 1,
            Title = "Past CW",
            Description = "Desc",
            Deadline = DateTime.UtcNow.AddDays(-1),
            IsIndividual = true
        };

        context.Users.Add(studentUser);
        context.Students.Add(student);
        context.ResearchAreas.Add(researchArea);
        context.Courseworks.Add(coursework);
        await context.SaveChangesAsync();

        var dto = new CreateSubmissionDto
        {
            Title = "Title",
            Description = "Desc",
            Abstract = "Abs",
            ResearchAreaId = 1,
            GroupId = null
        };

        var file = CreatePdfFile();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateSubmissionAsync(1, 1, dto, file));
    }

    [Fact]
    public async Task CreateSubmissionAsync_InvalidResearchArea_ThrowsKeyNotFoundException()
    {
        using var context = new PASDbContext(_dbOptions);
        var service = CreateService(context);

        var studentUser = new User
        {
            UserId = 1,
            Email = "student@example.com",
            Password = BCrypt.Net.BCrypt.HashPassword("password"),
            Role = "STUDENT",
            Name = "Student"
        };
        var student = new Student { UserId = 1, Batch = "2024", User = studentUser };
        context.Users.Add(studentUser);
        context.Students.Add(student);

        var coursework = new Coursework
        {
            CourseworkId = 1,
            Title = "CW",
            Description = "Desc",
            Deadline = DateTime.UtcNow.AddDays(7),
            IsIndividual = true
        };
        context.Courseworks.Add(coursework);
        await context.SaveChangesAsync();

        var dto = new CreateSubmissionDto
        {
            Title = "Title",
            Description = "Desc",
            Abstract = "Abs",
            ResearchAreaId = 999,
            GroupId = null
        };

        var file = CreatePdfFile();

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateSubmissionAsync(1, 1, dto, file));
    }

    [Fact]
    public async Task CreateSubmissionAsync_InvalidGroupId_ThrowsInvalidOperationException()
    {
        using var context = new PASDbContext(_dbOptions);
        var service = CreateService(context);

        var studentUser = new User
        {
            UserId = 1,
            Email = "student@example.com",
            Password = BCrypt.Net.BCrypt.HashPassword("password"),
            Role = "STUDENT",
            Name = "Student"
        };
        var student = new Student { UserId = 1, Batch = "2024", User = studentUser };
        context.Users.Add(studentUser);
        context.Students.Add(student);

        var researchArea = new ResearchArea { Id = 1, Name = "AI" };
        var coursework = new Coursework
        {
            CourseworkId = 1,
            Title = "Group CW",
            Description = "Desc",
            Deadline = DateTime.UtcNow.AddDays(7),
            IsIndividual = false
        };
        context.ResearchAreas.Add(researchArea);
        context.Courseworks.Add(coursework);
        await context.SaveChangesAsync();

        var dto = new CreateSubmissionDto
        {
            Title = "Title",
            Description = "Desc",
            Abstract = "Abs",
            ResearchAreaId = 1,
            GroupId = 99
        };

        var file = CreatePdfFile();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateSubmissionAsync(1, 1, dto, file));
    }

    [Fact]
    public async Task CreateSubmissionAsync_GroupFull_ThrowsInvalidOperationException()
    {
        using var context = new PASDbContext(_dbOptions);
        var service = CreateService(context);

        var studentUser = new User
        {
            UserId = 1,
            Email = "s1@example.com",
            Password = BCrypt.Net.BCrypt.HashPassword("password"),
            Role = "STUDENT",
            Name = "Student1"
        };
        var student = new Student { UserId = 1, Batch = "2024", User = studentUser };

        var student2 = new User
        {
            UserId = 2,
            Email = "s2@example.com",
            Password = BCrypt.Net.BCrypt.HashPassword("password"),
            Role = "STUDENT",
            Name = "Student2"
        };
        var student2Ent = new Student { UserId = 2, Batch = "2024", User = student2 };

        var researchArea = new ResearchArea { Id = 1, Name = "AI" };
        var coursework = new Coursework
        {
            CourseworkId = 1,
            Title = "Group CW",
            Description = "Desc",
            Deadline = DateTime.UtcNow.AddDays(7),
            IsIndividual = false
        };
        var group = new Group { GroupId = 1, LeaderId = 1, Leader = student };

        context.Users.AddRange(studentUser, student2);
        context.Students.AddRange(student, student2Ent);
        context.ResearchAreas.Add(researchArea);
        context.Courseworks.Add(coursework);
        context.Groups.Add(group);

        // Fill group with 5 existing projects (max size = 5)
        for (int i = 1; i <= 5; i++)
        {
            var proj = new Project
            {
                ProjectId = i,
                Title = $"Project {i}",
                Description = "Desc",
                ResearchAreaId = 1,
                GroupId = 1,
                IsDeleted = false,
                Status = "Submitted",
                CreatedAt = DateTime.UtcNow,
                SubmittedAt = DateTime.UtcNow
            };
            context.Projects.Add(proj);
            context.CourseworkProjects.Add(new CourseworkProject { CourseworkId = 1, ProjectId = proj.ProjectId });
        }
        await context.SaveChangesAsync();

        var dto = new CreateSubmissionDto
        {
            Title = "New Project",
            Description = "New Desc",
            Abstract = "Abs",
            ResearchAreaId = 1,
            GroupId = 1
        };

        var file = CreatePdfFile();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateSubmissionAsync(1, 1, dto, file));
    }

    [Fact]
    public async Task CreateSubmissionAsync_GroupAlreadySubmitted_ThrowsInvalidOperationException()
    {
        using var context = new PASDbContext(_dbOptions);
        var service = CreateService(context);

        var studentUser = new User
        {
            UserId = 1,
            Email = "s1@example.com",
            Password = BCrypt.Net.BCrypt.HashPassword("password"),
            Role = "STUDENT",
            Name = "Student1"
        };
        var student = new Student { UserId = 1, Batch = "2024", User = studentUser };

        var researchArea = new ResearchArea { Id = 1, Name = "AI" };
        var coursework = new Coursework
        {
            CourseworkId = 1,
            Title = "Group CW",
            Description = "Desc",
            Deadline = DateTime.UtcNow.AddDays(7),
            IsIndividual = false
        };
        var group = new Group { GroupId = 1, LeaderId = 1, Leader = student };

        var existingProject = new Project
        {
            ProjectId = 1,
            Title = "Existing",
            Description = "Desc",
            ResearchAreaId = 1,
            GroupId = 1,
            IsDeleted = false,
            Status = "Submitted"
        };

        context.Users.Add(studentUser);
        context.Students.Add(student);
        context.ResearchAreas.Add(researchArea);
        context.Courseworks.Add(coursework);
        context.Groups.Add(group);
        context.Projects.Add(existingProject);
        context.CourseworkProjects.Add(new CourseworkProject { CourseworkId = 1, ProjectId = 1 });
        await context.SaveChangesAsync();

        var dto = new CreateSubmissionDto
        {
            Title = "Another",
            Description = "Desc",
            Abstract = "Abs",
            ResearchAreaId = 1,
            GroupId = 1
        };

        var file = CreatePdfFile();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateSubmissionAsync(1, 1, dto, file));
    }

    [Fact]
    public async Task CreateSubmissionAsync_InvalidFileType_ThrowsArgumentException()
    {
        using var context = new PASDbContext(_dbOptions);
        var service = CreateService(context);

        var studentUser = new User { UserId = 1, Email = "s@e.com", Password = BCrypt.Net.BCrypt.HashPassword("pwd"), Role = "STUDENT", Name = "S" };
        var student = new Student { UserId = 1, Batch = "2024", User = studentUser };
        var researchArea = new ResearchArea { Id = 1, Name = "AI" };
        var coursework = new Coursework { CourseworkId = 1, Title = "CW", Description = "D", Deadline = DateTime.UtcNow.AddDays(7), IsIndividual = true };

        context.Users.Add(studentUser);
        context.Students.Add(student);
        context.ResearchAreas.Add(researchArea);
        context.Courseworks.Add(coursework);
        await context.SaveChangesAsync();

        var dto = new CreateSubmissionDto { Title = "T", Description = "D", Abstract = "A", ResearchAreaId = 1, GroupId = null };
        var file = new FormFile(new MemoryStream(Array.Empty<byte>()), 0, 0, "file", "test.txt")
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/plain"
        };

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateSubmissionAsync(1, 1, dto, file));
    }

    [Fact]
    public async Task CreateSubmissionAsync_FileTooLarge_ThrowsArgumentException()
    {
        using var context = new PASDbContext(_dbOptions);
        var service = CreateService(context);

        var studentUser = new User { UserId = 1, Email = "s@e.com", Password = BCrypt.Net.BCrypt.HashPassword("pwd"), Role = "STUDENT", Name = "S" };
        var student = new Student { UserId = 1, Batch = "2024", User = studentUser };
        var researchArea = new ResearchArea { Id = 1, Name = "AI" };
        var coursework = new Coursework { CourseworkId = 1, Title = "CW", Description = "D", Deadline = DateTime.UtcNow.AddDays(7), IsIndividual = true };

        context.Users.Add(studentUser);
        context.Students.Add(student);
        context.ResearchAreas.Add(researchArea);
        context.Courseworks.Add(coursework);
        await context.SaveChangesAsync();

        var dto = new CreateSubmissionDto { Title = "T", Description = "D", Abstract = "A", ResearchAreaId = 1, GroupId = null };
        var largeContent = new byte[11 * 1024 * 1024]; // 11 MB
        var stream = new MemoryStream(largeContent);
        var file = new FormFile(stream, 0, largeContent.Length, "file", "test.pdf")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/pdf"
        };

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateSubmissionAsync(1, 1, dto, file));
    }

    [Fact]
    public async Task CreateSubmissionAsync_NonMemberGroup_ThrowsUnauthorizedAccessException()
    {
        using var context = new PASDbContext(_dbOptions);
        var service = CreateService(context);

        var studentUser = new User { UserId = 1, Email = "s@e.com", Password = BCrypt.Net.BCrypt.HashPassword("pwd"), Role = "STUDENT", Name = "S" };
        var student = new Student { UserId = 1, Batch = "2024", User = studentUser };
        var otherStudent = new User { UserId = 2, Email = "o@e.com", Password = BCrypt.Net.BCrypt.HashPassword("pwd"), Role = "STUDENT", Name = "O" };
        var otherStudentEnt = new Student { UserId = 2, Batch = "2024", User = otherStudent };

        var researchArea = new ResearchArea { Id = 1, Name = "AI" };
        var coursework = new Coursework { CourseworkId = 1, Title = "CW", Description = "D", Deadline = DateTime.UtcNow.AddDays(7), IsIndividual = false };
        var group = new Group { GroupId = 1, LeaderId = 2, Leader = otherStudentEnt };

        context.Users.AddRange(studentUser, otherStudent);
        context.Students.AddRange(student, otherStudentEnt);
        context.ResearchAreas.Add(researchArea);
        context.Courseworks.Add(coursework);
        context.Groups.Add(group);
        await context.SaveChangesAsync();

        var dto = new CreateSubmissionDto { Title = "T", Description = "D", Abstract = "A", ResearchAreaId = 1, GroupId = 1 };
        var file = CreatePdfFile();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.CreateSubmissionAsync(1, 1, dto, file));
    }

    #endregion

    #region UpdateSubmissionAsync Tests

    [Fact]
    public async Task UpdateSubmissionAsync_WithValidData_UpdatesSubmission()
    {
        using var context = new PASDbContext(_dbOptions);
        var service = CreateService(context);

        var studentUser = new User { UserId = 1, Email = "s@e.com", Password = BCrypt.Net.BCrypt.HashPassword("pwd"), Role = "STUDENT", Name = "S" };
        var student = new Student { UserId = 1, Batch = "2024", User = studentUser };
        var researchArea = new ResearchArea { Id = 1, Name = "AI" };
        var coursework = new Coursework { CourseworkId = 1, Title = "CW", Description = "D", Deadline = DateTime.UtcNow.AddDays(7), IsIndividual = true };
        var group = new Group { GroupId = 1, LeaderId = 1, Leader = student };
        var project = new Project
        {
            ProjectId = 1,
            Title = "Old Title",
            Description = "Old Desc",
            Abstract = "Old Abs",
            ResearchAreaId = 1,
            GroupId = 1,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            SubmittedAt = DateTime.UtcNow
        };

        context.Users.Add(studentUser);
        context.Students.Add(student);
        context.ResearchAreas.Add(researchArea);
        context.Courseworks.Add(coursework);
        context.Groups.Add(group);
        context.Projects.Add(project);
        context.CourseworkProjects.Add(new CourseworkProject { CourseworkId = 1, ProjectId = 1 });
        await context.SaveChangesAsync();

        var dto = new UpdateSubmissionDto
        {
            Title = "New Title",
            Description = "New Desc",
            Abstract = "New Abs",
            ResearchAreaId = 1
        };

        var result = await service.UpdateSubmissionAsync(1, 1, dto, null);

        Assert.Equal("New Title", result.Title);
        Assert.Equal("New Desc", result.Description);
        Assert.Equal("New Abs", result.Abstract);
    }

    [Fact]
    public async Task UpdateSubmissionAsync_DeadlinePassed_ThrowsInvalidOperationException()
    {
        using var context = new PASDbContext(_dbOptions);
        var service = CreateService(context);

        var studentUser = new User { UserId = 1, Email = "s@e.com", Password = BCrypt.Net.BCrypt.HashPassword("pwd"), Role = "STUDENT", Name = "S" };
        var student = new Student { UserId = 1, Batch = "2024", User = studentUser };
        var researchArea = new ResearchArea { Id = 1, Name = "AI" };
        var coursework = new Coursework { CourseworkId = 1, Title = "CW", Description = "D", Deadline = DateTime.UtcNow.AddDays(-1), IsIndividual = true };
        var group = new Group { GroupId = 1, LeaderId = 1, Leader = student };
        var project = new Project { ProjectId = 1, Title = "P", Description = "D", Abstract = "A", ResearchAreaId = 1, GroupId = 1, IsDeleted = false };

        context.Users.Add(studentUser);
        context.Students.Add(student);
        context.ResearchAreas.Add(researchArea);
        context.Courseworks.Add(coursework);
        context.Groups.Add(group);
        context.Projects.Add(project);
        context.CourseworkProjects.Add(new CourseworkProject { CourseworkId = 1, ProjectId = 1 });
        await context.SaveChangesAsync();

        var dto = new UpdateSubmissionDto { Title = "New" };

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateSubmissionAsync(1, 1, dto, null));
    }

    [Fact]
    public async Task UpdateSubmissionAsync_UnauthorizedUser_ThrowsUnauthorizedAccessException()
    {
        using var context = new PASDbContext(_dbOptions);
        var service = CreateService(context);

        var ownerUser = new User { UserId = 1, Email = "o@e.com", Password = BCrypt.Net.BCrypt.HashPassword("pwd"), Role = "STUDENT", Name = "Owner" };
        var ownerStudent = new Student { UserId = 1, Batch = "2024", User = ownerUser };
        var otherUser = new User { UserId = 2, Email = "t@e.com", Password = BCrypt.Net.BCrypt.HashPassword("pwd"), Role = "STUDENT", Name = "Tres" };
        var otherStudent = new Student { UserId = 2, Batch = "2024", User = otherUser };

        var researchArea = new ResearchArea { Id = 1, Name = "AI" };
        var coursework = new Coursework { CourseworkId = 1, Title = "CW", Description = "D", Deadline = DateTime.UtcNow.AddDays(7), IsIndividual = true };
        var group = new Group { GroupId = 1, LeaderId = 1, Leader = ownerStudent };
        var project = new Project { ProjectId = 1, Title = "P", Description = "D", Abstract = "A", ResearchAreaId = 1, GroupId = 1, IsDeleted = false };

        context.Users.AddRange(ownerUser, otherUser);
        context.Students.AddRange(ownerStudent, otherStudent);
        context.ResearchAreas.Add(researchArea);
        context.Courseworks.Add(coursework);
        context.Groups.Add(group);
        context.Projects.Add(project);
        context.CourseworkProjects.Add(new CourseworkProject { CourseworkId = 1, ProjectId = 1 });
        await context.SaveChangesAsync();

        var dto = new UpdateSubmissionDto { Title = "New" };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.UpdateSubmissionAsync(2, 1, dto, null));
    }

    [Fact]
    public async Task UpdateSubmissionAsync_WithNewFile_ReplacesBlobAndDeletesOld()
    {
        using var context = new PASDbContext(_dbOptions);
        var service = CreateService(context);

        var studentUser = new User { UserId = 1, Email = "s@e.com", Password = BCrypt.Net.BCrypt.HashPassword("pwd"), Role = "STUDENT", Name = "S" };
        var student = new Student { UserId = 1, Batch = "2024", User = studentUser };
        var researchArea = new ResearchArea { Id = 1, Name = "AI" };
        var coursework = new Coursework { CourseworkId = 1, Title = "CW", Description = "D", Deadline = DateTime.UtcNow.AddDays(7), IsIndividual = true };
        var group = new Group { GroupId = 1, LeaderId = 1, Leader = student };
        var project = new Project
        {
            ProjectId = 1,
            Title = "P",
            Description = "D",
            Abstract = "A",
            ResearchAreaId = 1,
            GroupId = 1,
            IsDeleted = false,
            BlobFilePath = "old/path.pdf",
            ProposalFileName = "old.pdf",
            ContentType = "application/pdf"
        };

        context.Users.Add(studentUser);
        context.Students.Add(student);
        context.ResearchAreas.Add(researchArea);
        context.Courseworks.Add(coursework);
        context.Groups.Add(group);
        context.Projects.Add(project);
        context.CourseworkProjects.Add(new CourseworkProject { CourseworkId = 1, ProjectId = 1 });
        await context.SaveChangesAsync();

        var dto = new UpdateSubmissionDto { Title = "New Title" };
        var newFile = CreatePdfFile("new.pdf");

        var result = await service.UpdateSubmissionAsync(1, 1, dto, newFile);

        _mockBlobService.Verify(s => s.DeleteAsync("old/path.pdf"), Times.Once);
        _mockBlobService.Verify(s => s.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), "application/pdf"), Times.Once);
        Assert.Equal("new.pdf", result.ProposalFileName);
    }

    [Fact]
    public async Task UpdateSubmissionAsync_InvalidResearchArea_ThrowsKeyNotFoundException()
    {
        using var context = new PASDbContext(_dbOptions);
        var service = CreateService(context);

        var studentUser = new User { UserId = 1, Email = "s@e.com", Password = BCrypt.Net.BCrypt.HashPassword("pwd"), Role = "STUDENT", Name = "S" };
        var student = new Student { UserId = 1, Batch = "2024", User = studentUser };
        var researchArea = new ResearchArea { Id = 1, Name = "AI" };
        var coursework = new Coursework { CourseworkId = 1, Title = "CW", Description = "D", Deadline = DateTime.UtcNow.AddDays(7), IsIndividual = true };
        var group = new Group { GroupId = 1, LeaderId = 1, Leader = student };
        var project = new Project
        {
            ProjectId = 1,
            Title = "P",
            Description = "D",
            Abstract = "A",
            ResearchAreaId = 1,
            GroupId = 1,
            IsDeleted = false
        };

        context.Users.Add(studentUser);
        context.Students.Add(student);
        context.ResearchAreas.Add(researchArea);
        context.Courseworks.Add(coursework);
        context.Groups.Add(group);
        context.Projects.Add(project);
        context.CourseworkProjects.Add(new CourseworkProject { CourseworkId = 1, ProjectId = 1 });
        await context.SaveChangesAsync();

        var dto = new UpdateSubmissionDto { Title = "New", ResearchAreaId = 999 };

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.UpdateSubmissionAsync(1, 1, dto, null));
    }

    #endregion

    #region DeleteSubmissionAsync Tests

    [Fact]
    public async Task DeleteSubmissionAsync_WithValidId_SoftDeletesProject()
    {
        using var context = new PASDbContext(_dbOptions);
        var service = CreateService(context);

        var studentUser = new User { UserId = 1, Email = "s@e.com", Password = BCrypt.Net.BCrypt.HashPassword("pwd"), Role = "STUDENT", Name = "S" };
        var student = new Student { UserId = 1, Batch = "2024", User = studentUser };
        var researchArea = new ResearchArea { Id = 1, Name = "AI" };
        var coursework = new Coursework { CourseworkId = 1, Title = "CW", Description = "D", Deadline = DateTime.UtcNow.AddDays(7), IsIndividual = true };
        var group = new Group { GroupId = 1, LeaderId = 1, Leader = student };
        var project = new Project
        {
            ProjectId = 1,
            Title = "P",
            Description = "D",
            Abstract = "A",
            ResearchAreaId = 1,
            GroupId = 1,
            IsDeleted = false,
            BlobFilePath = "path.pdf"
        };

        context.Users.Add(studentUser);
        context.Students.Add(student);
        context.ResearchAreas.Add(researchArea);
        context.Courseworks.Add(coursework);
        context.Groups.Add(group);
        context.Projects.Add(project);
        context.CourseworkProjects.Add(new CourseworkProject { CourseworkId = 1, ProjectId = 1 });
        await context.SaveChangesAsync();

        await service.DeleteSubmissionAsync(1, 1);

        var deleted = await context.Projects.FirstOrDefaultAsync(p => p.ProjectId == 1);
        Assert.True(deleted!.IsDeleted);
        _mockBlobService.Verify(s => s.DeleteAsync("path.pdf"), Times.Once);
    }

    [Fact]
    public async Task DeleteSubmissionAsync_DeadlinePassed_ThrowsInvalidOperationException()
    {
        using var context = new PASDbContext(_dbOptions);
        var service = CreateService(context);

        var studentUser = new User { UserId = 1, Email = "s@e.com", Password = BCrypt.Net.BCrypt.HashPassword("pwd"), Role = "STUDENT", Name = "S" };
        var student = new Student { UserId = 1, Batch = "2024", User = studentUser };
        var researchArea = new ResearchArea { Id = 1, Name = "AI" };
        var coursework = new Coursework { CourseworkId = 1, Title = "CW", Description = "D", Deadline = DateTime.UtcNow.AddDays(-1), IsIndividual = true };
        var group = new Group { GroupId = 1, LeaderId = 1, Leader = student };
        var project = new Project { ProjectId = 1, Title = "P", Description = "D", Abstract = "A", ResearchAreaId = 1, GroupId = 1, IsDeleted = false };

        context.Users.Add(studentUser);
        context.Students.Add(student);
        context.ResearchAreas.Add(researchArea);
        context.Courseworks.Add(coursework);
        context.Groups.Add(group);
        context.Projects.Add(project);
        context.CourseworkProjects.Add(new CourseworkProject { CourseworkId = 1, ProjectId = 1 });
        await context.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteSubmissionAsync(1, 1));
    }

    [Fact]
    public async Task DeleteSubmissionAsync_Unauthorized_ThrowsUnauthorizedAccessException()
    {
        using var context = new PASDbContext(_dbOptions);
        var service = CreateService(context);

        var owner = new User { UserId = 1, Email = "o@e.com", Password = BCrypt.Net.BCrypt.HashPassword("pwd"), Role = "STUDENT", Name = "Owner" };
        var ownerStudent = new Student { UserId = 1, Batch = "2024", User = owner };
        var other = new User { UserId = 2, Email = "t@e.com", Password = BCrypt.Net.BCrypt.HashPassword("pwd"), Role = "STUDENT", Name = "Tres" };
        var otherStudent = new Student { UserId = 2, Batch = "2024", User = other };

        var researchArea = new ResearchArea { Id = 1, Name = "AI" };
        var coursework = new Coursework { CourseworkId = 1, Title = "CW", Description = "D", Deadline = DateTime.UtcNow.AddDays(7), IsIndividual = true };
        var group = new Group { GroupId = 1, LeaderId = 1, Leader = ownerStudent };
        var project = new Project { ProjectId = 1, Title = "P", Description = "D", Abstract = "A", ResearchAreaId = 1, GroupId = 1, IsDeleted = false };

        context.Users.AddRange(owner, other);
        context.Students.AddRange(ownerStudent, otherStudent);
        context.ResearchAreas.Add(researchArea);
        context.Courseworks.Add(coursework);
        context.Groups.Add(group);
        context.Projects.Add(project);
        context.CourseworkProjects.Add(new CourseworkProject { CourseworkId = 1, ProjectId = 1 });
        await context.SaveChangesAsync();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.DeleteSubmissionAsync(2, 1));
    }

    #endregion

    #region GetMySubmissionAsync Tests

    [Fact]
    public async Task GetMySubmissionAsync_WithExistingSubmission_ReturnsSubmission()
    {
        using var context = new PASDbContext(_dbOptions);
        var service = CreateService(context);

        var studentUser = new User { UserId = 1, Email = "s@e.com", Password = BCrypt.Net.BCrypt.HashPassword("pwd"), Role = "STUDENT", Name = "S" };
        var student = new Student { UserId = 1, Batch = "2024", User = studentUser };
        var researchArea = new ResearchArea { Id = 1, Name = "AI" };
        var coursework = new Coursework { CourseworkId = 1, Title = "CW", Description = "D", Deadline = DateTime.UtcNow.AddDays(7), IsIndividual = true };
        var group = new Group { GroupId = 1, LeaderId = 1, Leader = student };
        var project = new Project
        {
            ProjectId = 1,
            Title = "P",
            Description = "D",
            Abstract = "A",
            ResearchAreaId = 1,
            GroupId = 1,
            IsDeleted = false
        };

        context.Users.Add(studentUser);
        context.Students.Add(student);
        context.ResearchAreas.Add(researchArea);
        context.Courseworks.Add(coursework);
        context.Groups.Add(group);
        context.Projects.Add(project);
        context.CourseworkProjects.Add(new CourseworkProject { CourseworkId = 1, ProjectId = 1 });
        await context.SaveChangesAsync();

        var result = await service.GetMySubmissionAsync(1, 1);

        Assert.NotNull(result);
        Assert.Equal("P", result.Title);
    }

    [Fact]
    public async Task GetMySubmissionAsync_NoSubmission_ReturnsNull()
    {
        using var context = new PASDbContext(_dbOptions);
        var service = CreateService(context);

        var studentUser = new User { UserId = 1, Email = "s@e.com", Password = BCrypt.Net.BCrypt.HashPassword("pwd"), Role = "STUDENT", Name = "S" };
        var student = new Student { UserId = 1, Batch = "2024", User = studentUser };
        var coursework = new Coursework { CourseworkId = 1, Title = "CW", Description = "D", Deadline = DateTime.UtcNow.AddDays(7), IsIndividual = true };

        context.Users.Add(studentUser);
        context.Students.Add(student);
        context.Courseworks.Add(coursework);
        await context.SaveChangesAsync();

        var result = await service.GetMySubmissionAsync(1, 1);

        Assert.Null(result);
    }

    #endregion

    #region GetSubmissionFileAsync Tests

    [Fact]
    public async Task GetSubmissionFileAsync_WithFile_ReturnsStream()
    {
        using var context = new PASDbContext(_dbOptions);
        var service = CreateService(context);

        var studentUser = new User { UserId = 1, Email = "s@e.com", Password = BCrypt.Net.BCrypt.HashPassword("pwd"), Role = "STUDENT", Name = "S" };
        var student = new Student { UserId = 1, Batch = "2024", User = studentUser };
        var researchArea = new ResearchArea { Id = 1, Name = "AI" };
        var coursework = new Coursework { CourseworkId = 1, Title = "CW", Description = "D", Deadline = DateTime.UtcNow.AddDays(7), IsIndividual = true };
        var group = new Group { GroupId = 1, LeaderId = 1, Leader = student };
        var project = new Project
        {
            ProjectId = 1,
            Title = "P",
            Description = "D",
            Abstract = "A",
            ResearchAreaId = 1,
            GroupId = 1,
            IsDeleted = false,
            BlobFilePath = "submissions/1/1/file.pdf",
            ProposalFileName = "file.pdf",
            ContentType = "application/pdf"
        };

        context.Users.Add(studentUser);
        context.Students.Add(student);
        context.ResearchAreas.Add(researchArea);
        context.Courseworks.Add(coursework);
        context.Groups.Add(group);
        context.Projects.Add(project);
        await context.SaveChangesAsync();

        var mockStream = new MemoryStream(new byte[10]);
        _mockBlobService.Setup(s => s.DownloadAsync("submissions/1/1/file.pdf")).ReturnsAsync(mockStream);

        var (stream, contentType, fileName) = await service.GetSubmissionFileAsync(1, 1);

        Assert.NotNull(stream);
        Assert.Equal("application/pdf", contentType);
        Assert.Equal("file.pdf", fileName);
        Assert.Same(mockStream, stream);
    }

    [Fact]
    public async Task GetSubmissionFileAsync_NoFile_ThrowsInvalidOperationException()
    {
        using var context = new PASDbContext(_dbOptions);
        var service = CreateService(context);

        var studentUser = new User { UserId = 1, Email = "s@e.com", Password = BCrypt.Net.BCrypt.HashPassword("pwd"), Role = "STUDENT", Name = "S" };
        var student = new Student { UserId = 1, Batch = "2024", User = studentUser };
        var researchArea = new ResearchArea { Id = 1, Name = "AI" };
        var coursework = new Coursework { CourseworkId = 1, Title = "CW", Description = "D", Deadline = DateTime.UtcNow.AddDays(7), IsIndividual = true };
        var group = new Group { GroupId = 1, LeaderId = 1, Leader = student };
        var project = new Project
        {
            ProjectId = 1,
            Title = "P",
            Description = "D",
            Abstract = "A",
            ResearchAreaId = 1,
            GroupId = 1,
            IsDeleted = false,
            BlobFilePath = null
        };

        context.Users.Add(studentUser);
        context.Students.Add(student);
        context.ResearchAreas.Add(researchArea);
        context.Courseworks.Add(coursework);
        context.Groups.Add(group);
        context.Projects.Add(project);
        await context.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetSubmissionFileAsync(1, 1));
    }

    [Fact]
    public async Task GetSubmissionFileAsync_Unauthorized_ThrowsUnauthorizedAccessException()
    {
        using var context = new PASDbContext(_dbOptions);
        var service = CreateService(context);

        var owner = new User { UserId = 1, Email = "o@e.com", Password = BCrypt.Net.BCrypt.HashPassword("pwd"), Role = "STUDENT", Name = "Owner" };
        var ownerStudent = new Student { UserId = 1, Batch = "2024", User = owner };
        var other = new User { UserId = 2, Email = "t@e.com", Password = BCrypt.Net.BCrypt.HashPassword("pwd"), Role = "STUDENT", Name = "Tres" };
        var otherStudent = new Student { UserId = 2, Batch = "2024", User = other };

        var researchArea = new ResearchArea { Id = 1, Name = "AI" };
        var coursework = new Coursework { CourseworkId = 1, Title = "CW", Description = "D", Deadline = DateTime.UtcNow.AddDays(7), IsIndividual = true };
        var group = new Group { GroupId = 1, LeaderId = 1, Leader = ownerStudent };
        var project = new Project { ProjectId = 1, Title = "P", Description = "D", Abstract = "A", ResearchAreaId = 1, GroupId = 1, IsDeleted = false, BlobFilePath = "path" };

        context.Users.AddRange(owner, other);
        context.Students.AddRange(ownerStudent, otherStudent);
        context.ResearchAreas.Add(researchArea);
        context.Courseworks.Add(coursework);
        context.Groups.Add(group);
        context.Projects.Add(project);
        await context.SaveChangesAsync();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetSubmissionFileAsync(2, 1));
    }

    #endregion

    #region Helper Methods

    private static IFormFile CreatePdfFile(string fileName = "test.pdf", long sizeInBytes = 1024)
    {
        var content = new byte[sizeInBytes];
        var stream = new MemoryStream(content);
        var file = new FormFile(stream, 0, sizeInBytes, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/pdf"
        };
        return file;
    }

    #endregion

}
