namespace Drawing.Search.Application.Features.Search.Interfaces;

public interface ISearchExecutor
{
    SearchResult Execute(SearchConfiguration config, Tekla.Structures.Drawing.Drawing drawing);
}