using System;
using Drawing.Search.Core.CADIntegrationService.Interfaces;

namespace Drawing.Search.Core.SearchService;

public class SearchService
{
    private readonly IDrawingHandler _drawingHandler;

    public SearchService(IDrawingHandler drawingHandler)
    {
        _drawingHandler = drawingHandler;
    }

    public void PerformSearch()
    {
        //var drawing = _drawingHandler.GetDrawing();
        Console.WriteLine("We're working");
    }
}