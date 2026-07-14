using System;
using Drawing.Search.Domain.Interfaces;
using Drawing.Search.Infrastructure.CAD.Extractors;
using Tekla.Structures.Drawing;

namespace Drawing.Search.Application.Features.Search;

public abstract class SearchStrategyFactory
{
    
    private static IDataExtractor GetExtractor<T>()
    {
        return typeof(T) switch
        {
            { } t when t == typeof(Mark) => new MarkExtractor(),
            { } t when t == typeof(Tekla.Structures.Drawing.Text) => new TextExtractor(),
            { } t when t == typeof(ModelObject) => new ModelObjectExtractor(),
            { } t when t == typeof(string) => new StringExtractor(),
            _ => throw new ArgumentException($"No extractor available for type {typeof(T)}")
        };
    }

    public static ObservableSearch<T> CreateSearcher<T>(SearchConfiguration config)
    {
        var extractor = GetExtractor<T>();
        return new ObservableSearch<T>(config.SearchStrategies, extractor);
    }

    /// <summary>
    /// Creates a search query from the configuration.
    /// </summary>
    public static ISearchQuery CreateSearchQuery(SearchConfiguration config) => 
        new SearchQuery(config.SearchTerm ?? string.Empty, config.CaseSensitive, config.Wildcard);
}