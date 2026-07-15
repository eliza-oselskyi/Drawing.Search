using Tekla.Structures.Drawing.UI;

namespace Drawing.Search.Infrastructure.CAD.Tekla.Interfaces;

public interface IDrawingProvider
{
    global::Tekla.Structures.Drawing.Drawing GetActiveDrawing();
    DrawingObjectSelector GetSelector();
}