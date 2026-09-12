using Microsoft.EntityFrameworkCore;
using TNT.ProductService.Api.Entities;

namespace TNT.ProductService.Api.Data;

public class ProductDbContext : DbContext
{
    public ProductDbContext(DbContextOptions<ProductDbContext> options) : base(options) { }

    public DbSet<Store> Stores => Set<Store>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Store>(entity =>
        {
            entity.ToTable("Stores");
            entity.HasKey(store => store.Id);

            entity.Property(store => store.StoreCode).IsRequired().HasMaxLength(50);
            entity.Property(store => store.StoreName).IsRequired().HasMaxLength(200);
            entity.Property(store => store.AddressLine1).IsRequired().HasMaxLength(250);
            entity.Property(store => store.AddressLine2).HasMaxLength(250);
            entity.Property(store => store.City).IsRequired().HasMaxLength(100);
            entity.Property(store => store.PostalCode).IsRequired().HasMaxLength(30);
            entity.Property(store => store.Country).IsRequired().HasMaxLength(100);
            entity.Property(store => store.ContactNumber).IsRequired().HasMaxLength(30);
            entity.Property(store => store.Email).IsRequired().HasMaxLength(254);
            entity.Property(store => store.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(store => store.CreatedAtUtc).IsRequired();

            entity.HasIndex(store => store.StoreCode)
                .IsUnique()
                .HasDatabaseName("IX_Stores_StoreCode");
        });
    }
}
