using System;
using System.Text.RegularExpressions;
using Drawing.Search.Domain.Interfaces;

namespace Drawing.Search.Application.Features.Search;

/// <summary>
///     Encapsulates a search query
/// </summary>
public sealed class SearchQuery : ISearchQuery
{
    /// <summary>
    ///     Encapsulates a search query
    /// </summary>
    /// <param name="term">The query itself.</param>
    /// <param name="caseSensitive">Case sensitivity. False by default. </param>
    /// <param name="wildcard">Whether wildcard matching is enabled.</param>
    public SearchQuery(string term, bool caseSensitive = false, bool wildcard = false)
    {
        Term = term ?? string.Empty;
        CaseSensitive = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        CompiledRegex = new Regex(Term,
                RegexOptions.Compiled | (CaseSensitive == StringComparison.OrdinalIgnoreCase
                    ? RegexOptions.IgnoreCase
                    : RegexOptions.None));
    }

    public string Term { get; }

    public StringComparison CaseSensitive { get; }

    /// <summary>
    /// Whether the search type should use wildcards. Default is false (default to regex).
    /// </summary>
    public bool Wildcard { get; } = false;

    public Regex? CompiledRegex { get; }
}