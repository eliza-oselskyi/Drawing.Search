using System;
using System.Text.RegularExpressions;
using Drawing.Search.Domain.Interfaces;

namespace Drawing.Search.Application.Features.Search;

/// <summary>
/// Describes a search query
/// </summary>
public sealed record SearchQuery
{
    public SearchQuery(string term, bool caseSensitive = false, bool wildcard = false)
    {
        Term = term ?? string.Empty;
        CaseSensitive = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        Wildcard = wildcard;
        var regexOptions = RegexOptions.Compiled | (CaseSensitive == StringComparison.OrdinalIgnoreCase
            ? RegexOptions.IgnoreCase
            : RegexOptions.None);
        
        CompiledRegex = new Regex(Term,regexOptions);
        WildcardRegex = new Regex(ToWildCardRegex(Term), regexOptions);
    }

    /// <summary>
    /// Gets the search term.
    /// </summary>
    public string Term { get; }

    /// <summary>
    /// Gets the <see cref="StringComparison" /> value based on whether the search is case-sensitive.
    /// </summary>
    public StringComparison CaseSensitive { get; }

    /// <summary>
    /// Whether the search type should use wildcards. Default is false (default to regex).
    /// </summary>
    public bool Wildcard { get; } 

    /// <summary>
    /// Gets the compiled regular expression.
    /// </summary>
    public Regex CompiledRegex { get; }
    
    /// <summary>
    /// Gets the compiled regular expression for wildcards.
    /// </summary>
    public Regex WildcardRegex { get; }
    
    private static string ToWildCardRegex(string wildcard)
    {
        return "^" + Regex.Escape(wildcard)
            .Replace("\\?", ".")
            .Replace("\\*", ".*") + "$";
    }
}