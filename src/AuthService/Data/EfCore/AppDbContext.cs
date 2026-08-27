using AuthService.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Data.EfCore;

public class AppDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public AppDbContext(DbContextOptions<AppDbContext> options)
        :base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasConversion(
                v => v.ToString(), 
                v => Ulid.Parse(v));

            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(256);

            entity.HasIndex(e => e.Email).IsUnique();

            entity.Property(e => e.Role)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.RefreshTokenHash)
                .HasMaxLength(512);
        });
    }
}