using Contracts;
using MassTransit;
using Microsoft.Extensions.Logging;
using PaymentService.Api.Tracing;

namespace PaymentService.Api.Consumers;

public class OrderCreatedConsumer : IConsumer<OrderCreated>
{
    private readonly ILogger<OrderCreatedConsumer> _logger;
    private readonly TraceStore _trace;

    public OrderCreatedConsumer(ILogger<OrderCreatedConsumer> logger, TraceStore trace)
    {
        _logger = logger;
        _trace = trace;
    }

    public async Task Consume(ConsumeContext<OrderCreated> context)
    {
        var traceId = context.Headers.Get<string>("X-Trace-Id")?.ToString();
        _trace.Add(traceId, $"Payment:OrderCreated consumed; processing order {context.Message.OrderId}");
        _logger.LogInformation("Processing payment for Order {OrderId}, Total {Total}", context.Message.OrderId, context.Message.Total);

        var success = true; // Simulate payment success
        await context.Publish<PaymentCompleted>(new
        {
            OrderId = context.Message.OrderId,
            Success = success
        }, publishContext =>
        {
            if (!string.IsNullOrWhiteSpace(traceId)) publishContext.Headers.Set("X-Trace-Id", traceId);
        });
        _trace.Add(traceId, "Payment: published PaymentCompleted");
    }
}


