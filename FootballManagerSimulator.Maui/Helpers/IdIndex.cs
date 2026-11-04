namespace FootballManagerSimulator.Maui.Helpers;

/// <summary>
/// Finds items in a list by id without scanning it every time. The list stays the source of truth:
/// a remembered position is only trusted while the item there still has that id, so adding, removing
/// or replacing items (or the whole list, as loading a save does) just rebuilds the index on the next lookup.
/// </summary>
internal sealed class IdIndex<T>(Func<T, int> idOf) where T : class
{
    private sealed record Snapshot(List<T> List, Dictionary<int, int> Positions);

    // Swapped whole, never changed in place, so lookups from the UI thread during a background
    // simulation always see a complete index.
    private Snapshot? _snapshot;

    public T? Find(List<T> list, int id)
    {
        var snapshot = _snapshot;
        if (snapshot != null && ReferenceEquals(snapshot.List, list))
        {
            if (snapshot.Positions.TryGetValue(id, out var position)
                && position < list.Count && idOf(list[position]) == id)
                return list[position];

            // An unknown id that isn't in the list either (a released player, say) needs no rebuild.
            if (!snapshot.Positions.ContainsKey(id) && list.Count == snapshot.Positions.Count
                && !list.Exists(p => idOf(p) == id))
                return null;
        }

        var rebuilt = new Dictionary<int, int>(list.Count);
        for (var i = 0; i < list.Count; i++)
            rebuilt.TryAdd(idOf(list[i]), i);
        _snapshot = new Snapshot(list, rebuilt);

        return rebuilt.TryGetValue(id, out var found) ? list[found] : null;
    }
}
