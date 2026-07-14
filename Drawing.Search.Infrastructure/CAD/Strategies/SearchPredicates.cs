using System;
using System.Text.RegularExpressions;
using Drawing.Search.Domain.Interfaces;

namespace Drawing.Search.Infrastructure.CAD.Strategies;

public static class SearchPredicates
{
    public static bool Exact(string candidate, ISearchQuery query) => 
        string.Equals(candidate, query.Term, query.CaseSensitive);

    public static bool Contains(string candidate, ISearchQuery query) =>
        candidate.IndexOf(query.Term, query.CaseSensitive) >= 0;

    public static bool Regex(string candidate, ISearchQuery query) =>
        query.CompiledRegex.IsMatch(candidate) == true;

    public static bool Wildcard(string candidate, ISearchQuery query) =>
        query.WildcardRegex.IsMatch(candidate);
}