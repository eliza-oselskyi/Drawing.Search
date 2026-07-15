namespace Drawing.Search.Infrastructure.CAD.Search;

internal sealed record SearchEffectInterpretationResult(int SelectedObjectCount)
{
    public static SearchEffectInterpretationResult Empty { get; } = new(0);

    public static SearchEffectInterpretationResult Combine(SearchEffectInterpretationResult left, SearchEffectInterpretationResult right) =>
        new(left.SelectedObjectCount + right.SelectedObjectCount);
}