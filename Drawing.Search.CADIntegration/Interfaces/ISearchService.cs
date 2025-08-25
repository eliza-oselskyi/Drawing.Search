using System.Threading.Tasks;
using Drawing.Search.Common.SearchTypes;

namespace Drawing.Search.CADIntegration.Interfaces;

public interface ISearchService
{
    Task<SearchResult> ExecuteSearchAsync(SearchConfiguration config);
}