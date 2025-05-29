namespace Drawing.Search.Core;

public class SearchResult
{
    public int MatchCount { get; set; }
    public long ElapsedMilliseconds { get; set; }
    public SearchType SearchType { get; set; }
}