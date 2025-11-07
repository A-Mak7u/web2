using Microsoft.AspNetCore.Mvc;
using Monolith.Application;
using Monolith.Domain;

namespace Monolith.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _service;
    public OrdersController(IOrderService service) => _service = service;

    [HttpGet]
    public async Task<IEnumerable<Order>> Get(CancellationToken ct) => await _service.GetAllAsync(ct);

    public record CreateOrderDto(Guid CustomerId, List<CreateOrderItem> Items);
    public record CreateOrderItem(Guid ProductId, int Quantity);

    [HttpPost]
    // создаёт заказ по списку товаров без асинхронной шины
    public async Task<ActionResult<Order>> Create(CreateOrderDto dto, CancellationToken ct)
    {
        var items = dto.Items.Select(i => (i.ProductId, i.Quantity)).ToList();
        var created = await _service.CreateAsync(dto.CustomerId, items, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }
}



