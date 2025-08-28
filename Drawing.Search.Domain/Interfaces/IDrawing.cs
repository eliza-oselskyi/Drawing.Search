using System.Collections.Generic;

namespace Drawing.Search.Domain.Interfaces;

public interface IDrawing
{
    IEnumerable<object> GetAllObjects();
}