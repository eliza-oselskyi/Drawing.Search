namespace Drawing.Search.Domain.Drawings;

/// <summary>
/// Describes a searchable object in a drawing.
/// </summary>
public abstract record SearchableDrawingObject(string Id, DrawingId DrawingId, string SearchText)
{
    private SearchableDrawingObject() : this(string.Empty, new DrawingId(string.Empty), string.Empty) { }
    
    /// <summary>
    /// Describes a text object in a drawing.
    /// </summary>
    public sealed record TextObject(string Id,
        DrawingId DrawingId,
        string SearchText) : SearchableDrawingObject(Id, DrawingId, SearchText);
    
    /// <summary>
    /// Describes a part mark object in a drawing.
    /// </summary>
    public sealed record PartMarkObject(string Id,
        DrawingId DrawingId,
        string SearchText) : SearchableDrawingObject(Id, DrawingId, SearchText);
    
    /// <summary>
    /// Describes an assembly object in a drawing.
    /// </summary>
    public sealed record AssemblyObject(string Id,
        DrawingId DrawingId,
        string SearchText,
        string AssemblyPosition,
        bool IsMainPart) : SearchableDrawingObject(Id, DrawingId, SearchText);
}