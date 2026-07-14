using System;
using System.Collections.Generic;
using Drawing.Search.Domain.Enums;

namespace Drawing.Search.Application.Features.Search;

/// <summary>
/// Represents the result of a search operation, including details such as the
/// number of matches, duration of the search, and the type of search performed.
/// </summary>
public sealed record SearchResult
{
    /// <summary>
    /// Gets an empty search result.
    /// </summary>
    public static SearchResult Empty { get; } = new SearchResult();
    
    /// <summary>
    /// Gets the total number of matches found during the search.
    /// </summary>
    public int MatchCount { get; init; }

    /// <summary>
    /// Gets the total time, in milliseconds, that the search operation took to complete.
    /// </summary>
    public TimeSpan ElapsedTime { get; init; }

    /// <summary>
    /// Gets the type of the search that was performed.
    /// </summary>
    public SearchType SearchType { get; init; }
    
    /// <summary>
    /// Gets the searchable content that matched.
    /// </summary>
    public IReadOnlyList<string> MatchedContent { get; init; } = new List<string>();
}