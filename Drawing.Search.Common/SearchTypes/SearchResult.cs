using Drawing.Search.Common.Enums;

namespace Drawing.Search.Common.SearchTypes;

/// <summary>
///     Represents the result of a search operation, including details such as the
///     number of matches, duration of the search, and the type of search performed.
/// </summary>
public class SearchResult
{
    /// <summary>
    ///     Gets or sets the total number of matches found during the search.
    /// </summary>
    public int MatchCount { get; set; }

    /// <summary>
    ///     Gets or sets the total time, in milliseconds, that the search operation took to complete.
    /// </summary>
    public long ElapsedMilliseconds { get; set; }

    /// <summary>
    ///     Gets or sets the type of the search that was performed.
    /// </summary>
    /// <example>
    ///     The <see cref="SearchType" /> property can have values like:
    ///     <code>
    /// SearchType.Text
    /// SearchType.PartMark
    /// SearchType.Assembly
    /// </code>
    /// </example>
    public SearchType SearchType { get; set; }
}