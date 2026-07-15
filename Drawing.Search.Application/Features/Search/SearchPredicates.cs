using Drawing.Search.Domain.Interfaces;

namespace Drawing.Search.Application.Features.Search;

public static class SearchPredicates
{
    public static bool Exact(string candidate, SearchQuery query) => 
        string.Equals(candidate, query.Term, query.CaseSensitive);

    public static bool Contains(string candidate, SearchQuery query) =>
        candidate.IndexOf(query.Term, query.CaseSensitive) >= 0;

    public static bool Regex(string candidate, SearchQuery query) =>
        query.CompiledRegex.IsMatch(candidate) == true;

    public static bool Wildcard(string candidate, SearchQuery query) =>
        query.WildcardRegex.IsMatch(candidate);
}