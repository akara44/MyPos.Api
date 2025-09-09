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
        public DbSet<ProductVariant> ProductVariants { get; set; }
        public DbSet<ProductVariantValue> ProductVariantValues { get; set; }
        public DbSet<Personnel> Personnel { get; set; }
        public DbSet<Company> Company { get; set; }
        public DbSet<PaymentType> PaymentTypes { get; set; }
        public DbSet<ExpenseIncomeType> ExpenseIncomeTypes { get; set; }
        public DbSet<Expense> Expenses { get; set; }
        public DbSet<Income> Incomes { get; set; }
        public DbSet<StockTransaction> StockTransaction { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<Payment> Payments { get; set; }

        public DbSet<PurchaseInvoice> PurchaseInvoices { get; set; }
        public DbSet<PurchaseInvoiceItem> PurchaseInvoiceItems { get; set; }
        public DbSet<Sale> Sales { get; set; }
        public DbSet<SaleItem> SaleItems { get; set; }
        public DbSet<CompanyTransaction> CompanyTransactions { get; set; }

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

            // Müşteri ve Sipariş arasındaki ilişkiyi belirtiyoruz.
            modelBuilder.Entity<Order>()
                .HasOne(o => o.Customer)
                .WithMany(c => c.Orders)
                .HasForeignKey(o => o.CustomerId)
                .IsRequired(false); // CustomerId'nin null olabileceğini belirtiyoruz.

            // Müşteri ve Ödeme arasındaki ilişkiyi belirtiyoruz.
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Customer)
                .WithMany(c => c.Payments)
                .HasForeignKey(p => p.CustomerId)
                .IsRequired(); // CustomerId'nin zorunlu olduğunu belirtiyoruz.
            // Yeni eklenen ilişki: PurchaseInvoice ve PaymentType
            modelBuilder.Entity<PurchaseInvoice>()
             .HasOne(pi => pi.PaymentType)
             .WithMany(pt => pt.PurchaseInvoices)
             .HasForeignKey(pi => pi.PaymentTypeId)
             .OnDelete(DeleteBehavior.SetNull); // Bir PaymentType silinince, ona bağlı faturaların PaymentTypeId'si NULL olur.

            // Eğer isterseniz, bir fatura silinince fatura kalemleri de silinsin diye bu ilişkiyi de ekleyebilirsiniz.
            // Bu zaten PurchaseInvoice'da PurchaseInvoiceItems koleksiyonu tanımlandığında varsayılan olarak Cascade olarak gelir.
            // Ama açıkça belirtmek kodun anlaşılırlığını artırır.
            modelBuilder.Entity<PurchaseInvoiceItem>()
                .HasOne(pi => pi.PurchaseInvoice)
                .WithMany(pi => pi.PurchaseInvoiceItems)
                .HasForeignKey(pi => pi.PurchaseInvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}