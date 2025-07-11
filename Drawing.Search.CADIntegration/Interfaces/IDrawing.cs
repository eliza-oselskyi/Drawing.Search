using System.Collections.Generic;

namespace Drawing.Search.CADIntegration.Interfaces;

public interface IDrawing
{
    IEnumerable<object> GetAllObjects();
}