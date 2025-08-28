using System;
using System.Collections.Generic;
using Drawing.Search.Domain.Interfaces;

namespace Drawing.Search.Domain.Observers;

/// <summary>
///     An observer that collects content matched during a search operation.
/// </summary>
/// <remarks>
///     This class uses an <see cref="IDataExtractor" /> to extract searchable content from matched objects.
///     The collected content is stored in a case-insensitive <see cref="HashSet{T}" />.
///     Note: This functionality is expected to be integrated into the
///     <see>
///         <cref>SearchService</cref>
///     </see>
///     singleton
///     in the future, removing the need for a separate observer class.
/// </remarks>
public class ContentCollectingObserver : IObserver
{
    private readonly IDataExtractor _extractor;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ContentCollectingObserver" /> class.
    /// </summary>
    /// <param name="extractor">The data extractor used for extracting searchable strings.</param>
    /// <exception cref="ArgumentNullException">Thrown if the provided <paramref name="extractor" /> is <c>null</c>.</exception>
    public ContentCollectingObserver(IDataExtractor extractor)
    {
        _extractor = extractor ?? throw new ArgumentNullException(nameof(extractor));
    }

    /// <summary>
    ///     Gets the collection of content matched during the search operation.
    /// </summary>
    /// <remarks>
    ///     This property stores all unique matched content in a case-insensitive manner.
    /// </remarks>
    public HashSet<string> MatchedContent { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///     Handles the event when a match is found.
    ///     Uses the <see cref="IDataExtractor" /> to extract searchable content from the provided object
    ///     and adds it to the <see cref="MatchedContent" /> collection.
    /// </summary>
    /// <param name="obj">The matched object. Expected to contain searchable information.</param>
    public void OnMatchFound(object obj)
    {
        var content = _extractor.ExtractSearchableString(obj);
        if (!string.IsNullOrWhiteSpace(content))
            MatchedContent.Add(content);

        // Uncomment the following line for debugging purposes
        // Console.WriteLine($"Observer Match Found: {obj}");
    }
}