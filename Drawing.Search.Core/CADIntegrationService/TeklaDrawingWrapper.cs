using System.Collections.Generic;
using Drawing.Search.Core.CADIntegrationService.Interfaces;
using Tekla.Structures.DrawingInternal;

namespace Drawing.Search.Core.CADIntegrationService;


public class TeklaDrawingWrapper : IDrawing
{
    public Tekla.Structures.Drawing.Drawing Drawing { get; set; }
    public string ID { get; private set; }

    public TeklaDrawingWrapper(Tekla.Structures.Drawing.Drawing drawing)
    {
        Drawing = drawing;
        ID = GetIdentifier();
    }

    private string GetIdentifier()
    {
        return Drawing.GetIdentifier().ToString();
    }

    public IEnumerable<object> GetAllObjects()
    {
        var enumerator = Drawing.GetSheet().GetAllObjects();
        while (enumerator.MoveNext())
        {
            if (enumerator.Current is not null)
            {
                yield return enumerator.Current;
            }   
        }
    }
}