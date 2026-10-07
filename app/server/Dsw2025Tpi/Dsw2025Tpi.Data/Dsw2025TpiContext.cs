using Dsw2025Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dsw2025Tpi.Data;

public class Dsw2025TpiContext : DbContext
{
    public Dsw2025TpiContext(DbContextOptions<Dsw2025TpiContext> options) : base(options)
    {
    }
    //public DbSet<Product> Products { get; set; }
    //public DbSet<Order> Orders { get; set; }
    //public DbSet<Customer> Customers { get; set; }
    //public DbSet<OrderItem> OrderItems { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Product>(eb =>
        {
            eb.ToTable("Products");
            eb.HasKey(p => p.Id);
            eb.Property(p => p.Sku).IsRequired();
            eb.Property(p => p.InternalCode).HasMaxLength(20);
            eb.Property(p => p.Name).HasMaxLength(50);
            eb.Property(p => p.Description).HasMaxLength(60);
            eb.Property(p => p.CurrentUnitPrice).HasPrecision(15, 2);
            eb.HasIndex(p => p.Sku).IsUnique();

        });
        modelBuilder.Entity<Order>(eb =>
        {
            eb.ToTable("Orders");
            eb.Property(p => p.ShippingAddress).HasMaxLength(60);
            eb.Property(p => p.BillingAddress).HasMaxLength(60);
            eb.Property(p => p.Notes).HasMaxLength(60);
            eb.HasKey(p => p.Id);
        });
        modelBuilder.Entity<Customer>(eb =>
        {
            eb.ToTable("Customers");
            eb.HasKey(c => c.Id);
        });
        modelBuilder.Entity<OrderItem>(eb =>
        {
            eb.ToTable("OrderItems");
            eb.HasKey(oi => oi.Id);
            eb.Property(p => p.UnitPrice).HasPrecision(15, 2);
            eb.HasKey(p => p.Id);
        });
    }
}
