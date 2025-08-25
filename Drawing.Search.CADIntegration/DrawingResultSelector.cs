using System.Collections.Generic;
using System.Linq;
using Drawing.Search.CADIntegration.Interfaces;
using Drawing.Search.CADIntegration.TeklaWrappers;
using Tekla.Structures.Drawing;

namespace Drawing.Search.CADIntegration;

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
            TeklaWrapper.ModelObjectListToSelection(results.Cast<Tekla.Structures.Model.ModelObject>().ToList(), drawing);
        else
            TeklaWrapper.DrawingObjectListToSelection(results.Cast<DrawingObject>().ToList(), drawing);
    }
    
}