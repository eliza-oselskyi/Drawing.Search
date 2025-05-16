using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using Weld = Tekla.Structures.Drawing.Weld;

namespace Drawing.Search.Core;

/// <summary>
/// Handled search queries at one time, now some odd, duplicate methods.
/// <remarks>Do not use this class! To be removed in future versions.</remarks>
/// </summary>
/// <param name="query">Search query.</param>
[Obsolete("Use ObservableSearch class instead.")]
public class QueryHandler(string query)
{
    public string Query
    {
        get => _query;
        set => _query = value.ToLower();
    }

    private static readonly MemoryCache Cache = new MemoryCache(new MemoryCacheOptions());
    private string _query = query;

    private static string WildcardToRegex(string wildcard)
    {
        return "^" + Regex.Escape(wildcard).Replace("\\?", ".").Replace("\\*", ".*") + "$";
    }

    private static string ParseUnformattedString(string str)
    {
        return str.Replace("{", "").Replace("}", "").Replace("[", "").Replace("]", "").Replace("\n", "").Trim();
    }

    public static void ClearCache()
    {
        Cache.Clear();
    }
}