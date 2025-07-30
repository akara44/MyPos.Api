// MyPosDbContext.cs
using MyPos.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MyPos.Infrastructure.Persistence
{
    public class MyPosDbContext : DbContext
    {
        public MyPosDbContext(DbContextOptions<MyPosDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductGroup> ProductGroups { get; set; }
        public DbSet<VariantType> VariantTypes { get; set; } // Yeni eklendi
        public DbSet<VariantValue> VariantValues { get; set; } // Yeni eklendi

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ProductGroup>()
                .HasOne(pg => pg.ParentGroup)
                .WithMany(pg => pg.SubGroups)
                .HasForeignKey(pg => pg.ParentGroupId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Product>()
                .HasOne(p => p.ProductGroup)
                .WithMany()
                .HasForeignKey(p => p.ProductGroupId)
                .OnDelete(DeleteBehavior.Restrict);

            // VariantType ve VariantValue arasındaki ilişkiyi yapılandırın
            modelBuilder.Entity<VariantValue>()
                .HasOne(vv => vv.VariantType)
                .WithMany(vt => vt.VariantValues)
                .HasForeignKey(vv => vv.VariantTypeId)
                .OnDelete(DeleteBehavior.Restrict); // Varyant tipi silinirken bağlı değerlerin kalmasını engeller veya kısıtlar
        }
    }
}