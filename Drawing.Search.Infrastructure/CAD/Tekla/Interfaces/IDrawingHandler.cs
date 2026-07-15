using System.Collections.Generic;

namespace Drawing.Search.Infrastructure.CAD.Tekla.Interfaces;

public interface IDrawingHandler
{
    IDrawing GetDrawing();
    void SelectResultsInDrawing<T>(List<T> results);
}