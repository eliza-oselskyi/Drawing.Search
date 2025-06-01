using Drawing.Search.Core.SearchService.Interfaces;

namespace Drawing.Search.Core;

/// <summary>
///     An observer class that listens to an IObservableState object
/// </summary>
public class SearchResultObserver : IObserver
{
    public int Matches { get; private set; }

    public void OnMatchFound(object obj)
    {
        Matches++;
        //Console.WriteLine($"Observer Match Found: {obj}");
    }
}