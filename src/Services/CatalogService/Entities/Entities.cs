using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CatalogService.Api.Entities;

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
    [JsonIgnore]
    public List<Product> Products { get; set; } = new();
}


