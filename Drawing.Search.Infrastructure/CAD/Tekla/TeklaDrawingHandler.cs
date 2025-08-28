using System;
using System.Collections.Generic;
using Drawing.Search.Domain.Interfaces;
using Tekla.Structures.Drawing;
using Tekla.Structures.DrawingInternal;

namespace Drawing.Search.Infrastructure.CAD.Tekla;

public class TeklaDrawingHandler : IDrawingHandler
{
    private readonly DrawingHandler _drawingHandler = new();

    public IDrawing GetDrawing()
    {
        var drawing = new TeklaDrawingWrapper(_drawingHandler.GetActiveDrawing());
        return drawing;
    }

    public void SelectResultsInDrawing<T>(List<T> results)
    {
        throw new NotImplementedException();
    }

    public IDrawing? GetDrawing(string drawingId)
    {
        var allDrawings = _drawingHandler.GetDrawings();
        foreach (global::Tekla.Structures.Drawing.Drawing drawing in allDrawings)
            if (drawing.GetIdentifier().ToString() == drawingId)
                return new TeklaDrawingWrapper(drawing);

        return null;
    }
}