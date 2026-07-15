using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Drawing.Search.Application.Features.Search;
using Drawing.Search.Application.Services.Interfaces;
using Drawing.Search.Domain.Enums;
using Drawing.Search.Domain.Interfaces;
using Drawing.Search.Infrastructure.CAD.Models;
using Drawing.Search.Infrastructure.CAD.Search;

namespace Drawing.Search.Infrastructure.Services;

public class DrawingSearchService : ISearchService, IDisposable
{
    private readonly IDrawingProvider _drawingProvider;
    private readonly ISearchLogger _logger;
    private readonly Dictionary<SearchType, ISearchExecutor> _searchExecutors;

    public DrawingSearchService(
        IDrawingProvider drawingProvider,
        ISearchLogger logger,
        IAssemblyCache assemblyCache,
        IDrawingCache drawingCache,
        ICacheKeyGenerator cacheKeyGenerator)
    {
        _drawingProvider = drawingProvider;
        _logger = logger;

        var resultSelector = new DrawingResultSelector(drawingProvider);
        _searchExecutors = InitializeSearchExecutors(assemblyCache, drawingCache, resultSelector, cacheKeyGenerator);
    }

    public Task<SearchResult> ExecuteSearchAsync(SearchConfiguration config)
    {
        if (config == null) throw new ArgumentNullException(nameof(config));

        var drawing = _drawingProvider.GetActiveDrawing();
        if (drawing == null)
            throw new InvalidOperationException("No active drawing found.");

        _logger.LogInformation($"Executing search with configuration: {config}");

        try
        {
            if (!_searchExecutors.TryGetValue(config.Type, out var executor))
                throw new ArgumentException($"Unsupported search type: {config.Type}");

            return executor.ExecuteAsync(config, drawing);

        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error executing search.");
            throw;
        }
    }

    private static Dictionary<SearchType, ISearchExecutor> InitializeSearchExecutors(
        IAssemblyCache assemblyCache,
        IDrawingCache drawingCache,
        DrawingResultSelector resultSelector,
        ICacheKeyGenerator cacheKeyGenerator)
    {
        return new Dictionary<SearchType, ISearchExecutor>
        {
            { SearchType.PartMark, new PartMarkSearchExecutor(resultSelector, drawingCache, cacheKeyGenerator, assemblyCache) },
            { SearchType.Text, new TextSearchExecutor(resultSelector, drawingCache, assemblyCache) },
            { SearchType.Assembly, new AssemblySearchExecutor(resultSelector, assemblyCache, drawingCache) }
        };
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}