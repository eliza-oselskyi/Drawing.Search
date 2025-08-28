using System.Collections.Generic;
using System.Linq;
using Drawing.Search.Domain.Interfaces;
using Drawing.Search.Infrastructure.CAD.Tekla;
using Tekla.Structures.Drawing;
using Tekla.Structures.Model;
using ModelObject = Tekla.Structures.Model.ModelObject;

namespace Drawing.Search.Infrastructure.CAD.Models;

public class DrawingResultSelector
{
    
    private readonly IDrawingProvider _drawingProvider;

    public DrawingResultSelector(IDrawingProvider drawingProvider)
    {
        _drawingProvider = drawingProvider;
    }

    public void SelectResults<T>(List<T> results) where T : class
    {
        var drawing = _drawingProvider.GetActiveDrawing();
        var selector = _drawingProvider.GetSelector();
        selector.UnselectAllObjects();

        if (typeof(T) == typeof(ModelObject))
            TeklaWrapper.ModelObjectListToSelection(results.Cast<ModelObject>().ToList(), drawing);
        else
            TeklaWrapper.DrawingObjectListToSelection(results.Cast<DrawingObject>().ToList(), drawing);
    }
    
}