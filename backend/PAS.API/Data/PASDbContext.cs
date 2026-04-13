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
}