using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Tekla.Structures.DrawingInternal;

namespace Drawing.Search.CADIntegration.History;

public class DrawingHistory
{
    private readonly Stack<DrawingState> _drawingStates = new();
    public DrawingState? Current => _drawingStates.Count > 0 ? _drawingStates.Peek() : null;
    public bool HasDifference;
    public bool ViewHasDifference;

    public void Save(DrawingState drawingState)
    {
        if (_drawingStates.Count <= 0)
        {
            _drawingStates.Push(drawingState);
        }
        else if (ViewSizesDifferent(drawingState) || CountDifferent(drawingState))
        {
            _drawingStates.Push(drawingState);
            Console.WriteLine($"Drawing object state saved. Count: {_drawingStates.Count}");
            Console.WriteLine($"Amount of objects: {drawingState.DrawingObjects.GetSize()}");
        }
        else
        {
            Console.WriteLine($"No drawing count changes found.");
        }
    }

    private bool ViewSizesDifferent(DrawingState drawingState)
    {
        var currViews = drawingState.Views;
        var prevViews = _drawingStates.Peek().Views;
        
        if ((currViews.Count == 0) && (prevViews.Count == 0)) return false;
        if (currViews.Count != prevViews.Count) return true;
        if ((currViews.Count == 1) && (prevViews.Count == 1))
        {
            var currDimensions = currViews.First().GetDimensions();
            var prevDimensions = prevViews.First().GetDimensions();
            ViewHasDifference =  Math.Abs(currDimensions.Item1 - prevDimensions.Item1) > 0.0001 ||
                   Math.Abs(currDimensions.Item2 - prevDimensions.Item2) > 0.0001;

            return ViewHasDifference;
        }
        
        ViewHasDifference = currViews.FindAll((v) =>
        {
            var p = prevViews.First(p => p.View.GetIdentifier().GUID == v.View.GetIdentifier().GUID);

            var currDimensions = v.GetDimensions();
            var prevDimensions = p.GetDimensions();

            return Math.Abs(currDimensions.Item1 - prevDimensions.Item1) > 0.0001 ||
                   Math.Abs(currDimensions.Item2 - prevDimensions.Item2) > 0.0001;
        }).Count > 0;
        
        return ViewHasDifference;
    }

    private bool CountDifferent(DrawingState drawingState)
    {
        var currSize = drawingState.DrawingObjects.GetSize();
        var prevSize = _drawingStates.Peek().DrawingObjects.GetSize();
        var difference = Math.Abs(currSize - prevSize);
        Console.WriteLine($"Count difference: {difference}");
        HasDifference = difference > 0;
        return difference > 0;
    }
}