using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Drawing.Search.Caching.Interfaces;
using Drawing.Search.CADIntegration.Interfaces;
using Drawing.Search.CADIntegration.Strategies;
using Drawing.Search.Common.Enums;
using Drawing.Search.Common.Interfaces;
using Drawing.Search.Common.SearchTypes;

namespace Drawing.Search.CADIntegration;

public class DrawingSearchService : ISearchService, IDisposable
{
    
    private readonly IDrawingProvider _drawingProvider;
    private readonly ISearchLogger _logger;
    private readonly Dictionary<SearchType, ISearchExecutor> _searchExecutors;
    private readonly IDrawingCache _drawingCache;
    private readonly IAssemblyCache _assemblyCache;
    private readonly ICacheKeyGenerator _cacheKeyGenerator;

    public DrawingSearchService(
        IDrawingProvider drawingProvider,
        ISearchLogger logger,
        IAssemblyCache assemblyCache,
        IDrawingCache drawingCache,
        ICacheKeyGenerator cacheKeyGenerator)
    {
        _drawingCache = drawingCache;
        _assemblyCache = assemblyCache;
        _cacheKeyGenerator = cacheKeyGenerator;
        
        _drawingProvider = drawingProvider;
        _logger = logger;
        
        var resultSelector = new DrawingResultSelector(drawingProvider);
        _searchExecutors = InitializeSearchExecutors(assemblyCache, drawingCache, resultSelector, cacheKeyGenerator);
    }
    
    public Task<SearchResult> ExecuteSearchAsync(SearchConfiguration config)
    {
        var drawing = _drawingProvider.GetActiveDrawing();
        if (drawing == null)
            throw new InvalidOperationException("No active drawing found.");
        if (config == null) throw new ArgumentNullException(nameof(config));
        
        _logger.LogInformation($"Executing search with configuration: {config.ToString()}");

        try
        {
            if (!_searchExecutors.TryGetValue(config.Type, out var executor))
                throw new ArgumentException($"Unsupported search type: {config.Type}");
            
            return Task.Run(() => executor.Execute(config, drawing));

        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Error executing search.");
            throw;
        }
    }

    private static Dictionary<SearchType, ISearchExecutor> InitializeSearchExecutors(IAssemblyCache assemblyCache,
        IDrawingCache drawingCache,
        DrawingResultSelector resultSelector, ICacheKeyGenerator cacheKeyGenerator)
    {
        return new Dictionary<SearchType, ISearchExecutor>
        {
            { SearchType.PartMark, new PartMarkSearchExecutor(resultSelector, drawingCache, cacheKeyGenerator) },
            { SearchType.Text, new TextSearchExecutor(resultSelector, drawingCache) },
            { SearchType.Assembly, new AssemblySearchExecutor(resultSelector, assemblyCache, drawingCache) }
        };
    }


    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}