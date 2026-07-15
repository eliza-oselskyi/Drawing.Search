using System.Threading;
using System.Threading.Tasks;
using Drawing.Search.Domain.Effects;

namespace Drawing.Search.Application.Effects;

public delegate Task SearchEffectInterpreter(SearchEffect effect, CancellationToken cancellationToken);