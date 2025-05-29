using System;
using System.Diagnostics;

namespace Drawing.Search.Core;

public class SearchLogger : ISearchLogger
{
    public void LogInformation(string message)
    {
        Debug.WriteLine($"INFO: {message}");
        Console.WriteLine($"INFO: {message}");
    }

    public void LogError(Exception exception, string message)
    {
        Debug.WriteLine($"ERROR: {message}");
        Debug.WriteLine($"Exception: {exception}");
        Console.WriteLine($"ERROR: {message}");
        Console.WriteLine($"Exception: {exception}");
    }

    public void DebugInfo(string message)
    {
        Debug.WriteLine($"DEBUG: {message}");
    }
}