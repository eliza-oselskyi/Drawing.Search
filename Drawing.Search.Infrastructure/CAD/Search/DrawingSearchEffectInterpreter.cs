using System.Collections.Generic;
using System.Linq;
using Drawing.Search.Domain.Effects;
using Drawing.Search.Domain.Interfaces;
using Drawing.Search.Domain.Search;
using Drawing.Search.Infrastructure.CAD.Models;
using Tekla.Structures.Drawing;
using Tekla.Structures.DrawingInternal;

namespace Drawing.Search.Infrastructure.CAD.Search;

internal sealed class DrawingSearchEffectInterpreter(
    IDrawingCache drawingCache,
    IAssemblyCache assemblyCache,
    DrawingResultSelector resultSelector)
{
    public SearchEffectInterpretationResult Interpret(SearchPlan plan, string drawingCacheKey, global::Tekla.Structures.Drawing.Drawing drawing)
    {
        var selectedObjectCount = 0;
        
        foreach (var effect in plan.Effects)
        {
            switch (effect)
            {
                case SearchEffect.SelectTargets selectTargets:
                    selectedObjectCount += SelectTargets(selectTargets.Targets, drawingCacheKey, drawing);
                    break;
            }
        }
        
        return new SearchEffectInterpretationResult(selectedObjectCount);
    }

    private int SelectTargets(
        IReadOnlyList<SelectionTarget> targets,
        string drawingCacheKey,
        global::Tekla.Structures.Drawing.Drawing drawing)
    {
        var directDrawingObjects = GetDrawingObjectTargets(targets, drawingCacheKey).ToList();
        var assemblyParts = GetAssemblyPositionTargets(targets, drawing).ToList();

        var selectedObjects = directDrawingObjects
            .Concat(assemblyParts.Cast<DrawingObject>())
            .ToList();

        resultSelector.SelectResults(selectedObjects);
        
        return assemblyParts.Count > 0
            ? assemblyParts.Count
            : directDrawingObjects.Count;
    }

    private IEnumerable<Part> GetAssemblyPositionTargets(
        IReadOnlyList<SelectionTarget> targets,
        global::Tekla.Structures.Drawing.Drawing drawing)
    {
        var drawingId = drawing.GetIdentifier().ToString();

        return targets
            .OfType<SelectionTarget.AssemblyPosition>()
            .SelectMany(target =>
            {
                var relatedIdentifiers = assemblyCache.GetAssemblyObjects(target.Position) as HashSet<string>;

                if (relatedIdentifiers is null)
                    return [];

                var identifiersToProcess = target.IncludeAllParts
                    ? relatedIdentifiers
                    : relatedIdentifiers.Where(identifier => identifier.Contains("main"));

                return identifiersToProcess
                    .SelectMany(identifier => drawingCache.GetRelatedObjects(drawingId, identifier))
                    .OfType<Part>();
            });
    }

    private IEnumerable<DrawingObject> GetDrawingObjectTargets(
        IReadOnlyList<SelectionTarget> targets,
        string drawingCacheKey)
    {
        return targets
            .OfType<SelectionTarget.DrawingObject>()
            .Select(target => drawingCache.GetDrawingObject(drawingCacheKey, target.ObjectId))
            .OfType<DrawingObject>();
    }
}