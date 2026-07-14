namespace Drawing.Search.Domain.Drawings;

public sealed record DrawingId(string Value)
{
    public override string ToString() => Value;
}