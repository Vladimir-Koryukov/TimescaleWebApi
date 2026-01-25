using Microsoft.EntityFrameworkCore;
using TimescaleWebApi.Entities;

namespace TimescaleWebApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<FileValue> Values => Set<FileValue>();
    public DbSet<FileResult> Results => Set<FileResult>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<FileValue>()
            .HasIndex(x => new { x.FileName, x.Date });

        modelBuilder.Entity<FileResult>()
            .HasIndex(x => x.FileName)
            .IsUnique();
    }
}