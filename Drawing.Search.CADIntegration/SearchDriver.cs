using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Drawing.Search.Caching;
using Drawing.Search.Caching.Interfaces;
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
    private readonly IObserver _cacheObserver;
    private readonly ICacheService _cacheService;
    private readonly DrawingHandler _drawingHandler;
    private readonly Events _events;
    private readonly object _lockObject = new();
    private readonly ISearchLogger _logger;
    private bool _cacheInvalidated = true;
    private string _currentDrawingId;

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
        CacheObserver = new CachingObserver(SynchronizationContext.Current);
        _cacheObserver = CacheObserver;

        if (!model.GetConnectionStatus())
            throw new ApplicationException("Tekla connection not established.");

        _cacheService.WriteAllObjectsInDrawingToCache(_drawingHandler.GetActiveDrawing());

        InitializeEvents();
    }

    /// <summary>
    ///     Gets the caching observer that tracks the caching process and updates its status as needed.
    /// </summary>
    public CachingObserver CacheObserver { get; }

    /// <summary>
    ///     Releases resources used by <see cref="SearchDriver" /> and unregisters event handlers.
    /// </summary>
    public void Dispose()
    {
        _events.DrawingChanged -= OnDrawingModified;
        _events.DrawingUpdated -= OnDrawingUpdated;
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
        if (drawing == null) throw new InvalidOperationException("No active drawing.");
        if (config == null) throw new ArgumentNullException();

        _logger.LogInformation($"Executing search with configuration: {config.ToString()}.");

        try
        {
            var result = config.Type switch
            {
                SearchType.PartMark => NewExecutePartMarkSearch(config, drawing),
                SearchType.Text => NewExecuteTextSearch(config, drawing),
                SearchType.Assembly => NewExecuteAssemblySearch(config),
                _ => throw new ArgumentException($"Unsupported search type: {config.Type}")
            };

            return result;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error executing search.");
            throw;
        }
    }

    /// <summary>
    ///     Invalidates the cached drawing objects and sets the cache state to require a refresh.
    /// </summary>
    private void InvalidateCache()
    {
        var dwgKey = new CacheKeyBuilder(DrawingHandler.Instance.GetActiveDrawing().GetIdentifier().ToString())
            .UseDrawingKey().AppendObjectId().Build();
        _cacheService.RefreshCache(dwgKey, DrawingHandler.Instance.GetActiveDrawing());

        // lock (_lockObject)
        // {
        //     _cache.Remove($"{_currentDrawingId}_{DRAWING_OBJECTS_CACHE_KEY}");
        //     _cacheInvalidated = true;
        //     _logger.LogInformation($"Drawing cache invalidated.");
        // }
    }

    /// <summary>
    ///     Initializes event subscriptions required to handle changes in drawings.
    /// </summary>
    private void InitializeEvents()
    {
        // TODO: Refresh cache only when deletion happens in the active drawing. Currently any modification refreshes cache
        //_events.DrawingChanged += OnDrawingModified;
        //_events.DrawingUpdated += OnDrawingUpdated;
        _events.Register();
    }

    /// <summary>
    ///     Handles the event when a drawing is updated in Tekla.
    /// </summary>
    /// <param name="drawing">The updated Tekla drawing.</param>
    /// <param name="type">The type of update performed on the drawing.</param>
    private void OnDrawingUpdated(Tekla.Structures.Drawing.Drawing drawing, Events.DrawingUpdateTypeEnum type)
    {
        InvalidateCache();
    }

    /// <summary>
    ///     Handles the event when a drawing is modified in Tekla.
    /// </summary>
    private void OnDrawingModified()
    {
        InvalidateCache();
    }

    /// <summary>
    /// Retrieves all objects from the specified drawing.
    /// </summary>
    /// <returns>A list of all <see cref="DrawingObject" /> objects in the drawing.</returns>
    private SearchResult NewExecuteAssemblySearch(SearchConfiguration config)
    {
        var activeDrawing = _drawingHandler.GetActiveDrawing();
        if (activeDrawing == null)
            throw new InvalidOperationException("No active drawing found.");

        var drawingKey = new CacheKeyBuilder(activeDrawing.GetIdentifier().ToString())
            .UseDrawingKey()
            .AppendObjectId()
            .Build();

        // Fetch all model object identifiers for the current drawing
        var cachedModelObjectIds = _cacheService.DumpIdentifiers(activeDrawing.GetIdentifier().ToString());

        // Fetch associated ModelObject instances
        var cachedModelObjects = cachedModelObjectIds
            .Select(id => _cacheService.GetFromCache(drawingKey, id) as ModelObject)
            .Where(obj => obj != null)
            .ToList();

        // Perform search using the search configuration on ModelObjects
        var searcher = CreateSearcher<ModelObject>(config);
        {
            var searchResults = searcher.Search(cachedModelObjects!, CreateSearchQuery(config)).ToList();

            // For each matched ModelObject, find associated Part objects
            var selectableParts = new List<Part>();
            foreach (var modelObject in searchResults)
            {
                var relatedDrawingObjects = _cacheService.GetRelatedObjects(activeDrawing.GetIdentifier().ToString(),
                    modelObject.Identifier.ToString());
                selectableParts.AddRange(relatedDrawingObjects.OfType<Part>());
            }

            // Select the associated Part objects in the drawing
            TeklaWrapper.DrawingObjectListToSelection(selectableParts.Cast<DrawingObject>().ToList(), activeDrawing);

            return new SearchResult
            {
                MatchCount = selectableParts.Count,
                ElapsedMilliseconds = 0, // To be measured by the caller
                SearchType = SearchType.Assembly
            };
        }
    }

    private SearchResult NewExecuteTextSearch(SearchConfiguration config, Tekla.Structures.Drawing.Drawing drawing)
    {
        var dwgKey = new CacheKeyBuilder(drawing.GetIdentifier().ToString())
            .UseDrawingKey()
            .AppendObjectId()
            .Build();

        var ids = _cacheService.DumpIdentifiers(drawing.GetIdentifier().ToString());

        var texts = ids.Where(t => _cacheService.GetFromCache(dwgKey, t) is Text)
            .Select(t => _cacheService.GetFromCache(dwgKey, t) as Text).ToList();
        var searcher = CreateSearcher<Text>(config);
        var contentCollector = new ContentCollectingObserver(new TextExtractor());
        searcher.Subscribe(contentCollector);

        Debug.Assert(texts != null, nameof(texts) + " != null");
        var results = searcher.Search(texts, CreateSearchQuery(config));

        var enumerable = results.ToList();
        SelectResults(enumerable.Cast<DrawingObject>().ToList());

        return new SearchResult()
        {
            MatchCount = enumerable.Count(),
            ElapsedMilliseconds = 0, // Set by caller
            SearchType = SearchType.Text
        };
    }

    private SearchResult NewExecutePartMarkSearch(SearchConfiguration config,
        Tekla.Structures.Drawing.Drawing drawing)
    {
        var dwgKey = new CacheKeyBuilder(drawing.GetIdentifier().ToString())
            .UseDrawingKey()
            .AppendObjectId()
            .Build();

        var ids = _cacheService.DumpIdentifiers(drawing.GetIdentifier().ToString());
        var marks = ids.Where(t => _cacheService.GetFromCache(dwgKey, t) is Mark)
            .Select(t => _cacheService.GetFromCache(dwgKey, t) as Mark).ToList();
        var searcher = CreateSearcher<Mark>(config);
        var contentCollector = new ContentCollectingObserver(new MarkExtractor());
        searcher.Subscribe(contentCollector);

        Debug.Assert(marks != null, nameof(marks) + " != null");
        var results = searcher.Search(marks, CreateSearchQuery(config));

        var enumerable = results.ToList();
        SelectResults(enumerable.Cast<DrawingObject>().ToList());

        return new SearchResult
        {
            MatchCount = enumerable.Count(),
            ElapsedMilliseconds = 0, // Set by caller
            SearchType = SearchType.PartMark
        };
    }

    private void SelectResults<T>(List<T> results) where T : class
    {
        var drawing = _drawingHandler.GetActiveDrawing();
        var selector = _drawingHandler.GetDrawingObjectSelector();
        selector.UnselectAllObjects();

        if (typeof(T) == typeof(ModelObject))
        {
            TeklaWrapper.ModelObjectListToSelection(results.Cast<ModelObject>().ToList(), drawing);
        }
        else
        {
            TeklaWrapper.DrawingObjectListToSelection(results.Cast<DrawingObject>().ToList(), drawing);
        }
    }

    private ISearchQuery CreateSearchQuery(SearchConfiguration config)
    {
        return new SearchQuery(config.SearchTerm)
        {
            CaseSensitive = config.StringComparison
        };
    }

    private ObservableSearch<T> CreateSearcher<T>(SearchConfiguration config)
    {
        var extractor = GetExtractor<T>();
        return new ObservableSearch<T>(config.SearchStrategies, extractor);
    }

    private IDataExtractor GetExtractor<T>()
    {
        return typeof(T) switch
        {
            { } t when t == typeof(Mark) => new MarkExtractor(),
            { } t when t == typeof(Text) => new TextExtractor(),
            { } t when t == typeof(ModelObject) => new ModelObjectExtractor(),
            _ => throw new ArgumentException($"No extractor available for type {typeof(T)}")
        };
    }
}