using Microsoft.EntityFrameworkCore;
using Services.Entities;

namespace AdessoLeague.Repositories.Contexts;
public class AdessoLeagueDbContext : DbContext
{
    public AdessoLeagueDbContext(DbContextOptions<AdessoLeagueDbContext> options) : base(options) { }

    public DbSet<Team> Teams { get; set; }
    public DbSet<Group> Groups { get; set; }
    public DbSet<Draw> Draws { get; set; }
    public DbSet<GroupTeam> GroupTeams { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Team>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Country).IsRequired().HasMaxLength(50);
            entity.Property(e => e.City).IsRequired().HasMaxLength(50);
        });

        modelBuilder.Entity<Draw>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DrawnBy).IsRequired().HasMaxLength(100);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.NumberOfGroups).IsRequired();
        });

        modelBuilder.Entity<Group>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(10);
            entity.HasOne(e => e.Draw)
                  .WithMany(d => d.Groups)
                  .HasForeignKey(e => e.DrawId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

         modelBuilder.Entity<GroupTeam>(entity =>
        {
            entity.HasKey(e => new { e.GroupId, e.TeamId });

            entity.HasOne(e => e.Group)
                  .WithMany(g => g.GroupTeams)
                  .HasForeignKey(e => e.GroupId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Team)
                  .WithMany()
                  .HasForeignKey(e => e.TeamId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        base.OnModelCreating(modelBuilder);
    }
}