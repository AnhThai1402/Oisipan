using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;
using System.Security.Principal;
using LoginNumPhone.Models;

namespace LoginNumPhone
{
    public class OishipanContext : DbContext
    {
        public OishipanContext(DbContextOptions<OishipanContext> options) : base(options) { }

        //public DbSet<Category> Categories { get; set; }
        //public DbSet<Product> Products { get; set; }
        //public DbSet<ProductOption> ProductOptions { get; set; }
        //public DbSet<ProductValue> ProductValues { get; set; }
        public DbSet<Account> Accounts { get; set; }
        //public DbSet<Order> Orders { get; set; }
        //public DbSet<OrderDetail> OrderDetails { get; set; }
        //public DbSet<Payment> Payments { get; set; }
        //public DbSet<Voucher> Vouchers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Cấu hình Unique cho Email và Phone trong bảng Account
            modelBuilder.Entity<Account>()
                .HasIndex(a => a.Email).IsUnique();
            modelBuilder.Entity<Account>()
                .HasIndex(a => a.PhoneNumber).IsUnique();

            // Cấu hình Unique cho Voucher Code
            //modelBuilder.Entity<Voucher>()
            //    .HasIndex(v => v.Code).IsUnique();

            //modelBuilder.Entity<ProductOption>()
            //    .HasIndex(po => new { po.ProductId, po.OptionName })
            //    .IsUnique();

            //modelBuilder.Entity<ProductValue>()
            //    .HasIndex(pv => new { pv.ProductOptionId, pv.ValueName })
            //    .IsUnique();
        }
    }
}