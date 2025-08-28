using System.Threading.Tasks;
using Drawing.Search.Application.Features.Search;

namespace Drawing.Search.Application.Services.Interfaces;

public interface ISearchService
{
    Task<SearchResult> ExecuteSearchAsync(SearchConfiguration config);
}