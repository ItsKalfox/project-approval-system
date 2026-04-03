using Microsoft.EntityFrameworkCore;
using PAS.API.Models;

namespace PAS.API.Data;

public class PASDbContext : DbContext
{
    public PASDbContext(DbContextOptions<PASDbContext> options)
        : base(options)
    {
    }

    public DbSet<Student> Students { get; set; }
}