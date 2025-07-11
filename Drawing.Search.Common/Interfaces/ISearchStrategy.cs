namespace Drawing.Search.Common.Interfaces;

/// <summary>
///     Declares methods for matching strategies.
/// </summary>
public interface ISearchStrategy
{
    bool Match(string obj, ISearchQuery query);
}