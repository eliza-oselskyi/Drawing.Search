using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Drawing.Search.Caching;
using Drawing.Search.Caching.Interfaces;
using Drawing.Search.CADIntegration.Interfaces;
using Drawing.Search.CADIntegration.Strategies;
using Drawing.Search.CADIntegration.TeklaWrappers;
using Drawing.Search.Common.Enums;
using Drawing.Search.Common.Interfaces;
using Drawing.Search.Common.Observers;
using Drawing.Search.Common.SearchTypes;
using Drawing.Search.Searching;
using Tekla.Structures.Drawing;
using Tekla.Structures.DrawingInternal;
using Tekla.Structures.Model;
using Events = Tekla.Structures.Drawing.Events;
using ModelObject = Tekla.Structures.Model.ModelObject;
using Part = Tekla.Structures.Drawing.Part;

namespace Drawing.Search.CADIntegration;

/// <summary>
///     Handles the execution of search operations within Tekla drawings.
///     Manages search state and executes different search operations based on the given configuration.
/// </summary>
/// <remarks>
///     <strong>Note:</strong> This class will no longer handle caching. In the future, its role will
///     be strictly to execute searches using various configurations.
/// </remarks>
/// <example>
///     Example usage:
///     <code>
/// var searchDriver = new SearchDriver(memoryCache, SynchronizationContext.Current);
/// var config = new SearchConfiguration
/// {
///     SearchTerm = "example",
///     CaseSensitive = true,
///     Wildcard = false,
///     Type = SearchType.PartMark,
///     SearchStrategies = new List&lt;ISearchStrategy&gt; { new RegexMatchStrategy&lt;Mark&gt;() },
///     Observer = new SearchResultObserver()
/// };
/// var result = searchDriver.ExecuteSearch(config);
/// Console.WriteLine($"Matches: {result.MatchCount}");
/// </code>
/// </example>
public class SearchDriver : IDisposable
{
    private readonly ICacheService _cacheService;
    private readonly DrawingHandler _drawingHandler;
    private readonly Events _events;
    private readonly ISearchLogger _logger;
    private readonly Dictionary<SearchType, ISearchExecutor> _searchExecutors;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SearchDriver" /> class.
    /// </summary>
    /// <param name="cacheService">The cache service to inject</param>
    /// <param name="uiContext">The synchronization context used for UI-related updates.</param>
    /// <param name="logger">The logger to inject</param>
    /// <exception cref="ApplicationException">Thrown when Tekla connection cannot be established.</exception>
    public SearchDriver(ICacheService cacheService, SynchronizationContext uiContext, ISearchLogger logger)
    {
        _drawingHandler = new DrawingHandler();
        var model = new Model();
        _events = new Events();
        _cacheService = cacheService;
        _logger = logger;

        if (!model.GetConnectionStatus())
            throw new ApplicationException("Tekla connection not established.");

        var resultSelector = new DrawingResultSelector();
        _searchExecutors = new Dictionary<SearchType, ISearchExecutor>
        {
            { SearchType.PartMark, new PartMarkSearchExecutor(cacheService, resultSelector) },
            { SearchType.Text, new TextSearchExecutor(cacheService, resultSelector) },
            { SearchType.Assembly, new AssemblySearchExecutor(cacheService, resultSelector) }
        };
    }

    /// <summary>
    ///     Releases resources used by <see cref="SearchDriver" /> and unregisters event handlers.
    /// </summary>
    public void Dispose()
    {
        _events.UnRegister();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     Executes a search operation based on the given configuration.
    /// </summary>
    /// <param name="config">The configuration parameters for the search operation.</param>
    /// <returns>A <see cref="SearchResult" /> containing the results of the search operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the active drawing is not available.</exception>
    /// <exception cref="ArgumentNullException">Thrown if the provided configuration is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown if an unsupported search type is provided.</exception>
    public SearchResult ExecuteSearch(SearchConfiguration config)
    {
        var drawing = _drawingHandler.GetActiveDrawing();
        if (drawing == null)
            throw new InvalidOperationException("No active drawing found.");
        if (config == null) throw new ArgumentNullException(nameof(config));
        
        _logger.LogInformation($"Executing search with configuration: {config.ToString()}");

        try
        {
            if (!_searchExecutors.TryGetValue(config.Type, out var executor))
                throw new ArgumentException($"Unsupported search type: {config.Type}");
            
            return executor.Execute(config, drawing);
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Error executing search.");
            throw;
        }
    }
}