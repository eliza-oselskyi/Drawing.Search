using System;

namespace Drawing.Search.Core;

public class SearchQuery(string term, bool caseSensitive = false)
{
    public string Term { get; set; } = term;
    public StringComparison CaseSensitive { get; set; } = (caseSensitive == true) ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
    public bool Wildcard { get; set; } = false;
}