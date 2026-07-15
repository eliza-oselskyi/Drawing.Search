namespace Drawing.Search.Domain.Search;

/// <summary>
/// Describes what kind of search requests can be made for a drawing.
/// </summary>
public abstract record SearchRequest(
    string Term,
    bool UseRegex,
    bool CaseSensitive)
{
    private SearchRequest() : this(string.Empty, false, false) { }
    
    /// <summary>
    /// Describes a text search request.
    /// </summary>
    public sealed record Text(
        string Term,
        bool UseRegex,
        bool CaseSensitive) : SearchRequest(Term, UseRegex, CaseSensitive);
    
    /// <summary>
    /// Describes a part mark search request.
    /// </summary>
    public sealed record PartMark(
        string Term,
        bool UseRegex,
        bool CaseSensitive) : SearchRequest(Term, UseRegex, CaseSensitive);
    
    /// <summary>
    /// Describes an assembly search request.
    /// </summary>
    public sealed record Assembly(
        string Term,
        bool UseRegex,
        bool CaseSensitive,
        bool IncludeAllParts) : SearchRequest(Term, UseRegex, CaseSensitive);
}