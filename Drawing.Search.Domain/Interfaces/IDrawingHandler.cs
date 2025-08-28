using System.Collections.Generic;

namespace Drawing.Search.Domain.Interfaces;

public interface IDrawingHandler
{
    IDrawing GetDrawing();
    void SelectResultsInDrawing<T>(List<T> results);
}