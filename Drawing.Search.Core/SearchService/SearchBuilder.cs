using Drawing.Search.Core.Interfaces;

namespace Drawing.Search.Core.SearchService;

public class SearchBuilder
{
    private readonly SearchConfiguration _config = new();

    public SearchBuilder WithQuery(string query)
    {
        _config.SearchTerm = query;
        return this;
    }

    public SearchBuilder WithCaseSensitivity(bool caseSensitive)
    {
        _config.CaseSensitive = caseSensitive;
        return this;
    }

    public SearchBuilder WithWildcard(bool wildcard)
    {
        _config.Wildcard = wildcard;
        return this;
    }

    public SearchBuilder WithSearchType(SearchType type)
    {
        _config.Type = type;
        return this;
    }

    public SearchBuilder AddStrategy(ISearchStrategy strategy)
    {
        _config.SearchStrategies.Add(strategy);
        return this;
    }

    public SearchConfiguration Build()
    {
        return _config;
    }
}