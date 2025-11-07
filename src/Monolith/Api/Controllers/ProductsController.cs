using Microsoft.AspNetCore.Mvc;
using Monolith.Application;
using Monolith.Domain;

namespace Monolith.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _service;
    public ProductsController(IProductService service) => _service = service;

    [HttpGet]
    public async Task<IEnumerable<Product>> Get(CancellationToken ct) => await _service.GetAllAsync(ct);

    [HttpPost]
    // добавление нового продукта и возврат 201 Created
    public async Task<ActionResult<Product>> Create(Product product, CancellationToken ct)
    {
        var created = await _service.CreateAsync(product, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }
}



