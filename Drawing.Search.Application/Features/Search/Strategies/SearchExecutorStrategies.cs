using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Drawing.Search.Application.Features.Search.Interfaces;
using Drawing.Search.Domain.Drawings;
using Drawing.Search.Domain.Effects;
using Drawing.Search.Domain.Enums;
using Drawing.Search.Domain.Interfaces;
using Drawing.Search.Domain.Observers;
using Drawing.Search.Domain.Search;
using Drawing.Search.Infrastructure.Caching.Models;
using Drawing.Search.Infrastructure.CAD.Extractors;
using Drawing.Search.Infrastructure.CAD.Models;
using Drawing.Search.Infrastructure.CAD.Strategies;
using Drawing.Search.Infrastructure.CAD.Tekla;
using Tekla.Structures.Drawing;
using Tekla.Structures.DrawingInternal;

namespace Drawing.Search.Application.Features.Search.Strategies;

public class PartMarkSearchExecutor : ISearchExecutor
{

    private readonly IDrawingCache _drawingCache;
    private readonly DrawingResultSelector _resultSelector;
    private readonly ICacheKeyGenerator _cacheKeyGenerator;

    public PartMarkSearchExecutor(
        DrawingResultSelector resultSelector,
        IDrawingCache drawingCache,
        ICacheKeyGenerator cacheKeyGenerator)
    {
        //_cacheService = cacheService;

        _drawingCache = drawingCache;
        _cacheKeyGenerator = cacheKeyGenerator;
        
        _resultSelector = resultSelector;
    }
    
    public SearchResult Execute(SearchConfiguration config, Tekla.Structures.Drawing.Drawing drawing)
    {
        var dwgKey = _cacheKeyGenerator.GenerateDrawingKey(drawing.GetIdentifier().ToString());

        var ids = _drawingCache.GetDrawingIdentifiers(drawing.GetIdentifier().ToString());
        //var ids = _cacheService.DumpIdentifiers(drawing.GetIdentifier().ToString());
        var marks = ids.Where(t => _drawingCache.GetDrawingObject(dwgKey, t) is Mark)
            .Select(t => _drawingCache.GetDrawingObject(dwgKey, t) as Mark).ToList();
        var searcher = SearchStrategyFactory.CreateSearcher<Mark>(config);
        var contentCollector = new ContentCollectingObserver(new MarkExtractor());
        searcher.Subscribe(contentCollector);

        Debug.Assert(marks != null, nameof(marks) + " != null");
        if (marks == null) return new SearchResult();
        var results = searcher.Search(marks, SearchStrategyFactory.CreateSearchQuery(config));

        var enumerable = results.ToList();
        _resultSelector.SelectResults(enumerable.Cast<DrawingObject>().ToList());

        return SearchResult.Empty with
        {
            MatchCount = enumerable.Count(),
            ElapsedTime = TimeSpan.Zero, // set by caller
            SearchType = SearchType.PartMark
        };
    }
}

public class TextSearchExecutor(DrawingResultSelector resultSelector, IDrawingCache drawingCache)
    : Interfaces.ISearchExecutor
{
    
    public SearchResult Execute(SearchConfiguration config, Tekla.Structures.Drawing.Drawing drawing)
    {
        if (config is null) throw new ArgumentNullException(nameof(config));
        if (drawing is null) throw new ArgumentNullException(nameof(drawing));

        var drawingId = drawing.GetIdentifier().ToString();
        var domainDrawingId = new DrawingId(drawingId);
        var dwgKey = new CacheKeyBuilder(drawingId).CreateDrawingCacheKey();

        var ids = drawingCache.GetDrawingIdentifiers(drawingId);

        var searchableTexts = ids
            .Select(id => new
            {
                Id = id,
                Object = drawingCache.GetDrawingObject(dwgKey, id)
            })
            .Where(entry => entry.Object is Text)
            .Select(entry =>
            {
                var text = (Text)entry.Object;

                return new SearchableDrawingObject.TextObject(entry.Id, domainDrawingId,
                    text.TextString ?? string.Empty);
            })
            .ToList();
        
        var request = new SearchRequest.Text(config.SearchTerm ?? string.Empty, !config.Wildcard, config.CaseSensitive);

        var planResult = SearchPipeline.TrySearch(searchableTexts, request);

        if (!planResult.IsSuccessful)
            throw planResult.Error;
        
        var plan = planResult.Value;

        InterpretTextSearchEffects(plan, dwgKey);

        return SearchResult.Empty with
        {
            MatchCount = plan.Summary.MatchCount,
            ElapsedTime = plan.Summary.ElapsedTime,
            SearchType = SearchType.Text,
            MatchedContent = plan.Summary.MatchedContent
        };
    }

    private void InterpretTextSearchEffects(SearchPlan plan, string dwgKey)
    {
        foreach (var effect in plan.Effects)
        {
            switch (effect)
            {
                case SearchEffect.SelectTargets targets: 
                    SelectTextTargets(targets.Targets, dwgKey);
                    break;
            }
        }
    }

    private void SelectTextTargets(IReadOnlyList<SelectionTarget> targets, string dwgKey)
    {
        var drawingObjects = targets
            .OfType<SelectionTarget.DrawingObject>()
            .Select(target => drawingCache.GetDrawingObject(dwgKey, target.ObjectId))
            .OfType<DrawingObject>()
            .ToList();
        
        resultSelector.SelectResults(drawingObjects);
    }
}

public class AssemblySearchExecutor : Interfaces.ISearchExecutor
{
    private readonly DrawingResultSelector _resultSelector;
    private readonly IDrawingCache _drawingCache;
    private readonly IAssemblyCache _assemblyCache;

    public AssemblySearchExecutor(DrawingResultSelector resultSelector, IAssemblyCache assemblyCache, IDrawingCache drawingCache)
    {
        _drawingCache = drawingCache;
        _assemblyCache = assemblyCache;
        _resultSelector = resultSelector;
    }
    
    public SearchResult Execute(SearchConfiguration config, Tekla.Structures.Drawing.Drawing drawing)
    {
        var activeDrawing = DrawingHandler.Instance.GetActiveDrawing();
        if (activeDrawing == null)
            throw new InvalidOperationException("No active drawing found.");

        var drawingKey = new CacheKeyBuilder(activeDrawing.GetIdentifier().ToString()).CreateDrawingCacheKey();

        // Instead of searching ModelObjects directly, search the cached assembly positions
        var assemblyPositions = _assemblyCache.GetAllAssemblyPositions();
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
            var relatedIdentifiers = _assemblyCache.GetAssemblyObjects(assemblyPos) as HashSet<string>;


            if (relatedIdentifiers == null) continue;
            var identifiersToProcess = config.ShowAllAssemblyParts
                ? relatedIdentifiers
                : relatedIdentifiers.Where(r => r.Contains("main"));
            foreach (var identifier in identifiersToProcess)
            {
                var relatedObjects = _drawingCache.GetRelatedObjects(
                    activeDrawing.GetIdentifier().ToString(),
                    identifier);
                selectableParts.AddRange(relatedObjects.OfType<Part>());
            }
        }

        TeklaWrapper.DrawingObjectListToSelection(selectableParts.Cast<DrawingObject>().ToList(), activeDrawing);

        return SearchResult.Empty with
        {
            MatchCount = selectableParts.Count(),
            ElapsedTime = TimeSpan.Zero, // set by caller
            SearchType = SearchType.Assembly
        };
    }
}