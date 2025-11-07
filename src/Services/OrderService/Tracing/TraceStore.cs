using System.Collections.Concurrent;

namespace OrderService.Api.Tracing;

public record TraceEvent(DateTime TimestampUtc, string Service, string Message);

// простое in-memory хранилище событий для трейсинга
public class TraceStore
{
    private readonly ConcurrentDictionary<string, List<TraceEvent>> _events = new();
    private readonly string _service = "order";
    private readonly List<TraceEvent> _global = new();

    public void Add(string? traceId, string message)
    {
        var evt = new TraceEvent(DateTime.UtcNow, _service, message);
        if (!string.IsNullOrWhiteSpace(traceId))
        {
            var list = _events.GetOrAdd(traceId, _ => new List<TraceEvent>());
            lock (list) list.Add(evt);
        }
        lock (_global) _global.Add(evt);
    }

    public IReadOnlyList<TraceEvent> Get(string traceId)
        => _events.TryGetValue(traceId, out var list) ? list.OrderBy(e => e.TimestampUtc).ToList() : new List<TraceEvent>();

    public IReadOnlyList<TraceEvent> GetRecent(int take = 200)
        => _global.OrderByDescending(e => e.TimestampUtc).Take(take).Reverse().ToList();
}


