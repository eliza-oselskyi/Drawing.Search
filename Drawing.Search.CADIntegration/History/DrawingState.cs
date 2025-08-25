using System.Collections.Generic;
using Tekla.Structures.Drawing;

namespace Drawing.Search.CADIntegration.History;

public class DrawingState(DrawingObjectEnumerator drawingObjects, List<TeklaWrappers.TeklaView> views)
{
    public DrawingObjectEnumerator DrawingObjects { get; internal set; } = drawingObjects;
    public List<TeklaWrappers.TeklaView> Views { get; internal set; } = views;
}