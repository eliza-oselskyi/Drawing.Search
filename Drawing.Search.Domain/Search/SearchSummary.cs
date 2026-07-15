using System;
using System.Collections.Generic;

namespace Drawing.Search.Domain.Search;

public sealed record SearchSummary(
    int MatchCount,
    TimeSpan ElapsedTime,
    IReadOnlyList<string> MatchedContent);