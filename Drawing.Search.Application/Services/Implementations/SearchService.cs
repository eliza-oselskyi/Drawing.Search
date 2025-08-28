using System;
using Drawing.Search.Domain.Interfaces;

namespace Drawing.Search.Application.Services.Implementations;

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
}