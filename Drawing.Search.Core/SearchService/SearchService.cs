using System;
using Drawing.Search.Core.CADIntegrationService.Interfaces;
using Drawing.Search.Core.SearchService.Interfaces;

namespace Drawing.Search.Core.SearchService;

public class SearchService
{
    private readonly IDrawingHandler _drawingHandler;
    private static readonly Lazy<ISearchLogger> LoggerInstance = new Lazy<ISearchLogger>(() => new SearchLogger());
    
    public static ISearchLogger GetLoggerInstance() => LoggerInstance.Value;

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