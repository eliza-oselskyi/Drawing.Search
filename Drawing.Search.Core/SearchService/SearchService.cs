using System;
using Drawing.Search.CADIntegration.Interfaces;
using Drawing.Search.Common.Interfaces;

namespace Drawing.Search.Core.SearchService;

public class SearchService
{
    private static Lazy<ISearchLogger> _loggerInstance = new();
    private readonly IDrawingHandler _drawingHandler;

    public SearchService(IDrawingHandler drawingHandler, ISearchLogger logger)
    {
        _drawingHandler = drawingHandler;
        _loggerInstance = new Lazy<ISearchLogger>(() => logger);
    }

    public static ISearchLogger GetLoggerInstance()
    {
        return _loggerInstance.Value;
    }

    public void PerformSearch()
    {
        //var drawing = _drawingHandler.GetDrawing();
        Console.WriteLine("We're working");
    }
}