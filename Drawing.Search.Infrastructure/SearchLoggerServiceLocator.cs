using System;
using Drawing.Search.Domain.Interfaces;

namespace Drawing.Search.Infrastructure;

public static class SearchLoggerServiceLocator
{
    private static ISearchLogger? _searchLogger;

    public static void Initialize(ISearchLogger searchLogger)
    {
        _searchLogger = searchLogger;
    }

    public static ISearchLogger Current =>
        _searchLogger ?? throw new InvalidOperationException($"Search logger not initialized.");
}