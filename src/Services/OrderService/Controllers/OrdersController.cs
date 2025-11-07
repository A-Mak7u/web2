using Contracts;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderService.Api.Data;
using OrderService.Api.Entities;
using OrderService.Api.Tracing;
using Microsoft.AspNetCore.Authorization;

namespace OrderService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly OrderDbContext _db;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly TraceStore _trace;

    public OrdersController(OrderDbContext db, IPublishEndpoint publishEndpoint, TraceStore trace)
    {
        _db = db;
        _publishEndpoint = publishEndpoint;
        _trace = trace;
    }

    [HttpGet]
    public async Task<IEnumerable<Order>> Get() => await _db.Orders.Include(o => o.Items).AsNoTracking().ToListAsync();

[HttpPost]
[Authorize]
// создаёт заказ и публикует доменное событие OrderCreated
    public async Task<ActionResult<Order>> Create(CreateOrderRequest request)
    {
        var traceId = HttpContext.Request.Headers["X-Trace-Id"].ToString();
        _trace.Add(traceId, "Order:Create: request accepted");
        var order = new Order
        {
            CustomerId = request.CustomerId,
            Items = request.Items.Select(i => new OrderItem
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList(),
        };
        order.Total = order.Items.Sum(i => i.UnitPrice * i.Quantity);

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();
        _trace.Add(traceId, $"Order:Create: saved order {order.Id}");

        // публикуем событие; используем заголовок трассировки, если он передан
        await _publishEndpoint.Publish<OrderCreated>(new
        {
            OrderId = order.Id,
            CustomerId = order.CustomerId,
            Total = order.Total
        }, context =>
        {
            if (!string.IsNullOrWhiteSpace(traceId)) context.Headers.Set("X-Trace-Id", traceId);
        });
        _trace.Add(traceId, "Order:Create: published OrderCreated");

        return CreatedAtAction(nameof(Get), new { id = order.Id }, order);
    }
}

public record CreateOrderRequest(Guid CustomerId, List<CreateOrderItem> Items);
public record CreateOrderItem(Guid ProductId, int Quantity, decimal UnitPrice);


