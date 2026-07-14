using System.Collections.Generic;
using Drawing.Search.Domain.Effects;

namespace Drawing.Search.Domain.Search;

public sealed record SearchPlan(
    SearchSummary Summary,
    IReadOnlyList<SearchEffect> Effects);