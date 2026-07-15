using System;
using System.Threading;
using System.Threading.Tasks;
using DotNext;
using Drawing.Search.Domain.Effects;
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

    public static async Task<Result<SearchPlanExecutionResult<TInterpretation>>> RunAsync<TInterpretation>(
        Result<SearchPlan> planResult,
        Func<SearchEffect, CancellationToken, Task<TInterpretation>> interpret,
        Func<TInterpretation, TInterpretation, TInterpretation> combine,
        TInterpretation empty,
        CancellationToken cancellationToken = default)
    {
        if (interpret is null) throw new ArgumentNullException(nameof(interpret));
        if (combine is null) throw new ArgumentNullException(nameof(combine));

        if (!planResult.IsSuccessful)
            return planResult.Error.ResultFromException<SearchPlanExecutionResult<TInterpretation>>();

        var plan = planResult.Value;
        var interpretation = empty;

        foreach (var effect in plan.Effects)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var effectResult = await interpret(effect, cancellationToken);
            interpretation = combine(interpretation, effectResult);
        }

        return new SearchPlanExecutionResult<TInterpretation>(
                plan,
                interpretation)
            .ResultFromValue();
    }
}