using Tekla.Structures.Drawing.UI;

namespace Drawing.Search.CADIntegration.Interfaces;

public interface IDrawingProvider
{
    Tekla.Structures.Drawing.Drawing GetActiveDrawing();
    DrawingObjectSelector GetSelector();
}