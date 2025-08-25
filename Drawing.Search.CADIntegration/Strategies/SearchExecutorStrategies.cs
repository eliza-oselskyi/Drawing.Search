using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Drawing.Search.Caching;
using Drawing.Search.Caching.Interfaces;
using Drawing.Search.CADIntegration.TeklaWrappers;
using Drawing.Search.Common.Enums;
using Drawing.Search.Common.Observers;
using Drawing.Search.Common.SearchTypes;
using Tekla.Structures.Drawing;
using Tekla.Structures.DrawingInternal;

namespace Drawing.Search.CADIntegration.Strategies;

public class PartMarkSearchExecutor : Interfaces.ISearchExecutor
{

    private readonly ICacheService _cacheService;
    private readonly DrawingResultSelector _resultSelector;

    public PartMarkSearchExecutor(ICacheService cacheService, DrawingResultSelector resultSelector)
    {
        _cacheService = cacheService;
        _resultSelector = resultSelector;
    }
    
    public SearchResult Execute(SearchConfiguration config, Tekla.Structures.Drawing.Drawing drawing)
    {
        var dwgKey = new CacheKeyBuilder(drawing.GetIdentifier().ToString()).CreateDrawingCacheKey();

        var ids = _cacheService.DumpIdentifiers(drawing.GetIdentifier().ToString());
        var marks = ids.Where(t => _cacheService.GetFromCache(dwgKey, t) is Mark)
            .Select(t => _cacheService.GetFromCache(dwgKey, t) as Mark).ToList();
        var searcher = SearchStrategyFactory.CreateSearcher<Mark>(config);
        var contentCollector = new ContentCollectingObserver(new MarkExtractor());
        searcher.Subscribe(contentCollector);

        Debug.Assert(marks != null, nameof(marks) + " != null");
        if (marks == null) return new SearchResult();
        var results = searcher.Search(marks, SearchStrategyFactory.CreateSearchQuery(config));

        var enumerable = results.ToList();
        _resultSelector.SelectResults(enumerable.Cast<DrawingObject>().ToList());

        return new SearchResult
        {
            MatchCount = enumerable.Count(),
            ElapsedMilliseconds = 0, // Set by caller
            SearchType = SearchType.PartMark
        };
    }
}

public class TextSearchExecutor : Interfaces.ISearchExecutor
{
    private readonly ICacheService _cacheService;
    private readonly DrawingResultSelector _resultSelector;

    public TextSearchExecutor(ICacheService cacheService, DrawingResultSelector resultSelector)
    {
        _cacheService = cacheService;
        _resultSelector = resultSelector;
    }
    
    
    public SearchResult Execute(SearchConfiguration config, Tekla.Structures.Drawing.Drawing drawing)
    {
        var dwgKey = new CacheKeyBuilder(drawing.GetIdentifier().ToString()).CreateDrawingCacheKey();

        var ids = _cacheService.DumpIdentifiers(drawing.GetIdentifier().ToString());

        var texts = ids.Where(t => _cacheService.GetFromCache(dwgKey, t) is Text)
            .Select(t => _cacheService.GetFromCache(dwgKey, t) as Text).ToList();
        var searcher = SearchStrategyFactory.CreateSearcher<Text>(config);
        var contentCollector = new ContentCollectingObserver(new TextExtractor());
        searcher.Subscribe(contentCollector);

        Debug.Assert(texts != null, nameof(texts) + " != null");
        var results = searcher.Search(texts ?? throw new InvalidOperationException("No search term"),
            SearchStrategyFactory.CreateSearchQuery(config));

        var enumerable = results.ToList();
        _resultSelector.SelectResults(enumerable.Cast<DrawingObject>().ToList());

        return new SearchResult()
        {
            MatchCount = enumerable.Count(),
            ElapsedMilliseconds = 0, // Set by caller
            SearchType = SearchType.Text
        };
    }
}

public class AssemblySearchExecutor : Interfaces.ISearchExecutor
{
    private readonly ICacheService _cacheService;
    private readonly DrawingResultSelector _resultSelector;

    public AssemblySearchExecutor(ICacheService cacheService, DrawingResultSelector resultSelector)
    {
        _cacheService = cacheService;
        _resultSelector = resultSelector;
    }
    
    public SearchResult Execute(SearchConfiguration config, Tekla.Structures.Drawing.Drawing drawing)
    {
        var activeDrawing = DrawingHandler.Instance.GetActiveDrawing();
        if (activeDrawing == null)
            throw new InvalidOperationException("No active drawing found.");

        var drawingKey = new CacheKeyBuilder(activeDrawing.GetIdentifier().ToString()).CreateDrawingCacheKey();

        // Instead of searching ModelObjects directly, search the cached assembly positions
        var assemblyPositions = _cacheService.DumpAssemblyPositions();
        var searcher = SearchStrategyFactory.CreateSearcher<string>(config);
        var contentCollector = new ContentCollectingObserver(new StringExtractor());
        searcher.Subscribe(contentCollector);

        // Search through assembly positions
        var matchedAssemblyPositions = searcher.Search(assemblyPositions, SearchStrategyFactory.CreateSearchQuery(config));

        // Get all parts related to matched assembly positions
        var selectableParts = new List<Part>();
        foreach (var assemblyPos in matchedAssemblyPositions)
        {
            if (assemblyPos == null) continue;
            var relatedIdentifiers = _cacheService.FetchAssemblyPosition(assemblyPos) as HashSet<string>;


            if (relatedIdentifiers == null) continue;
            var identifiersToProcess = config.ShowAllAssemblyParts
                ? relatedIdentifiers
                : relatedIdentifiers.Where(r => r.Contains("main"));
            foreach (var identifier in identifiersToProcess)
            {
                var relatedObjects = _cacheService.GetRelatedObjects(
                    activeDrawing.GetIdentifier().ToString(),
                    identifier);
                selectableParts.AddRange(relatedObjects.OfType<Part>());
            }
        }

        TeklaWrapper.DrawingObjectListToSelection(selectableParts.Cast<DrawingObject>().ToList(), activeDrawing);

        return new SearchResult
        {
            MatchCount = selectableParts.Count,
            ElapsedMilliseconds = 0,
            SearchType = SearchType.Assembly
        };
    }
}