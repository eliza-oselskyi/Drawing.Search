using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Drawing.Search.Application.Effects;
using Drawing.Search.Application.Features.Search;
using Drawing.Search.Domain.Drawings;
using Drawing.Search.Domain.Enums;
using Drawing.Search.Domain.Interfaces;
using Drawing.Search.Infrastructure.Caching.Models;
using Drawing.Search.Infrastructure.CAD.Extractors;
using Drawing.Search.Infrastructure.CAD.Models;
using Tekla.Structures.Drawing;
using Tekla.Structures.DrawingInternal;

namespace Drawing.Search.Infrastructure.CAD.Search;

public class PartMarkSearchExecutor(
    DrawingResultSelector resultSelector,
    IDrawingCache drawingCache,
    ICacheKeyGenerator cacheKeyGenerator,
    IAssemblyCache assemblyCache)
    : ISearchExecutor
{
    private readonly DrawingSearchEffectInterpreter _effectInterpreter = new(drawingCache, assemblyCache, resultSelector);

    public async Task<SearchResult> ExecuteAsync(SearchConfiguration config, global::Tekla.Structures.Drawing.Drawing drawing, CancellationToken cancellationToken = default)
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
        
        var planResult = SearchPipeline.TrySearch(searchableMarks, config.ToSearchRequest());

        var executionResult = await SearchPlanRunner.RunAsync(
            planResult,
            (effect, token) => _effectInterpreter.InterpretAsync(effect, dwgKey, drawing, token),
            SearchEffectInterpretationResult.Combine,
            SearchEffectInterpretationResult.Empty,
            cancellationToken);

        if (!executionResult.IsSuccessful)
            throw executionResult.Error;
        
        var execution = executionResult.Value;

        return SearchResult.Empty with
        {
            MatchCount = execution.Plan.Summary.MatchCount,
            ElapsedTime = execution.Plan.Summary.ElapsedTime,
            SearchType = SearchType.Assembly,
            MatchedContent = execution.Plan.Summary.MatchedContent
        };
    }
}

public class TextSearchExecutor(DrawingResultSelector resultSelector, IDrawingCache drawingCache, IAssemblyCache assemblyCache)
    : ISearchExecutor
{
    private readonly DrawingSearchEffectInterpreter _effectInterpreter = new(drawingCache, assemblyCache, resultSelector);

    public async Task<SearchResult> ExecuteAsync(SearchConfiguration config, global::Tekla.Structures.Drawing.Drawing drawing, CancellationToken cancellationToken = default)
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
        
        var planResult = SearchPipeline.TrySearch(searchableTexts, config.ToSearchRequest());

        var executionResult = await SearchPlanRunner.RunAsync(
            planResult,
            (effect, token) => _effectInterpreter.InterpretAsync(effect, dwgKey, drawing, token),
            SearchEffectInterpretationResult.Combine,
            SearchEffectInterpretationResult.Empty,
            cancellationToken);

        if (!executionResult.IsSuccessful)
            throw executionResult.Error;
        
        var execution = executionResult.Value;

        return SearchResult.Empty with
        {
            MatchCount = execution.Plan.Summary.MatchCount,
            ElapsedTime = execution.Plan.Summary.ElapsedTime,
            SearchType = SearchType.Assembly,
            MatchedContent = execution.Plan.Summary.MatchedContent
        };
    }
}

public class AssemblySearchExecutor(
    DrawingResultSelector resultSelector,
    IAssemblyCache assemblyCache,
    IDrawingCache drawingCache)
    : ISearchExecutor
{
    
    private readonly DrawingSearchEffectInterpreter _effectInterpreter = new(drawingCache, assemblyCache, resultSelector);

    public async Task<SearchResult> ExecuteAsync(SearchConfiguration config, global::Tekla.Structures.Drawing.Drawing drawing, CancellationToken cancellationToken = default)
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

        var planResult = SearchPipeline.TrySearch(searchableAssemblies, config.ToSearchRequest());

        var executionResult = await SearchPlanRunner.RunAsync(
            planResult,
            (effect, token) => _effectInterpreter.InterpretAsync(effect, dwgKey, drawing, token),
            SearchEffectInterpretationResult.Combine,
            SearchEffectInterpretationResult.Empty,
            cancellationToken);

        if (!executionResult.IsSuccessful)
            throw executionResult.Error;
        
        var execution = executionResult.Value;

        return SearchResult.Empty with
        {
            MatchCount = execution.Interpretation.SelectedObjectCount,
            ElapsedTime = execution.Plan.Summary.ElapsedTime,
            SearchType = SearchType.Assembly,
            MatchedContent = execution.Plan.Summary.MatchedContent
        };
    }
}