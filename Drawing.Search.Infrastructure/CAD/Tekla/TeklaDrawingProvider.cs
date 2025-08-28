using System;
using Drawing.Search.Domain.Interfaces;
using Tekla.Structures.Drawing;
using Tekla.Structures.Drawing.UI;
using Tekla.Structures.Model;

namespace Drawing.Search.Infrastructure.CAD.Tekla;

public class TeklaDrawingProvider : IDrawingProvider
{
    private readonly DrawingHandler _drawingHandler;

    public TeklaDrawingProvider()
    {
        _drawingHandler = DrawingHandler.Instance;
        if (!new Model().GetConnectionStatus())
            throw new ApplicationException($"Tekla connection not established.");
    }
    public global::Tekla.Structures.Drawing.Drawing GetActiveDrawing() => DrawingHandler.Instance.GetActiveDrawing();

    public DrawingObjectSelector GetSelector() => DrawingHandler.Instance.GetDrawingObjectSelector();
}