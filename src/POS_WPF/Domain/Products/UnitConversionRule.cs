namespace POS_WPF.Domain.Products;

public sealed class UnitConversionRule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProductId { get; set; }
    public Guid FromUnitId { get; set; }
    public Guid ToUnitId { get; set; }
    public decimal Factor { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class UnitConversionGraph
{
    public decimal ResolveToBase(Guid baseUnitId, Guid fromUnitId, IReadOnlyCollection<UnitConversionRule> rules, IReadOnlyCollection<ProductUnit> units)
    {
        if (fromUnitId == baseUnitId) return 1m;
        var adjacency = rules.Where(x => x.IsActive).GroupBy(x => x.FromUnitId).ToDictionary(x => x.Key, x => x.ToList());
        var queue = new Queue<(Guid Unit, decimal Factor)>();
        var visited = new HashSet<Guid> { fromUnitId };
        queue.Enqueue((fromUnitId, 1m));
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!adjacency.TryGetValue(current.Unit, out var edges)) continue;
            foreach (var edge in edges)
            {
                if (edge.Factor <= 0) throw new InvalidOperationException("Conversion factors must be greater than zero.");
                var factor = current.Factor * edge.Factor;
                if (edge.ToUnitId == baseUnitId) return factor;
                if (visited.Add(edge.ToUnitId)) queue.Enqueue((edge.ToUnitId, factor));
            }
        }
        var direct = units.SingleOrDefault(x => x.Id == fromUnitId);
        if (direct is not null && direct.ConversionFactorToBase > 0) return direct.ConversionFactorToBase;
        throw new InvalidOperationException("No valid conversion path to the base unit exists.");
    }

    public void ValidateNoCycles(IReadOnlyCollection<UnitConversionRule> rules)
    {
        var graph = rules.Where(x => x.IsActive).GroupBy(x => x.FromUnitId).ToDictionary(x => x.Key, x => x.Select(r => r.ToUnitId).ToList());
        foreach (var node in graph.Keys) if (HasCycle(node, graph, new HashSet<Guid>(), new HashSet<Guid>())) throw new InvalidOperationException("Circular unit conversion is not allowed.");
    }

    private static bool HasCycle(Guid node, Dictionary<Guid, List<Guid>> graph, HashSet<Guid> visiting, HashSet<Guid> visited)
    {
        if (visiting.Contains(node)) return true;
        if (visited.Contains(node)) return false;
        visiting.Add(node);
        if (graph.TryGetValue(node, out var children)) foreach (var child in children) if (HasCycle(child, graph, visiting, visited)) return true;
        visiting.Remove(node); visited.Add(node); return false;
    }
}
