namespace Drawing.Search.Domain.Search;

public abstract record SearchError
{
    private SearchError() { }
    
    public sealed record EmptySearchTerm : SearchError;
    public sealed record NoActiveDrawing : SearchError;

    public sealed record UnsupportedSearchRequest(string RequestType) : SearchError;

    public sealed record InvalidRegex(string Pattern, string Message) : SearchError;

    public sealed record CacheUnavailable(string Message) : SearchError;

    public sealed record Unexpected(string Message) : SearchError;
}