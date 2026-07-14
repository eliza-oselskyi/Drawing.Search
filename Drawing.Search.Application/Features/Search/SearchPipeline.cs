using System;
using System.Collections.Generic;
using System.Linq;
using Drawing.Search.Domain.Interfaces;

namespace Drawing.Search.Application.Features.Search;

public static class SearchPipeline
{
    public static IReadOnlyList<SearchMatch<T>> Search<T>(
        IEnumerable<T> items,
        Func<T, string> extract,
        ISearchQuery query,
        SearchPredicate predicate)
    {
        if (items is null) throw new ArgumentNullException(nameof(items));
        if (extract is null) throw new ArgumentNullException(nameof(extract));
        if (query is null) throw new ArgumentNullException(nameof(query));
        if (predicate is null) throw new ArgumentNullException(nameof(predicate));

        return items
            .Where(item => item is not null)
            .Select(item => new SearchMatch<T>(item, extract(item) ?? string.Empty))
            .Where(match => predicate(match.Content, query))
            .ToList();
    }
}