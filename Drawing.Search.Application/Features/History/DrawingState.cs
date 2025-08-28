using System.Collections.Generic;
using Drawing.Search.Infrastructure.CAD.Tekla;
using Tekla.Structures.Drawing;

namespace Drawing.Search.Application.Features.History;

public class DrawingState(DrawingObjectEnumerator drawingObjects, List<TeklaView> views)
{
    public DrawingObjectEnumerator DrawingObjects { get; internal set; } = drawingObjects;
    public List<TeklaView> Views { get; internal set; } = views;
}