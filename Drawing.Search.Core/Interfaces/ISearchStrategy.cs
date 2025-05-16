namespace Drawing.Search.Core.Interfaces;

/// <summary>
/// Declares methods for matching strategies.
/// </summary>
/// <typeparam name="T">Type.</typeparam>
public interface ISearchStrategy<T>
{
    bool Match(T obj, SearchQuery query);
    bool Match(string str, SearchQuery query);
}