namespace Drawing.Search.Core.Interfaces;

/// <summary>
///     Declares methods for matching strategies.
/// </summary>
/// <typeparam name="T">Type.</typeparam>
public interface ISearchStrategy
{
    bool Match(string obj, SearchQuery query);
}