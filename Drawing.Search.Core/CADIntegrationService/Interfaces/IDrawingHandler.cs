using System.Collections.Generic;

namespace Drawing.Search.Core.CADIntegrationService.Interfaces;

public interface IDrawingHandler
{
    IDrawing GetDrawing();
    void SelectResultsInDrawing<T>(List<T> results);
}