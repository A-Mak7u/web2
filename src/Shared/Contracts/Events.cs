namespace Contracts;

// доменные события: создание заказа и результат оплаты

public interface OrderCreated
{
    Guid OrderId { get; }
    Guid CustomerId { get; }
    decimal Total { get; }
}

public interface PaymentCompleted
{
    Guid OrderId { get; }
    bool Success { get; }
}



