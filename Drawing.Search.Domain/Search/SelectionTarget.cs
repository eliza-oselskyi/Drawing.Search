using Drawing.Search.Domain.Drawings;

namespace Drawing.Search.Domain.Search;

/// <summary>
/// Describes a selection target in a drawing.
/// </summary>
public abstract record SelectionTarget
{
    private SelectionTarget() { }

    /// <summary>
    /// Describes selecting a drawing object.
    /// </summary>
    public sealed record DrawingObject(DrawingId DrawingId, string ObjectId) : SelectionTarget;
    
    /// <summary>
    /// Describes selecting an assembly position.
    /// </summary>
    public sealed record AssemblyPosition(DrawingId DrawingId, string Position, bool IncludeAllParts) : SelectionTarget;
}