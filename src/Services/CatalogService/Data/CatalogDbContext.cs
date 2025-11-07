using CatalogService.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace CatalogService.Api.Data;

// контекст EF Core для каталога (товары и категории)
public class CatalogDbContext : DbContext
{
    public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
}




