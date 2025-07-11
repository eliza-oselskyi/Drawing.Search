using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Drawing.Search.Common.Enums;
using Drawing.Search.Common.Interfaces;

namespace Drawing.Search.Common.SearchTypes;

/// <summary>
///     Represents the configuration for a search operation, including parameters such as search term, case sensitivity,
///     wildcard support, search strategies, and search type.
/// </summary>
/// <remarks>
///     This class provides a centralized way to define all the necessary configurations for a search
///     operation, including handling observables for notifying the client about matches.
/// </remarks>
/// <example>
///     Example usage:
///     <code>
/// var searchConfig = new SearchConfiguration
/// {
///     SearchTerm = "example",
///     CaseSensitive = true,
///     Wildcard = false,
///     Type = SearchType.Text,
///     SearchStrategies = new List&lt;ISearchStrategy&gt;
///     {
///         new RegexMatchStrategy&lt;Text&gt;()
///     },
///     Observer = new SearchResultObserver()
/// };
/// 
/// Console.WriteLine(searchConfig.ToString());
/// </code>
/// </example>
public class SearchConfiguration
{
    /// <summary>
    ///     Gets or sets the search term to be used in the search operation.
    /// </summary>
    public string SearchTerm { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the search is case-sensitive.
    /// </summary>
    public bool CaseSensitive { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether wildcards are enabled for the search term.
    /// </summary>
    public bool Wildcard { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether to show all assembly parts.
    /// </summary>
    public bool ShowAllAssemblyParts { get; set; }

    /// <summary>
    ///     Gets or sets the list of search strategies to be used during the search operation.
    /// </summary>
    public List<ISearchStrategy> SearchStrategies { get; set; } = new();

    /// <summary>
    ///     Gets or sets the type of the search operation.
    /// </summary>
    public SearchType Type { get; set; }

    /// <summary>
    ///     Gets the <see cref="StringComparison" /> value based on whether the search is case-sensitive.
    /// </summary>
    public StringComparison StringComparison =>
        CaseSensitive
            ? StringComparison.Ordinal
            : StringComparison.OrdinalIgnoreCase;

    /// <summary>
    ///     Gets or sets the observer that listens to the search operation result notifications.
    /// </summary>
    public IObserver Observer { get; set; }


    /// <summary>
    ///     Returns a string representation of the search configuration, containing all properties.
    /// </summary>
    /// <returns>A <see cref="string" /> describing the current search configuration.</returns>
    public override string ToString()
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine($"Search Term: {SearchTerm}");
        stringBuilder.AppendLine($"Case Sensitive: {CaseSensitive}");
        stringBuilder.AppendLine($"Wildcard: {Wildcard}");
        var stratList = SearchStrategies.Select(m => m.GetType().Name).ToList();
        var stratString = string.Join(", ", stratList);
        stringBuilder.AppendLine($"Search Strategies: {stratString}");
        stringBuilder.AppendLine($"Search Type: {Type}");
        return stringBuilder.ToString();
    }
}