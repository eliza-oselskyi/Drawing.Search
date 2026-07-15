using Drawing.Search.Application.Features.Search;

namespace Drawing.Search.Infrastructure.CAD.Search;

public interface ISearchExecutor
{
    SearchResult Execute(SearchConfiguration config, global::Tekla.Structures.Drawing.Drawing drawing);
}