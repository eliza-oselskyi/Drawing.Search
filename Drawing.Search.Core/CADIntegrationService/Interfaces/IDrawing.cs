using System.Collections.Generic;

namespace Drawing.Search.Core.CADIntegrationService.Interfaces;

public interface IDrawing
{
    IEnumerable<object> GetAllObjects();
}