using System;
using Tekla.Structures.Drawing;
using Tekla.Structures.DrawingInternal;

namespace Drawing.Search.CADIntegration.History;

public class DrawingObjectsOriginator
{
    private DrawingObjectEnumerator _objects;
    public DrawingObjectEnumerator Objects { get; set; }
    public Tekla.Structures.Drawing.Drawing Drawing { get; set; }

    public DrawingObjectsOriginator(Tekla.Structures.Drawing.Drawing drawing)
    {
        _objects = drawing.GetSheet().GetAllObjects();
        Objects = _objects;
        Drawing = drawing;
    }

    public void Save()
    {
        Console.WriteLine($"Saving objects to history... \nDrawing ID: {Drawing.GetIdentifier().ToString()}");
    }

    private void UpdateObjects()
    {
        _objects = Drawing.GetSheet().GetAllObjects();
        Objects = _objects;
    }

    public DrawingState CreateDrawingState()
    {
        UpdateObjects();
        return new DrawingState(Objects);
    }
}