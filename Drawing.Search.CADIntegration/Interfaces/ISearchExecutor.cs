using Drawing.Search.Common.SearchTypes;

namespace Drawing.Search.CADIntegration.Interfaces;

public interface ISearchExecutor
{
    SearchResult Execute(SearchConfiguration config, Tekla.Structures.Drawing.Drawing drawing);
}