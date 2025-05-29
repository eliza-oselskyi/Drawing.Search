using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

    public override string ToString()
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine($"Search Term: {SearchTerm}");
        stringBuilder.AppendLine($"Case Sensitive: {CaseSensitive}");
        stringBuilder.AppendLine($"Wildcard: {Wildcard}");
        var stratList = SearchStrategies.Select(m => m.GetType().Name).ToList();
        var stratString = string.Join(", ", stratList);
        stringBuilder.AppendLine($"Search Strategies: {stratString}");
        stringBuilder.AppendLine($"Search Type: {Type}");
        return stringBuilder.ToString();
    }
}