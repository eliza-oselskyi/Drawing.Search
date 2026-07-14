using System.Collections.Generic;
using Drawing.Search.Domain.Search;

namespace Drawing.Search.Domain.Effects;

public abstract record SearchEffect
{
    private SearchEffect() { }

    public sealed record LogInformation(string Message) : SearchEffect;

    public sealed record LogError(string Message) : SearchEffect;
    
    public sealed record SetStatus(string Message) : SearchEffect;

    public sealed record SelectTargets(IReadOnlyList<SelectionTarget> Targets) : SearchEffect;

    public sealed record RefreshDrawingCache(string DrawingId) : SearchEffect;
    
    public sealed record InvalidateDrawingCache(string DrawingId) : SearchEffect;
}