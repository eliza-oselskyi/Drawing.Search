using System;
using System.Collections.Generic;

namespace Drawing.Search.CADIntegration.History;

public class DrawingHistory
{
    private readonly Stack<DrawingState> _drawingStates = new();
    public DrawingState? Current => _drawingStates.Count > 0 ? _drawingStates.Peek() : null;
    public bool HasDifference = false;

    public void Save(DrawingState drawingState)
    {
        if (_drawingStates.Count <= 0)
        {
            _drawingStates.Push(drawingState);
        }
        else if (CountDifferent(drawingState))
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