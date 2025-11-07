using System.Text.Json;
using System.Text.Json.Serialization;
using CatalogService.Api.Data;
using CatalogService.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using CatalogService.Api.Tracing;
using Microsoft.AspNetCore.Authorization;

namespace CatalogService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly CatalogDbContext _db;
    private readonly IDistributedCache _cache;
    private static readonly string CacheKey = "products_all";
    private readonly TraceStore _trace;

    public ProductsController(CatalogDbContext db, IDistributedCache cache, TraceStore trace)
    {
        _db = db;
        _cache = cache;
        _trace = trace;
    }

    [HttpGet]
    public async Task<IEnumerable<Product>> Get()
    {
        var jsonOptions = new JsonSerializerOptions { ReferenceHandler = ReferenceHandler.IgnoreCycles };
        var traceId = HttpContext.Request.Headers["X-Trace-Id"].ToString();
        _trace.Add(traceId, "Catalog:GetProducts: request accepted");
        string? cached = null;
        try
        {
            // попытаться отдать данные из кэша, чтобы ускорить ответ
            cached = await _cache.GetStringAsync(CacheKey);
            if (!string.IsNullOrEmpty(cached))
            {
                _trace.Add(traceId, "Catalog:GetProducts: cache hit");
                return JsonSerializer.Deserialize<List<Product>>(cached, jsonOptions) ?? new List<Product>();
            }
        }
        catch { }

        var products = await _db.Products.Include(p => p.Category).AsNoTracking().ToListAsync();
        _trace.Add(traceId, "Catalog:GetProducts: cache miss; loaded from DB");
        try
        {
            // прогреваем кэш на несколько минут для последующих запросов
            await _cache.SetStringAsync(CacheKey, JsonSerializer.Serialize(products, jsonOptions), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            });
            _trace.Add(traceId, "Catalog:GetProducts: cached");
        }
        catch { }
        return products;
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<Product>> Create(Product product)
    {
        _db.Products.Add(product);
        await _db.SaveChangesAsync();
        await _cache.RemoveAsync(CacheKey);
        return CreatedAtAction(nameof(Get), new { id = product.Id }, product);
    }
}


