using Microsoft.EntityFrameworkCore;
using PAS.API.Models;

namespace PAS.API.Data;

public class PASDbContext : DbContext
{
    public PASDbContext(DbContextOptions<PASDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Student> Students { get; set; }
    public DbSet<Supervisor> Supervisors { get; set; }
    public DbSet<ModuleLeader> ModuleLeaders { get; set; }
    public DbSet<ResearchArea> ResearchAreas { get; set; }
    public DbSet<Group> Groups { get; set; }
    public DbSet<Project> Projects { get; set; }
    public DbSet<Coursework> Courseworks { get; set; }
    public DbSet<CourseworkProject> CourseworkProjects { get; set; }
    public DbSet<Match> Matches { get; set; }
    public DbSet<Interest> Interests { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Composite key for many-to-many join table
        modelBuilder.Entity<CourseworkProject>()
            .HasKey(cp => new { cp.CourseworkId, cp.ProjectId });

        // One-to-one: User -> Student
        modelBuilder.Entity<Student>()
            .HasKey(s => s.UserId);

        modelBuilder.Entity<Student>()
            .HasOne(s => s.User)
            .WithOne(u => u.Student)
            .HasForeignKey<Student>(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // One-to-one: User -> Supervisor
        modelBuilder.Entity<Supervisor>()
            .HasKey(s => s.UserId);

        modelBuilder.Entity<Supervisor>()
            .HasOne(s => s.User)
            .WithOne(u => u.Supervisor)
            .HasForeignKey<Supervisor>(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // One-to-one: User -> ModuleLeader
        modelBuilder.Entity<ModuleLeader>()
            .HasKey(m => m.UserId);

        modelBuilder.Entity<ModuleLeader>()
            .HasOne(m => m.User)
            .WithOne(u => u.ModuleLeader)
            .HasForeignKey<ModuleLeader>(m => m.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique email
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();
    }
}