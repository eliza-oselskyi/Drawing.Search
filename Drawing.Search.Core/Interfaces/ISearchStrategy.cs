namespace Drawing.Search.Core.Interfaces;

public interface ISearchStrategy<T>
{
    bool Match(T obj, SearchQuery query);
    bool Match(string str, SearchQuery query);
}