using System.Collections.Generic;
using Drawing.Search.CADIntegration.TeklaWrappers;
using Tekla.Structures.Drawing;

namespace Drawing.Search.CADIntegration.History;

public class DrawingState(DrawingObjectEnumerator drawingObjects, List<TeklaView> views)
{
    public DrawingObjectEnumerator DrawingObjects { get; internal set; } = drawingObjects;
    public List<TeklaView> Views { get; internal set; } = views;
}