namespace Drawing.Search.Core.SearchService.Interfaces;

/// <summary>
///     Declares methods for matching strategies.
/// </summary>
/// <typeparam name="T">Type.</typeparam>
public interface ISearchStrategy
{
    bool Match(string obj, SearchQuery query);
}