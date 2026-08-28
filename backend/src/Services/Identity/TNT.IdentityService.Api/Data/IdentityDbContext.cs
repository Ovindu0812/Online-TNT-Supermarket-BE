using Microsoft.EntityFrameworkCore;
using TNT.IdentityService.Api.Entities;

namespace TNT.IdentityService.Api.Data;

/// <summary>
/// EF Core database context for the Identity Service.
/// Owns: ApplicationUsers, RefreshTokens.
/// This database must NOT be accessed by any other service.
/// </summary>
public class IdentityDbContext : DbContext
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options) { }

    public DbSet<ApplicationUser> ApplicationUsers => Set<ApplicationUser>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // --- ApplicationUser ---
        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.ToTable("ApplicationUsers");
            entity.HasKey(u => u.Id);

            entity.Property(u => u.FullName).IsRequired().HasMaxLength(200);
            entity.Property(u => u.Email).IsRequired().HasMaxLength(320);
            entity.Property(u => u.NormalizedEmail).IsRequired().HasMaxLength(320);
            entity.Property(u => u.PasswordHash).IsRequired();
            entity.Property(u => u.PhoneNumber).HasMaxLength(30);
            entity.Property(u => u.Role).IsRequired().HasMaxLength(20).HasDefaultValue("Buyer");
            entity.Property(u => u.IsActive).HasDefaultValue(true);
            entity.Property(u => u.CreatedAtUtc).IsRequired();

            // Unique index on normalised email ensures case-insensitive uniqueness
            entity.HasIndex(u => u.NormalizedEmail)
                  .IsUnique()
                  .HasDatabaseName("IX_ApplicationUsers_NormalizedEmail");
        });

        // --- RefreshToken ---
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens");
            entity.HasKey(rt => rt.Id);

            entity.Property(rt => rt.TokenHash).IsRequired().HasMaxLength(512);
            entity.Property(rt => rt.ExpiresAtUtc).IsRequired();
            entity.Property(rt => rt.CreatedAtUtc).IsRequired();
            entity.Property(rt => rt.CreatedByIp).HasMaxLength(50);
            entity.Property(rt => rt.ReplacedByTokenHash).HasMaxLength(512);

            entity.HasIndex(rt => rt.UserId)
                  .HasDatabaseName("IX_RefreshTokens_UserId");

            entity.HasIndex(rt => rt.TokenHash)
                  .HasDatabaseName("IX_RefreshTokens_TokenHash");

            // FK to ApplicationUser — cascade delete removes tokens when user is deleted
            entity.HasOne(rt => rt.User)
                  .WithMany(u => u.RefreshTokens)
                  .HasForeignKey(rt => rt.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
