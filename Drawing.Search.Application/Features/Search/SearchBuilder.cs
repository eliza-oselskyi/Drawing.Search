using Drawing.Search.Domain.Enums;
using Drawing.Search.Domain.Interfaces;

namespace Drawing.Search.Application.Features.Search;

/// <summary>
///     A builder class for creating and configuring instances of <see cref="SearchConfiguration" />.
/// </summary>
/// <remarks>
///     This class helps construct a search configuration in a fluent and readable way by providing methods
///     to customize search parameters, such as the query, case sensitivity, wildcard usage, search type, and strategies.
/// </remarks>
/// <example>
///     Example usage:
///     <code>
/// var searchConfig = new SearchBuilder()
///     .WithQuery("example query")
///     .WithCaseSensitivity(true)
///     .WithWildcard(false)
///     .WithSearchType(SearchType.Text)
///     .AddStrategy(new RegexMatchStrategy&lt;Text&gt;())
///     .Build();
/// </code>
/// </example>
public class SearchBuilder
{
    private readonly SearchConfiguration _config = new();

    /// <summary>
    ///     Sets the query string to be used for the search.
    /// </summary>
    /// <param name="query">The query string to search for.</param>
    /// <returns>The current instance of <see cref="SearchBuilder" /> for fluent configuration.</returns>
    public SearchBuilder WithQuery(string query)
    {
        _config.SearchTerm = query;
        return this;
    }

    /// <summary>
    ///     Sets whether the search should be case-sensitive.
    /// </summary>
    /// <param name="caseSensitive">
    ///     A boolean indicating if the search should be case-sensitive (<c>true</c>) or case-insensitive (<c>false</c>).
    /// </param>
    /// <returns>The current instance of <see cref="SearchBuilder" /> for fluent configuration.</returns>
    public SearchBuilder WithCaseSensitivity(bool caseSensitive)
    {
        _config.CaseSensitive = caseSensitive;
        return this;
    }

    /// <summary>
    ///     Sets whether wildcard characters should be supported in the search query.
    /// </summary>
    /// <param name="wildcard">A boolean indicating if wildcard support should be enabled.</param>
    /// <returns>The current instance of <see cref="SearchBuilder" /> for fluent configuration.</returns>
    public SearchBuilder WithWildcard(bool wildcard)
    {
        _config.Wildcard = wildcard;
        return this;
    }

    /// <summary>
    ///     Specifies the type of search to perform.
    /// </summary>
    /// <param name="type">The <see cref="SearchType" /> indicating the type of search.</param>
    /// <returns>The current instance of <see cref="SearchBuilder" /> for fluent configuration.</returns>
    public SearchBuilder WithSearchType(SearchType type)
    {
        _config.Type = type;
        return this;
    }

    /// <summary>
    ///     Adds a search strategy to the search configuration.
    /// </summary>
    /// <param name="strategy">
    ///     An implementation of <see cref="ISearchStrategy" /> that defines a strategy for performing the search.
    /// </param>
    /// <returns>The current instance of <see cref="SearchBuilder" /> for fluent configuration.</returns>
    public SearchBuilder AddStrategy(ISearchStrategy strategy)
    {
        _config.SearchStrategies.Add(strategy);
        return this;
    }

    /// <summary>
    ///     Builds and returns the configured <see cref="SearchConfiguration" /> instance.
    /// </summary>
    /// <returns>A fully configured <see cref="SearchConfiguration" /> object.</returns>
    public SearchConfiguration Build()
    {
        return _config;
    }
}