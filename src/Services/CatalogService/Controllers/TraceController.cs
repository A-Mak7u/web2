using CatalogService.Api.Tracing;
using Microsoft.AspNetCore.Mvc;

namespace CatalogService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TraceController : ControllerBase
{
    private readonly TraceStore _store;
    public TraceController(TraceStore store) => _store = store;

    [HttpGet]
    public ActionResult<IEnumerable<TraceEvent>> GetRecent() => Ok(_store.GetRecent());

    [HttpGet("{id}")]
    public ActionResult<IEnumerable<TraceEvent>> Get(string id) => Ok(_store.Get(id));
}


