using System.Collections.Generic;
using System.Linq;
using Drawing.Search.CADIntegration.TeklaWrappers;
using Tekla.Structures.Drawing;

namespace Drawing.Search.CADIntegration;

public class DrawingResultSelector
{
    
    private readonly DrawingHandler _drawingHandler = DrawingHandler.Instance;

    public void SelectResults<T>(List<T> results) where T : class
    {
        var drawing = _drawingHandler.GetActiveDrawing();
        var selector = _drawingHandler.GetDrawingObjectSelector();
        selector.UnselectAllObjects();

        if (typeof(T) == typeof(ModelObject))
            TeklaWrapper.ModelObjectListToSelection(results.Cast<Tekla.Structures.Model.ModelObject>().ToList(), drawing);
        else
            TeklaWrapper.DrawingObjectListToSelection(results.Cast<DrawingObject>().ToList(), drawing);
    }
    
}