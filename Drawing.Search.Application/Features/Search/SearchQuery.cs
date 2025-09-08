using System;
using System.Text.RegularExpressions;
using Drawing.Search.Domain.Interfaces;

namespace Drawing.Search.Application.Features.Search;

/// <summary>
///     Encapsulates a search query
/// </summary>
public class SearchQuery : ISearchQuery
{
    /// <summary>
    ///     Encapsulates a search query
    /// </summary>
    /// <param name="term">The query itself.</param>
    /// <param name="caseSensitive">Case sensitivity. False by default. </param>
    public SearchQuery(string term, bool caseSensitive = false)
    {
        Term = term;
        CaseSensitive = caseSensitive == true ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        CompiledRegex = new Regex(Term,
                RegexOptions.Compiled | (CaseSensitive == StringComparison.OrdinalIgnoreCase
                    ? RegexOptions.IgnoreCase
                    : RegexOptions.None));
    }

    public string Term { get; set; }

    public StringComparison CaseSensitive { get; set; }

    /// <summary>
    ///     Whether the search type should use wildcards. Default is false (default to regex).
    /// </summary>
    public bool Wildcard { get; set; } = false;

    public Regex? CompiledRegex { get; set; }
}