using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Drawing.Search.Application.Effects;
using Drawing.Search.Application.Features.Search;
using Drawing.Search.Domain.Drawings;
using Drawing.Search.Domain.Effects;
using Drawing.Search.Domain.Enums;
using Drawing.Search.Domain.Search;

namespace Drawing.Search.Infrastructure.CAD.Search;

internal static class TeklaSearchExecutorCore
{
    public static async Task<SearchResult> ExecuteAsync(
        IReadOnlyList<SearchableDrawingObject> searchableObjects,
        SearchConfiguration config,
        SearchType searchType,
        Func<SearchEffect, CancellationToken, Task<SearchEffectInterpretationResult>> interpret,
        Func<SearchPlanExecutionResult<SearchEffectInterpretationResult>, int> countMatches,
        CancellationToken cancellationToken)
    {
        var planResult = SearchPipeline.TryPlanSearch(
            searchableObjects,
            config.ToSearchRequest());

        var executionResult = await SearchPlanRunner.RunAsync(
            planResult,
            interpret,
            SearchEffectInterpretationResult.Combine,
            SearchEffectInterpretationResult.Empty,
            cancellationToken);

        if (!executionResult.IsSuccessful)
            throw executionResult.Error;

        var execution = executionResult.Value;

        return SearchResult.Empty with
        {
            MatchCount = countMatches(execution),
            ElapsedTime = execution.Plan.Summary.ElapsedTime,
            SearchType = searchType,
            MatchedContent = execution.Plan.Summary.MatchedContent
        };
    }
}
