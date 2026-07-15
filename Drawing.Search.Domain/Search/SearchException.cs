using System;

namespace Drawing.Search.Domain.Search;

public sealed class SearchException : Exception
{

    public SearchException(SearchError error, Exception innerException) : base(error.ToString(), innerException)
    {
        Error = error;
    }

    public SearchException(SearchError error) : base(error.ToString())
    {
        Error = error;
    }

    public SearchError Error { get; }
}