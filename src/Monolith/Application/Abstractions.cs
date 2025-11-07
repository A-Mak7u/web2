using Monolith.Domain;

namespace Monolith.Application;

public interface IProductService
{
    Task<List<Product>> GetAllAsync(CancellationToken ct = default);
    Task<Product> CreateAsync(Product product, CancellationToken ct = default);
}

public interface IOrderService
{
    Task<List<Order>> GetAllAsync(CancellationToken ct = default);
    Task<Order> CreateAsync(Guid customerId, List<(Guid productId, int quantity)> items, CancellationToken ct = default);
}




