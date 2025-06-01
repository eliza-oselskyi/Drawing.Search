using System;
using Drawing.Search.Common.Interfaces;
using Drawing.Search.Core.CADIntegrationService.Interfaces;
using Drawing.Search.Core.SearchService.Interfaces;

namespace Drawing.Search.Core.SearchService;

public class SearchService
{
    private readonly IDrawingHandler _drawingHandler;
    private static Lazy<ISearchLogger> LoggerInstance;
    
    public static ISearchLogger GetLoggerInstance() => LoggerInstance.Value;

    public SearchService(IDrawingHandler drawingHandler, ISearchLogger logger)
    {
        _drawingHandler = drawingHandler;
        LoggerInstance = new Lazy<ISearchLogger>(() => logger);
    }

    public void PerformSearch()
    {
        //var drawing = _drawingHandler.GetDrawing();
        Console.WriteLine("We're working");
    }
}