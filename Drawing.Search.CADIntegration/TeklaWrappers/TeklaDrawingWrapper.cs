using System.Collections.Generic;
using Drawing.Search.CADIntegration.Interfaces;
using Tekla.Structures.DrawingInternal;

namespace Drawing.Search.CADIntegration.TeklaWrappers;

public class TeklaDrawingWrapper : IDrawing
{
    public TeklaDrawingWrapper(Tekla.Structures.Drawing.Drawing drawing)
    {
        Drawing = drawing;
        Id = GetIdentifier();
    }

    public Tekla.Structures.Drawing.Drawing Drawing { get; set; }
    public string Id { get; private set; }

    public IEnumerable<object> GetAllObjects()
    {
        var enumerator = Drawing.GetSheet().GetAllObjects();
        while (enumerator.MoveNext())
            if (enumerator.Current is not null)
                yield return enumerator.Current;
    }

    private string GetIdentifier()
    {
        return Drawing.GetIdentifier().ToString();
    }
}