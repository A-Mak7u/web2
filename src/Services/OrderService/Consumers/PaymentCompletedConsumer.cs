using Contracts;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using OrderService.Api.Data;
using OrderService.Api.Tracing;

namespace OrderService.Api.Consumers;

public class PaymentCompletedConsumer : IConsumer<PaymentCompleted>
{
    private readonly OrderDbContext _db;
    private readonly TraceStore _trace;

    public PaymentCompletedConsumer(OrderDbContext db, TraceStore trace)
    {
        _db = db;
        _trace = trace;
    }

    public async Task Consume(ConsumeContext<PaymentCompleted> context)
    {
        var traceId = context.Headers.Get<string>("X-Trace-Id")?.ToString();
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == context.Message.OrderId);
        if (order == null) return;
        order.Status = context.Message.Success ? "Paid" : "Failed";
        await _db.SaveChangesAsync();
        _trace.Add(traceId, $"Order:PaymentCompleted consumed; order {order.Id} -> {order.Status}");
    }
}


