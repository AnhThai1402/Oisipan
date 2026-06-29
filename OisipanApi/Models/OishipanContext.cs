using Microsoft.EntityFrameworkCore;

namespace Oishipan.Models
{
    public class OishipanContext : DbContext
    {
        public OishipanContext(DbContextOptions<OishipanContext> options) : base(options) { }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductVariant> ProductVariants { get; set; }
        public DbSet<ProductOption> ProductOptions { get; set; }
        public DbSet<ProductValue> ProductValues { get; set; }
        public DbSet<Account> Accounts { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public virtual DbSet<Voucher> Vouchers { get; set; }
        public virtual DbSet<UserAddress> UserAddresses { get; set; }
        public DbSet<NewsArticle> NewsArticles { get; set; }
        public DbSet<OrderCancellationRequest> OrderCancellationRequests { get; set; }
        public DbSet<UserVoucher> UserVouchers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Cấu hình Unique cho Email và Phone trong bảng Account
            modelBuilder.Entity<Account>()
                .HasIndex(a => a.Email).IsUnique();
            modelBuilder.Entity<Account>()
                .HasIndex(a => a.PhoneNumber).IsUnique();

            // Cấu hình Unique cho Voucher Code
            modelBuilder.Entity<Voucher>()
                .HasIndex(v => v.Code).IsUnique();

            modelBuilder.Entity<ProductOption>()
                .HasIndex(po => new { po.ProductId, po.OptionName })
                .IsUnique();

            modelBuilder.Entity<ProductValue>()
                .HasIndex(pv => new { pv.ProductOptionId, pv.ValueName })
                .IsUnique();

            modelBuilder.Entity<ProductVariant>()
                .HasIndex(variant => new { variant.ProductId, variant.Size, variant.Filling })
                .IsUnique();

            // UserVoucher relationships
            modelBuilder.Entity<UserVoucher>()
                .HasOne(uv => uv.Account)
                .WithMany()
                .HasForeignKey(uv => uv.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserVoucher>()
                .HasOne(uv => uv.Voucher)
                .WithMany()
                .HasForeignKey(uv => uv.VoucherId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
