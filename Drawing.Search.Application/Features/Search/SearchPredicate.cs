using Drawing.Search.Domain.Interfaces;

namespace Drawing.Search.Application.Features.Search;

/// <summary>
/// Represents a predicate function that takes a candidate string and a search query and returns a boolean value.
/// </summary>
public delegate bool SearchPredicate(string candidate, SearchQuery query);