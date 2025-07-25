using System;
using Drawing.Search.Common.Interfaces;

namespace Drawing.Search.Common.SearchTypes;

/// <summary>
///     Encapsulates a search query
/// </summary>
/// <param name="term">The query itself.</param>
/// <param name="caseSensitive">Case sensitivity. False by default. </param>
public class SearchQuery(string term, bool caseSensitive = false) : ISearchQuery
{
    public string Term { get; set; } = term;

    public StringComparison CaseSensitive { get; set; } =
        caseSensitive == true ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

    /// <summary>
    ///     Whether the search type should use wildcards. Default is false (default to regex).
    /// </summary>
    public bool Wildcard { get; set; } = false;
}