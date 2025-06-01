using System.Collections.Generic;
using Drawing.Search.CADIntegration.Interfaces;

namespace Drawing.Search.Core.CADIntegrationService.Interfaces;

public interface IDrawingHandler
{
    IDrawing GetDrawing();
    void SelectResultsInDrawing<T>(List<T> results);
}