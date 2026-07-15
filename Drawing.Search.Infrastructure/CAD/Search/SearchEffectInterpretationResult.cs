namespace Drawing.Search.Infrastructure.CAD.Search;

internal sealed record SearchEffectInterpretationResult(int SelectedObjectCount)
{
    public static SearchEffectInterpretationResult Empty { get; } = new(0);
}