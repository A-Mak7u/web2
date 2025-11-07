using Microsoft.EntityFrameworkCore;
using Monolith.Domain;

namespace Monolith.Infrastructure;

// единый контекст EF Core для монолита (продукты, заказы и др.)
public class MonolithDbContext : DbContext
{
    public MonolithDbContext(DbContextOptions<MonolithDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
}




