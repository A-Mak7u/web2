using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Monolith.Domain;

public class Product
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    [Required]
    public string Name { get; set; } = string.Empty;
    [Column(TypeName = "numeric(18,2)")]
    public decimal Price { get; set; }
    public Guid CategoryId { get; set; }
    public Category? Category { get; set; }
}

public class Category
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    [Required]
    public string Name { get; set; } = string.Empty;
    public List<Product> Products { get; set; } = new();
}

public class Customer
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    [Required]
    public string Email { get; set; } = string.Empty;
}

public class Order
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CustomerId { get; set; }
    public Customer? Customer { get; set; }
    [Column(TypeName = "numeric(18,2)")]
    public decimal Total { get; set; }
    public string Status { get; set; } = "Pending";
    public List<OrderItem> Items { get; set; } = new();
}

public class OrderItem
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
    public int Quantity { get; set; }
    [Column(TypeName = "numeric(18,2)")]
    public decimal UnitPrice { get; set; }
    public Guid OrderId { get; set; }
    public Order? Order { get; set; }
}

public class Cart
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CustomerId { get; set; }
    public List<CartItem> Items { get; set; } = new();
}

public class CartItem
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public Guid CartId { get; set; }
    public Cart? Cart { get; set; }
}

public class Payment
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public string Provider { get; set; } = "Demo";
    public bool Success { get; set; }
}

public class InventoryItem
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProductId { get; set; }
    public int QuantityAvailable { get; set; }
}




