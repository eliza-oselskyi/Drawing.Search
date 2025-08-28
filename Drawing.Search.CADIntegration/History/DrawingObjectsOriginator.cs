using System;
using System.Collections.Generic;
using Drawing.Search.CADIntegration.TeklaWrappers;
using Tekla.Structures.Drawing;
using Tekla.Structures.DrawingInternal;

namespace Drawing.Search.CADIntegration.History;

public class DrawingObjectsOriginator
{
    private DrawingObjectEnumerator _objects;
    private List<TeklaView>? _views;

    public DrawingObjectsOriginator(Tekla.Structures.Drawing.Drawing drawing)
    {
        _objects = drawing.GetSheet().GetAllObjects();
        Objects = _objects;
        Drawing = drawing;
        PopulateViews();
    }

    public DrawingObjectEnumerator Objects { get; set; }
    public List<TeklaView>? Views { get; set; }
    public Tekla.Structures.Drawing.Drawing Drawing { get; set; }

    public void Save()
    {
        Console.WriteLine($"Saving objects to history... \nDrawing ID: {Drawing.GetIdentifier().ToString()}");
    }

    private void UpdateObjects()
    {
        _objects = Drawing.GetSheet().GetAllObjects();
        Objects = _objects;
    }

    private void PopulateViews()
    {
        var viewEnumerator = Drawing.GetSheet().GetAllViews();
        var views = new List<TeklaView>();
        if (views == null) throw new ArgumentNullException(nameof(views));

        foreach (View view in viewEnumerator)
        {
            var v = new TeklaView(view);
            views.Add(v);
        }

        _views = views;
        Views = _views;
    }

    public DrawingState CreateDrawingState()
    {
        UpdateObjects();
        PopulateViews();
        return new DrawingState(Objects, Views ?? throw new InvalidOperationException());
    }
}