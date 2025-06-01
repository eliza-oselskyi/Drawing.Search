using System;
using System.Text.RegularExpressions;
using Drawing.Search.Common.Interfaces;

namespace Drawing.Search.CADIntegration;

public class ExactMatchStrategy : ISearchStrategy
{
    public bool Match(string obj, ISearchQuery query)
    {
        var res = obj.ToString().Equals(query.Term, StringComparison.OrdinalIgnoreCase);
        return res;
    }
}

public class ContainsMatchStrategy : ISearchStrategy
{
    public bool Match(string obj, ISearchQuery query)
    {
        string s;
        if (query.CaseSensitive == StringComparison.OrdinalIgnoreCase)
        {
            s = obj.ToString().ToLower();
            return s.Contains(query.Term.ToLower());
        }
        else
        {
            s = obj.ToString();
            return s.Contains(query.Term);
        }
    }
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
        var s = obj.ToString();

        return Regex.IsMatch(s,
            query.Term,
            query.CaseSensitive == StringComparison.OrdinalIgnoreCase
                ? RegexOptions.IgnoreCase
                : RegexOptions.None);
    }
}

public class WildcardMatchStrategy<T> : ISearchStrategy
{
    public bool Match(string obj, ISearchQuery query)
    {
        var s = obj.ToString();
        var reg = WildcardToRegex(query.Term);
        return Regex.IsMatch(s,
            reg,
            query.CaseSensitive == StringComparison.OrdinalIgnoreCase
                ? RegexOptions.IgnoreCase
                : RegexOptions.None);
    }

    private static string WildcardToRegex(string wildcard)
    {
        return "^" + Regex.Escape(wildcard).Replace("\\?", ".").Replace("\\*", ".*") + "$";
    }
}