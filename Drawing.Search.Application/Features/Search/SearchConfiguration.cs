using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Drawing.Search.Domain.Enums;
using Drawing.Search.Domain.Interfaces;
using Drawing.Search.Domain.Search;

namespace Drawing.Search.Application.Features.Search;

/// <summary>
/// Describes a configuration for a search operation.
/// </summary>
public sealed record SearchConfiguration(
    string SearchTerm,
    bool CaseSensitive,
    bool Wildcard,
    bool ShowAllAssemblyParts,
    SearchType Type)
{
    /// <summary>
    ///     Gets the <see cref="StringComparison" /> value based on whether the search is case-sensitive.
    /// </summary>
    public StringComparison StringComparison =>
        CaseSensitive
            ? StringComparison.Ordinal
            : StringComparison.OrdinalIgnoreCase;

    public SearchRequest ToSearchRequest()
    {
        var useRegex = !Wildcard;

        return Type switch
        {
            SearchType.Text => new SearchRequest.Text(SearchTerm, useRegex, CaseSensitive),
            SearchType.PartMark => new SearchRequest.PartMark(SearchTerm, useRegex, CaseSensitive),
            SearchType.Assembly => new SearchRequest.Assembly(SearchTerm, useRegex, CaseSensitive, ShowAllAssemblyParts),
            _ => throw new ArgumentOutOfRangeException(nameof(Type), Type, "Unsupported search type.")
        };
    }

    /// <summary>
    /// Returns a string representation of the search configuration, containing all properties.
    /// </summary>
    /// <returns>A <see cref="string" /> describing the current search configuration.</returns>
    public override string ToString()
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine($"Search Term: {SearchTerm}");
        stringBuilder.AppendLine($"Case Sensitive: {CaseSensitive}");
        stringBuilder.AppendLine($"Wildcard: {Wildcard}");
        stringBuilder.AppendLine($"Search Type: {Type}");
        stringBuilder.AppendLine($"Show All Assembly Parts: {ShowAllAssemblyParts}");
        return stringBuilder.ToString();
    }
}