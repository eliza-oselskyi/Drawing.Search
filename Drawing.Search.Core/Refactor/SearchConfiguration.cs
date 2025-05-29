using System;
using System.Collections.Generic;
using Drawing.Search.Core.Interfaces;

namespace Drawing.Search.Core;

public class SearchConfiguration
{
    public string SearchTerm { get; set; }
    public bool CaseSensitive { get; set; }
    public bool Wildcard { get; set; }
    public List<ISearchStrategy> SearchStrategies { get; set; } = new();
    public SearchType Type { get; set; }
    public StringComparison StringComparison =>
        CaseSensitive
            ? StringComparison.Ordinal
            : StringComparison.OrdinalIgnoreCase;
}