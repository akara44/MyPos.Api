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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ProductGroup>()
                .HasOne(pg => pg.ParentGroup)
                .WithMany(pg => pg.SubGroups)
                .HasForeignKey(pg => pg.ParentGroupId)
                .OnDelete(DeleteBehavior.Restrict); // Özyinelemeli silmeyi engeller

            // Product ve ProductGroup arasındaki ilişkiyi yapılandırın
            modelBuilder.Entity<Product>()
                .HasOne(p => p.ProductGroup) // Bir Ürünün bir Ürün Grubu vardır
                .WithMany() // Bir Ürün Grubunun birçok Ürünü olabilir (ProductGroup'ta Ürünler için gezinme özelliğine gerek yok)
                .HasForeignKey(p => p.ProductGroupId) // Yabancı anahtar ProductGroupId'dir
                .OnDelete(DeleteBehavior.Restrict); // Ürün grubunun, ona bağlı ürünler varsa silinmesini engeller
        }
    }
}