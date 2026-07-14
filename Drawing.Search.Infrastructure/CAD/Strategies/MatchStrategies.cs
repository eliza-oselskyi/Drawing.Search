using System;
using System.Text.RegularExpressions;
using Drawing.Search.Domain.Interfaces;

namespace Drawing.Search.Infrastructure.CAD.Strategies;

public class ExactMatchStrategy : ISearchStrategy
{
    public bool Match(string obj, ISearchQuery query) => SearchPredicates.Exact(obj, query);
}

public class ContainsMatchStrategy : ISearchStrategy
{
    public bool Match(string obj, ISearchQuery query) => SearchPredicates.Contains(obj, query);
}

/// <summary>
///     Match strategy, using regular expressions.
/// </summary>
/// <typeparam name="T">Type.</typeparam>
public class RegexMatchStrategy<T> : ISearchStrategy
{
    /// <summary>
    ///     Matches some object <c>T</c> to a <c>SearchQuery</c> query.
    /// </summary>
    /// <param name="obj">Searchable object.</param>
    /// <param name="query">Search query. <c>SearchQuery</c> object instance.</param>
    /// <returns>True on a successful match.</returns>
    public bool Match(string obj, ISearchQuery query)
    {
        try
        {
            return SearchPredicates.Regex(obj, query);
        }
        catch (ArgumentException e)
        {
            if (TestModeServiceLocator.Current.IsTestMode)
            {
                SearchLoggerServiceLocator.Current.LogError(e,"Invalid regex");
            }
            return false;
        }
    }
}

public class WildcardMatchStrategy<T> : ISearchStrategy
{
    public bool Match(string obj, ISearchQuery query) => SearchPredicates.Wildcard(obj, query);
}