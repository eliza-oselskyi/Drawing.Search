using System;
using System.Threading;
using System.Threading.Tasks;
using DotNext;
using Drawing.Search.Domain.Search;

namespace Drawing.Search.Application.Effects;

public static class SearchPlanRunner
{
    public static async Task<Result<SearchPlan>> RunAsync(
        Result<SearchPlan> planResult,
        SearchEffectInterpreter interpret,
        CancellationToken cancellationToken = default)
    {
        if (interpret is null) throw new ArgumentNullException(nameof(interpret));

        if (!planResult.IsSuccessful) return planResult;

        var plan = planResult.Value;

        foreach (var effect in plan.Effects)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await interpret(effect, cancellationToken);
        }

        return plan.ResultFromValue();
    }
}