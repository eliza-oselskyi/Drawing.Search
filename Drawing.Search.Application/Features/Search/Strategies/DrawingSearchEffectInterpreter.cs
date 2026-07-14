using System.Collections.Generic;
using System.Linq;
using Drawing.Search.Domain.Effects;
using Drawing.Search.Domain.Interfaces;
using Drawing.Search.Domain.Search;
using Drawing.Search.Infrastructure.CAD.Models;
using Tekla.Structures.Drawing;
using Tekla.Structures.DrawingInternal;

namespace Drawing.Search.Application.Features.Search.Strategies;

internal sealed class DrawingSearchEffectInterpreter(IDrawingCache drawingCache, IAssemblyCache assemblyCache, DrawingResultSelector resultSelector)
{
    public void Interpret(SearchPlan plan, string drawingCacheKey, Tekla.Structures.Drawing.Drawing drawing)
    {
        foreach (var effect in plan.Effects)
        {
            switch (effect)
            {
                case SearchEffect.SelectTargets selectTargets:
                    SelectTargets(selectTargets.Targets, drawingCacheKey, drawing);
                    break;
            }
        }
    }

    private void SelectTargets(IReadOnlyList<SelectionTarget> targets, string drawingCacheKey, Tekla.Structures.Drawing.Drawing drawing)
    {
        var directDrawingObjects = GetDrawingObjectTargets(targets, drawingCacheKey);
        var assemblyDrawingObjects = GetAssemblyPositionTargets(targets, drawing);
        
        var selectedObjects = directDrawingObjects
            .Concat(assemblyDrawingObjects)
            .Distinct()
            .ToList();
        
        resultSelector.SelectResults(selectedObjects);
    }

    private IEnumerable<DrawingObject> GetAssemblyPositionTargets(IReadOnlyList<SelectionTarget> targets, Tekla.Structures.Drawing.Drawing drawing)
    {
        var drawingId = drawing.GetIdentifier().ToString();

        return targets
            .OfType<SelectionTarget.AssemblyPosition>()
            .SelectMany(target =>
            {
                var relatedIdentifiers = assemblyCache.GetAssemblyObjects(target.Position) as HashSet<string>;

                if (relatedIdentifiers is null)
                    return Enumerable.Empty<DrawingObject>();

                var identifiersToProcess = target.IncludeAllParts
                    ? relatedIdentifiers
                    : relatedIdentifiers.Where(r => r.Contains("main"));

                return identifiersToProcess
                    .SelectMany(identifier => drawingCache.GetRelatedObjects(drawingId, identifier))
                    .OfType<DrawingObject>();
            });
    }

    private IEnumerable<DrawingObject> GetDrawingObjectTargets(IReadOnlyList<SelectionTarget> targets, string drawingCacheKey)
    {
        return targets
            .OfType<SelectionTarget.DrawingObject>()
            .Select(target => drawingCache.GetDrawingObject(drawingCacheKey, target.ObjectId))
            .OfType<DrawingObject>();
    }
}