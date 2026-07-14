using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Drawing.Search.Application.Features.Search.Interfaces;
using Drawing.Search.Domain.Drawings;
using Drawing.Search.Domain.Effects;
using Drawing.Search.Domain.Enums;
using Drawing.Search.Domain.Interfaces;
using Drawing.Search.Domain.Search;
using Drawing.Search.Infrastructure.Caching.Models;
using Drawing.Search.Infrastructure.CAD.Extractors;
using Drawing.Search.Infrastructure.CAD.Models;
using Drawing.Search.Infrastructure.CAD.Tekla;
using Tekla.Structures.Drawing;
using Tekla.Structures.DrawingInternal;

namespace Drawing.Search.Application.Features.Search.Strategies;

public class PartMarkSearchExecutor(
    DrawingResultSelector resultSelector,
    IDrawingCache drawingCache,
    ICacheKeyGenerator cacheKeyGenerator,
    IAssemblyCache assemblyCache)
    : ISearchExecutor
{
    private readonly DrawingSearchEffectInterpreter _effectInterpreter = new(drawingCache, assemblyCache, resultSelector);

    public SearchResult Execute(SearchConfiguration config, Tekla.Structures.Drawing.Drawing drawing)
    {
        if (config is null) throw new ArgumentNullException(nameof(config));
        if (drawing is null) throw new ArgumentNullException(nameof(drawing));
        
        var drawingId = drawing.GetIdentifier().ToString();
        var domainDrawingId = new DrawingId(drawingId);
        var dwgKey = cacheKeyGenerator.GenerateDrawingKey(drawingId);
        
        var ids = drawingCache.GetDrawingIdentifiers(drawingId);

        var searchableMarks = ids
            .Select(id => new
            {
                Id = id,
                Object = drawingCache.GetDrawingObject(dwgKey, id)
            })
            .Where(entry => entry.Object is Mark)
            .Select(entry =>
            {
                var mark = (Mark)entry.Object;

                return new SearchableDrawingObject.PartMarkObject(entry.Id, domainDrawingId,
                    MarkSearchText.Extract(mark));
            })
            .ToList();
        
        var request = new SearchRequest.PartMark(config.SearchTerm ?? string.Empty, !config.Wildcard, config.CaseSensitive);
        
        var planResult = SearchPipeline.TrySearch(searchableMarks, request);
        
        if (!planResult.IsSuccessful)
            throw planResult.Error;
        
        var plan = planResult.Value;
        
        _effectInterpreter.Interpret(plan, dwgKey, drawing);

        return SearchResult.Empty with
        {
            MatchCount = plan.Summary.MatchCount,
            ElapsedTime = plan.Summary.ElapsedTime,
            SearchType = SearchType.PartMark,
            MatchedContent = plan.Summary.MatchedContent
        };
    }
}

public class TextSearchExecutor(DrawingResultSelector resultSelector, IDrawingCache drawingCache, IAssemblyCache assemblyCache)
    : Interfaces.ISearchExecutor
{
    private readonly DrawingResultSelector _resultSelector = resultSelector;
    private readonly DrawingSearchEffectInterpreter _effectInterpreter = new(drawingCache, assemblyCache, resultSelector);

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

        _effectInterpreter.Interpret(plan, dwgKey, drawing);

        return SearchResult.Empty with
        {
            MatchCount = plan.Summary.MatchCount,
            ElapsedTime = plan.Summary.ElapsedTime,
            SearchType = SearchType.Text,
            MatchedContent = plan.Summary.MatchedContent
        };
    }
}

public class AssemblySearchExecutor(
    DrawingResultSelector resultSelector,
    IAssemblyCache assemblyCache,
    IDrawingCache drawingCache)
    : Interfaces.ISearchExecutor
{
    
    private readonly DrawingSearchEffectInterpreter _effectInterpreter = new(drawingCache, assemblyCache, resultSelector);

    public SearchResult Execute(SearchConfiguration config, Tekla.Structures.Drawing.Drawing drawing)
    {
        if (config is null) throw new ArgumentNullException(nameof(config));
        if (drawing is null) throw new ArgumentNullException(nameof(drawing));
        
        var drawingId = drawing.GetIdentifier().ToString();
        var domainDrawingId = new DrawingId(drawingId);
        var dwgKey = new CacheKeyBuilder(drawingId).CreateDrawingCacheKey();

        var searchableAssemblies = assemblyCache.GetAllAssemblyPositions()
            .Where(position => !string.IsNullOrWhiteSpace(position))
            .Select(position => new SearchableDrawingObject.AssemblyObject(
                position,
                domainDrawingId,
                position,
                AssemblyPosition: position,
                IsMainPart: true))
            .ToList();

        var request = new SearchRequest.Assembly(
            config.SearchTerm ?? string.Empty,
            !config.Wildcard,
            config.CaseSensitive,
            config.ShowAllAssemblyParts);
        
        var planResult = SearchPipeline.TrySearch(searchableAssemblies, request);
        
        if (!planResult.IsSuccessful)
            throw planResult.Error;
        
        var plan = planResult.Value;
        
        _effectInterpreter.Interpret(plan, dwgKey, drawing);

        return SearchResult.Empty with
        {
            MatchCount = plan.Summary.MatchCount,
            ElapsedTime = plan.Summary.ElapsedTime,
            SearchType = SearchType.Assembly,
            MatchedContent = plan.Summary.MatchedContent
        };
    }
}