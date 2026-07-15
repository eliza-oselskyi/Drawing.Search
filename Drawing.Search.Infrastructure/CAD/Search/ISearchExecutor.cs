using System.Threading;
using System.Threading.Tasks;
using Drawing.Search.Application.Features.Search;

namespace Drawing.Search.Infrastructure.CAD.Search;

public interface ISearchExecutor
{
    Task<SearchResult> ExecuteAsync(SearchConfiguration config, global::Tekla.Structures.Drawing.Drawing drawing, CancellationToken cancellationToken = default);
}