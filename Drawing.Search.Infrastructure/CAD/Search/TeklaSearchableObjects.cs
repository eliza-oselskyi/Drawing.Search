using System.Collections.Generic;
using System.Linq;
using Drawing.Search.Domain.Drawings;
using Drawing.Search.Domain.Interfaces;
using Drawing.Search.Infrastructure.Caching.Models;
using Drawing.Search.Infrastructure.CAD.Extractors;
using Tekla.Structures.Drawing;
using Tekla.Structures.DrawingInternal;

namespace Drawing.Search.Infrastructure.CAD.Search;

internal static class TeklaSearchableObjects
{
    public static IReadOnlyList<SearchableDrawingObject.TextObject> Texts(
        IDrawingCache drawingCache,
        global::Tekla.Structures.Drawing.Drawing drawing)
    {
        var drawingId = drawing.GetIdentifier().ToString();
        var domainDrawingId = new DrawingId(drawingId);
        var drawingCacheKey = new CacheKeyBuilder(drawingId).CreateDrawingCacheKey();

        return drawingCache
            .GetDrawingIdentifiers(drawingId)
            .Select(id => new
            {
                Id = id,
                Object = drawingCache.GetDrawingObject(drawingCacheKey, id)
            })
            .Where(entry => entry.Object is Text)
            .Select(entry =>
            {
                var text = (Text)entry.Object;

                return new SearchableDrawingObject.TextObject(
                    entry.Id,
                    domainDrawingId,
                    text.TextString ?? string.Empty);
            })
            .ToList();
    }

    public static IReadOnlyList<SearchableDrawingObject.PartMarkObject> PartMarks(
        IDrawingCache drawingCache,
        ICacheKeyGenerator cacheKeyGenerator,
        global::Tekla.Structures.Drawing.Drawing drawing)
    {
        var drawingId = drawing.GetIdentifier().ToString();
        var domainDrawingId = new DrawingId(drawingId);
        var drawingCacheKey = cacheKeyGenerator.GenerateDrawingKey(drawingId);

        return drawingCache
            .GetDrawingIdentifiers(drawingId)
            .Select(id => new
            {
                Id = id,
                Object = drawingCache.GetDrawingObject(drawingCacheKey, id)
            })
            .Where(entry => entry.Object is Mark)
            .Select(entry =>
            {
                var mark = (Mark)entry.Object;

                return new SearchableDrawingObject.PartMarkObject(
                    entry.Id,
                    domainDrawingId,
                    MarkSearchText.Extract(mark));
            })
            .ToList();
    }

    public static IReadOnlyList<SearchableDrawingObject.AssemblyObject> Assemblies(
        IAssemblyCache assemblyCache,
        global::Tekla.Structures.Drawing.Drawing drawing)
    {
        var drawingId = drawing.GetIdentifier().ToString();
        var domainDrawingId = new DrawingId(drawingId);

        return assemblyCache
            .GetAllAssemblyPositions()
            .Where(position => !string.IsNullOrWhiteSpace(position))
            .Select(position => new SearchableDrawingObject.AssemblyObject(
                position,
                domainDrawingId,
                position,
                AssemblyPosition: position,
                IsMainPart: true))
            .ToList();
    }
}
