using System;
using System.Net.Mime;
using Drawing.Search.Common.Interfaces;
using Drawing.Search.Common.SearchTypes;
using Drawing.Search.Searching;
using Tekla.Structures;
using Tekla.Structures.Drawing;

namespace Drawing.Search.CADIntegration;

public class SearchStrategyFactory
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

    public static ISearchQuery CreateSearchQuery(SearchConfiguration config)
    {
        if (config.SearchTerm == null) return new SearchQuery("");
        return new SearchQuery(config.SearchTerm)
        {
            CaseSensitive = config.StringComparison
        };
    }
}