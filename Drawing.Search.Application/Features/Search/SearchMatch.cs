namespace Drawing.Search.Application.Features.Search;

public sealed record SearchMatch<T>(T Item, string Content);