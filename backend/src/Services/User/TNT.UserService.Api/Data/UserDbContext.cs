using Microsoft.EntityFrameworkCore;
using TNT.UserService.Api.Entities;

namespace TNT.UserService.Api.Data;

/// <summary>
/// EF Core database context for the User Service.
/// Owns: UserProfiles.
/// This database is private to the User Service only.
/// </summary>
public class UserDbContext : DbContext
{
    public UserDbContext(DbContextOptions<UserDbContext> options) : base(options) { }

    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.ToTable("UserProfiles");
            entity.HasKey(p => p.Id);

            entity.Property(p => p.DisplayName).HasMaxLength(200);
            entity.Property(p => p.PhoneNumber).HasMaxLength(30);
            entity.Property(p => p.Address).HasMaxLength(500);
            entity.Property(p => p.City).HasMaxLength(100);
            entity.Property(p => p.CreatedAtUtc).IsRequired();

            // Each identity user has at most one profile
            entity.HasIndex(p => p.IdentityUserId)
                  .IsUnique()
                  .HasDatabaseName("IX_UserProfiles_IdentityUserId");
        });
    }
}
