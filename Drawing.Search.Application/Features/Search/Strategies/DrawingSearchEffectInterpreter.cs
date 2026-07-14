using System.Collections.Generic;
using System.Linq;
using Drawing.Search.Domain.Effects;
using Drawing.Search.Domain.Interfaces;
using Drawing.Search.Domain.Search;
using Drawing.Search.Infrastructure.CAD.Models;
using Tekla.Structures.Drawing;

namespace Drawing.Search.Application.Features.Search.Strategies;

internal sealed class DrawingSearchEffectInterpreter(IDrawingCache drawingCache, DrawingResultSelector resultSelector)
{
    public void Interpret(SearchPlan plan, string drawingCacheKey)
    {
        foreach (var effect in plan.Effects)
        {
            switch (effect)
            {
                case SearchEffect.SelectTargets selectTargets:
                    SelectDrawingObjectTargets(selectTargets.Targets, drawingCacheKey);
                    break;
            }
        }
    }

    private void SelectDrawingObjectTargets(IReadOnlyList<SelectionTarget> targets, string drawingCacheKey)
    {
        var drawingObjects = targets
            .OfType<SelectionTarget.DrawingObject>()
            .Select(target => drawingCache.GetDrawingObject(drawingCacheKey, target.ObjectId))
            .OfType<DrawingObject>()
            .ToList();
        
        resultSelector.SelectResults(drawingObjects);
    }
}