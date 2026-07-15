using Drawing.Search.Domain.Search;

namespace Drawing.Search.Application.Effects;

public sealed record SearchPlanExecutionResult<TInterpretation>(SearchPlan Plan, TInterpretation Interpretation);