using System;
using System.Collections.Generic;
using Drawing.Search.Core.Interfaces;

namespace Drawing.Search.Core;

public class ContentCollectingObserver : IObserver
{
    private readonly IDataExtractor _extractor;

    public ContentCollectingObserver(IDataExtractor extractor)
    {
        _extractor = extractor;
    }

    public HashSet<string> MatchedContent { get; } = new(StringComparer.OrdinalIgnoreCase);

    public void OnMatchFound(object obj)
    {
        var content = _extractor.ExtractSearchableString(obj);
        if (!string.IsNullOrWhiteSpace(content))
            MatchedContent.Add(content);
        //Console.WriteLine($"Observer Match Found: {obj}")
    }
}