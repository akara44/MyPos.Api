// MyPosDbContext.cs (Güncellenmiş Kısım)
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
        public DbSet<VariantType> VariantTypes { get; set; }
        public DbSet<VariantValue> VariantValues { get; set; }
        // Yeni eklenenler
        public DbSet<ProductVariant> ProductVariants { get; set; }
        public DbSet<ProductVariantValue> ProductVariantValues { get; set; }
        public DbSet<Personnel> Personnel { get; set; }
        public DbSet<Company> Company { get; set; }

        public DbSet<PaymentType> PaymentTypes { get; set; }

        public DbSet<ExpenseIncomeType> ExpenseIncomeTypes { get; set; }

        public DbSet<Expense> Expenses { get; set; }
        public DbSet<Income> Incomes { get; set; }



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
                .WithMany() // ProductGroup'tan Product'a tek yönlü bir ilişki olabilir veya ProductGroup içinde Products koleksiyonu tanımlanabilir.
                .HasForeignKey(p => p.ProductGroupId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<VariantValue>()
                .HasOne(vv => vv.VariantType)
                .WithMany(vt => vt.VariantValues)
                .HasForeignKey(vv => vv.VariantTypeId)
                .OnDelete(DeleteBehavior.Restrict);
             
            // Yeni Varyant İlişkileri
            modelBuilder.Entity<ProductVariant>()
                .HasOne(pv => pv.Product)
                .WithMany(p => p.ProductVariants) // Product içinde ProductVariants koleksiyonunu tanımlamamız gerekecek
                .HasForeignKey(pv => pv.ProductId)
                .OnDelete(DeleteBehavior.Cascade); // Ana ürün silinince varyantları da silinsin

            modelBuilder.Entity<ProductVariantValue>()
                .HasOne(pvv => pvv.ProductVariant)
                .WithMany(pv => pv.ProductVariantValues)
                .HasForeignKey(pvv => pvv.ProductVariantId)
                .OnDelete(DeleteBehavior.Cascade); // Varyant silinince varyant değer ilişkisi de silinsin

            modelBuilder.Entity<ProductVariantValue>()
                .HasOne(pvv => pvv.VariantValue)
                .WithMany() // VariantValue'dan ProductVariantValue'ya tek yönlü bir ilişki olabilir
                .HasForeignKey(pvv => pvv.VariantValueId)
                .OnDelete(DeleteBehavior.Restrict); // Varyant değeri silinirken ilişkili ürün varyant değerleri kalmalı
        }
    }
}