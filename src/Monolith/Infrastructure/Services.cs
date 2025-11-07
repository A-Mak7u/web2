using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Monolith.Application;
using Monolith.Domain;

namespace Monolith.Infrastructure;

public class ProductService : IProductService
{
    private readonly MonolithDbContext _db;
    public ProductService(MonolithDbContext db) => _db = db;

    public async Task<List<Product>> GetAllAsync(CancellationToken ct = default)
        => await _db.Products.Include(p => p.Category).AsNoTracking().ToListAsync(ct);

    public async Task<Product> CreateAsync(Product product, CancellationToken ct = default)
    {
        _db.Products.Add(product);
        await _db.SaveChangesAsync(ct);
        return product;
    }
}

public class OrderService : IOrderService
{
    private readonly MonolithDbContext _db;
    public OrderService(MonolithDbContext db) => _db = db;

    public async Task<List<Order>> GetAllAsync(CancellationToken ct = default)
        => await _db.Orders.Include(o => o.Items).ThenInclude(i => i.Product).AsNoTracking().ToListAsync(ct);

    public async Task<Order> CreateAsync(Guid customerId, List<(Guid productId, int quantity)> items, CancellationToken ct = default)
    {
        var products = await _db.Products.Where(p => items.Select(i => i.productId).Contains(p.Id)).ToListAsync(ct);
        var order = new Order { CustomerId = customerId };
        foreach (var i in items)
        {
            var product = products.First(p => p.Id == i.productId);
            order.Items.Add(new OrderItem
            {
                ProductId = product.Id,
                Quantity = i.quantity,
                UnitPrice = product.Price
            });
        }
        order.Total = order.Items.Sum(i => i.UnitPrice * i.Quantity);
        _db.Orders.Add(order);
        await _db.SaveChangesAsync(ct);
        return order;
    }
}

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<MonolithDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration["Redis:Configuration"];
            options.InstanceName = configuration["Redis:InstanceName"];
        });

        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IOrderService, OrderService>();

        return services;
    }
}


