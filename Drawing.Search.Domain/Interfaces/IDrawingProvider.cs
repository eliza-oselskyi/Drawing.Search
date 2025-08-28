using Tekla.Structures.Drawing.UI;

namespace Drawing.Search.Domain.Interfaces;

public interface IDrawingProvider
{
    Tekla.Structures.Drawing.Drawing GetActiveDrawing();
    DrawingObjectSelector GetSelector();
}