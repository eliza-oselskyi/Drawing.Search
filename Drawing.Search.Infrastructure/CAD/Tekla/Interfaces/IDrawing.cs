using System.Collections.Generic;

namespace Drawing.Search.Infrastructure.CAD.Tekla.Interfaces;

public interface IDrawing
{
    IEnumerable<object> GetAllObjects();
}