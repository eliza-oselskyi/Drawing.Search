using Tekla.Structures.Drawing;

namespace Drawing.Search.CADIntegration.History;

public class DrawingState(DrawingObjectEnumerator drawingObjects)
{
    public DrawingObjectEnumerator DrawingObjects { get; private set; } = drawingObjects;
}