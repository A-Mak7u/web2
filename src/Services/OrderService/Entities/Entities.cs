using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace OrderService.Api.Entities;

public class Order
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CustomerId { get; set; }
    [Column(TypeName = "numeric(18,2)")]
    public decimal Total { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Paid, Failed
    public List<OrderItem> Items { get; set; } = new();
}

public class OrderItem
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    [Column(TypeName = "numeric(18,2)")]
    public decimal UnitPrice { get; set; }
    public Guid OrderId { get; set; }
    [JsonIgnore]
    public Order? Order { get; set; }
}


