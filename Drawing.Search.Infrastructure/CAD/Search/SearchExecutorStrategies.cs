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

    public Task<SearchResult> ExecuteAsync(SearchConfiguration config, global::Tekla.Structures.Drawing.Drawing drawing, CancellationToken cancellationToken = default)
    {
        if (config is null) throw new ArgumentNullException(nameof(config));
        if (drawing is null) throw new ArgumentNullException(nameof(drawing));
        
        var drawingId = drawing.GetIdentifier().ToString();
        var dwgKey = cacheKeyGenerator.GenerateDrawingKey(drawingId);

        return TeklaSearchExecutorCore.ExecuteAsync(
            TeklaSearchableObjects.PartMarks(drawingCache, cacheKeyGenerator, drawing),
            config,
            SearchType.PartMark,
            (effect, token) => _effectInterpreter.InterpretAsync(effect, dwgKey, drawing, token),
            execution => execution.Plan.Summary.MatchCount,
            cancellationToken);
    }
}

public class TextSearchExecutor(DrawingResultSelector resultSelector, IDrawingCache drawingCache, IAssemblyCache assemblyCache)
    : ISearchExecutor
{
    private readonly DrawingSearchEffectInterpreter _effectInterpreter = new(drawingCache, assemblyCache, resultSelector);

    public Task<SearchResult> ExecuteAsync(SearchConfiguration config, global::Tekla.Structures.Drawing.Drawing drawing, CancellationToken cancellationToken = default)
    {
        if (config is null) throw new ArgumentNullException(nameof(config));
        if (drawing is null) throw new ArgumentNullException(nameof(drawing));

        var drawingId = drawing.GetIdentifier().ToString();
        var dwgKey = new CacheKeyBuilder(drawingId).CreateDrawingCacheKey();

        return TeklaSearchExecutorCore.ExecuteAsync(
            TeklaSearchableObjects.Texts(drawingCache, drawing),
            config,
            SearchType.Text,
            (effect, token) => _effectInterpreter.InterpretAsync(effect, dwgKey, drawing, token),
            execution => execution.Plan.Summary.MatchCount,
            cancellationToken);
    }
}

public class AssemblySearchExecutor(
    DrawingResultSelector resultSelector,
    IAssemblyCache assemblyCache,
    IDrawingCache drawingCache)
    : ISearchExecutor
{
    
    private readonly DrawingSearchEffectInterpreter _effectInterpreter = new(drawingCache, assemblyCache, resultSelector);

    public Task<SearchResult> ExecuteAsync(SearchConfiguration config, global::Tekla.Structures.Drawing.Drawing drawing, CancellationToken cancellationToken = default)
    {
        if (config is null) throw new ArgumentNullException(nameof(config));
        if (drawing is null) throw new ArgumentNullException(nameof(drawing));
        
        var drawingId = drawing.GetIdentifier().ToString();
        var dwgKey = new CacheKeyBuilder(drawingId).CreateDrawingCacheKey();

        return TeklaSearchExecutorCore.ExecuteAsync(
            TeklaSearchableObjects.Assemblies(assemblyCache, drawing),
            config,
            SearchType.Assembly,
            (effect, token) => _effectInterpreter.InterpretAsync(effect, dwgKey, drawing, token),
            execution => execution.Interpretation.SelectedObjectCount,
            cancellationToken);
    }
}