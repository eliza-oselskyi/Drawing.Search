using System;
using System.Text.RegularExpressions;
using Drawing.Search.Core.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Tekla.Structures.Drawing;

namespace Drawing.Search.Core;

public class ExactMatchStrategy<T>(double cacheExpiration = 30) : ISearchStrategy<T>
{
    public bool Match(T obj, SearchQuery query)
    {
        var res = obj != null && obj.ToString().Equals(query.Term, StringComparison.OrdinalIgnoreCase);
        return res;
    }

    public bool Match(string str, SearchQuery query)
    {
        throw new NotImplementedException();
    }
}

public class ContainsMatchStrategy<T> : ISearchStrategy<T>
{
    public bool Match(T obj, SearchQuery query)
    {
        var s = string.Empty;
        if (query.CaseSensitive == StringComparison.OrdinalIgnoreCase)
        {
            if (obj != null) s = obj.ToString().ToLower();
            return s.Contains(query.Term.ToLower());
        }
        else
        {

            if (obj != null) s = obj.ToString();
            return s.Contains(query.Term);
        }
    }

    public bool Match(string str, SearchQuery query)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Match strategy, using regular expressions.
/// </summary>
/// <typeparam name="T">Type.</typeparam>
public class RegexMatchStrategy<T> : ISearchStrategy<T>
{
    /// <summary>
    /// Matches some object <c>T</c> to a <c>SearchQuery</c> query.
    /// </summary>
    /// <param name="obj">Searchable object.</param>
    /// <param name="query">Search query. <c>SearchQuery</c> object instance.</param>
    /// <returns>True on successful match.</returns>
    public bool Match(T obj, SearchQuery query)
    {
        var s = obj?.ToString();

        return s != null &&
               Regex.IsMatch(s,
                   query.Term,
                   query.CaseSensitive == StringComparison.OrdinalIgnoreCase
                       ? RegexOptions.IgnoreCase
                       : RegexOptions.None);
    }

    // TODO: Remove this overload and find a way to do it generically like the method above.
    public bool Match(string str, SearchQuery query)
    {
        var s = str?.ToString();

        return s != null &&
               Regex.IsMatch(s,
                   query.Term,
                   query.CaseSensitive == StringComparison.OrdinalIgnoreCase
                       ? RegexOptions.IgnoreCase
                       : RegexOptions.None);
        
    }
}
