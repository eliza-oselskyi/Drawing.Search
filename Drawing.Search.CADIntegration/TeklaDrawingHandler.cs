using System.Collections.Generic;
using Drawing.Search.CADIntegration.Interfaces;
using Drawing.Search.Core.CADIntegrationService;
using Drawing.Search.Core.CADIntegrationService.Interfaces;
using Tekla.Structures.Drawing;
using Tekla.Structures.DrawingInternal;

namespace Drawing.Search.CADIntegration;

public class TeklaDrawingHandler : IDrawingHandler
{
    private readonly DrawingHandler _drawingHandler = new DrawingHandler();
    public IDrawing GetDrawing()
    {
        var drawing = new TeklaDrawingWrapper(_drawingHandler.GetActiveDrawing());
        return drawing;
    }

    public IDrawing? GetDrawing(string drawingId)
    {
        var allDrawings = _drawingHandler.GetDrawings();
        foreach (Tekla.Structures.Drawing.Drawing drawing in allDrawings)
        {
            if (drawing.GetIdentifier().ToString() == drawingId)
            {
                return new TeklaDrawingWrapper(drawing);
            }
        }
        return null;
    }

    public void SelectResultsInDrawing<T>(List<T> results)
    {
        throw new System.NotImplementedException();
    }
}