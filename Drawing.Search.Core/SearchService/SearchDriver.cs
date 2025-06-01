using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Drawing.Search.Core.SearchService.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Tekla.Structures.Drawing;
using Tekla.Structures.DrawingInternal;
using Tekla.Structures.Model;
using Events = Tekla.Structures.Drawing.Events;
using ModelObject = Tekla.Structures.Model.ModelObject;

namespace Drawing.Search.Core.SearchService
{
    /// <summary>
    /// Handles the execution of search operations within Tekla drawings.
    /// Manages search state and executes different search operations based on the given configuration.
    /// </summary>
    /// <remarks>
    /// <strong>Note:</strong> This class will no longer handle caching. In the future, its role will 
    /// be strictly to execute searches using various configurations.
    /// </remarks>
    /// <example>
    /// Example usage:
    /// <code>
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
        private const string ASSEMBLY_CACHE_KEY = "assembly_objects";
        private const string DRAWING_OBJECTS_CACHE_KEY = "drawing_objects";
        private const string MATCHED_CONTENT_CACHE_KEY = "matched_content";
        private readonly IMemoryCache _cache;
        private readonly DrawingHandler _drawingHandler;
        private readonly Events _events;
        private readonly object _lockObject = new();
        private readonly ISearchLogger _logger;
        private readonly Model _model;
        private bool _cacheInvalidated = true;
        private readonly IObserver _cacheObserver;
        private string _currentDrawingId;
        private bool _isCaching;


    public SearchDriver(IMemoryCache cache, SynchronizationContext uiContext)
    {
        _drawingHandler = new DrawingHandler();
        _model = new Model();
        _events = new Events();
        _cache = cache;
        _logger = new SearchLogger();
        CacheObserver = new CachingObserver(SynchronizationContext.Current);
        _cacheObserver = CacheObserver;
        /// <summary>
        /// Initializes a new instance of the <see cref="SearchDriver"/> class.
        /// </summary>
        /// <param name="cache">The memory cache used for caching objects during searches.</param>
        /// <param name="uiContext">The synchronization context used for UI-related updates.</param>
        /// <exception cref="ApplicationException">Thrown when Tekla connection cannot be established.</exception>
        public SearchDriver(IMemoryCache cache, SynchronizationContext uiContext)
        {
            _drawingHandler = new DrawingHandler();
            _model = new Model();
            _events = new Events();
            _cache = cache;
            CacheObserver = new CachingObserver(SynchronizationContext.Current);
            _cacheObserver = CacheObserver;

            if (!_model.GetConnectionStatus())
                throw new ApplicationException("Tekla connection not established.");

            InitializeEvents();
        }

        /// <summary>
        /// Gets the caching observer that tracks the caching process and updates its status as needed.
        /// </summary>
        public CachingObserver CacheObserver { get; }

        /// <summary>
        /// Releases resources used by <see cref="SearchDriver"/> and unregisters event handlers.
        /// </summary>
        public void Dispose()
        {
            _events.DrawingChanged -= OnDrawingModified;
            _events.DrawingUpdated -= OnDrawingUpdated;
            _events.UnRegister();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Executes a search operation based on the given configuration.
        /// </summary>
        /// <param name="config">The configuration parameters for the search operation.</param>
        /// <returns>A <see cref="SearchResult"/> containing the results of the search operation.</returns>
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
                    SearchType.PartMark => ExecutePartMarkSearch(config, drawing),
                    SearchType.Text => ExecuteTextSearch(config, drawing),
                    SearchType.Assembly => ExecuteAssemblySearch(config),
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
        /// Invalidates the cached drawing objects and sets the cache state to require a refresh.
        /// </summary>
        private void InvalidateCache()
        {
            lock (_lockObject)
            {
                _cache.Remove($"{_currentDrawingId}_{DRAWING_OBJECTS_CACHE_KEY}");
                _cacheInvalidated = true;
                _logger.LogInformation($"Drawing cache invalidated.");
            }
        }

        /// <summary>
        /// Initializes event subscriptions required to handle changes in drawings.
        /// </summary>
        private void InitializeEvents()
        {
            _events.DrawingChanged += OnDrawingModified;
            _events.DrawingUpdated += OnDrawingUpdated;
            _events.Register();
        }

        /// <summary>
        /// Handles the event when a drawing is updated in Tekla.
        /// </summary>
        /// <param name="drawing">The updated Tekla drawing.</param>
        /// <param name="type">The type of update performed on the drawing.</param>
        private void OnDrawingUpdated(Tekla.Structures.Drawing.Drawing drawing, Events.DrawingUpdateTypeEnum type)
        {
            InvalidateCache();
        }

        /// <summary>
        /// Handles the event when a drawing is modified in Tekla.
        /// </summary>
        private void OnDrawingModified()
        {
            InvalidateCache();
        }

        /// <summary>
        /// Fetches all drawing objects from the active drawing and caches them.
        /// </summary>
        /// <param name="drawing">The drawing to fetch objects from.</param>
        private void RefreshCache(Tekla.Structures.Drawing.Drawing drawing)
        {
            lock (_lockObject)
            {
                _isCaching = true;
                _cacheObserver.OnMatchFound(_isCaching);

                if (_cacheInvalidated)
                {
                    var stopwatch = Stopwatch.StartNew();

                    _logger.LogInformation("Refreshing drawing objects cache...");
                    var objects = FetchAllDrawingObjects(drawing);
                    var drawingId = drawing.GetIdentifier().ToString();
                    var cacheKey = $"{drawingId}_{DRAWING_OBJECTS_CACHE_KEY}";

                    _cache.Set(cacheKey, objects, TimeSpan.FromMinutes(15));
                    _currentDrawingId = drawingId;
                    _cacheInvalidated = false;

                    stopwatch.Stop();
                    _logger.LogInformation($"Drawing objects cache refreshed with {objects.Count} objects.");
                    _logger.DebugInfo($"Cache refreshed in {stopwatch.ElapsedMilliseconds} ms.");
                }

                _isCaching = false;
                _cacheObserver.OnMatchFound(_isCaching);
            }
        }

        /// <summary>
        /// Retrieves all objects from the specified drawing.
        /// </summary>
        /// <param name="drawing">The Tekla drawing to search in.</param>
        /// <returns>A list of all <see cref="DrawingObject"/> objects in the drawing.</returns>
        private List<DrawingObject> FetchAllDrawingObjects(Tekla.Structures.Drawing.Drawing drawing)
        {
            var list = new List<DrawingObject>();
            var enumerator = drawing.GetSheet().GetAllObjects();
            while (enumerator.MoveNext())
                if (enumerator.Current is not null)
                    list.Add(enumerator.Current);

            return list;
        }

        private SearchResult ExecuteAssemblySearch(SearchConfiguration config)
        {
            var modelObjects = GetCachedAssemblyObjects();

            var searcher = CreateSearcher<ModelObject>(config);
            var contentCollector = new ContentCollectingObserver(new ModelObjectExtractor());
            searcher.Subscribe(contentCollector);

            var results = searcher.Search(modelObjects, CreateSearchQuery(config));

            SelectResults(results.Cast<ModelObject>().ToList());
            foreach (var content in contentCollector.MatchedContent)
                _cache.GetOrCreate(MATCHED_CONTENT_CACHE_KEY,
                    entry => new HashSet<string>(StringComparer.OrdinalIgnoreCase)).Add(content);

            return new SearchResult()
            {
                MatchCount = results.Count(),
                ElapsedMilliseconds = 0, // Set by caller
                SearchType = SearchType.Assembly
            };
        }

        private List<ModelObject> GetCachedAssemblyObjects()
        {
            // always check if cache needs refresh
            if (_cacheInvalidated ||
                !_cache.TryGetValue($"{_currentDrawingId}_{DRAWING_OBJECTS_CACHE_KEY}",
                    out List<DrawingObject> _))
                RefreshCache(_drawingHandler.GetActiveDrawing());
            return _cache.GetOrCreate(ASSEMBLY_CACHE_KEY, entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromMinutes(15);
                lock (_lockObject)
                {
                    return GetModelObjects();
                }
            });
        }

        private SearchResult ExecuteTextSearch(SearchConfiguration config, Tekla.Structures.Drawing.Drawing drawing)
        {
            var texts = GetFilteredObjects<Text>(drawing);

            var searcher = CreateSearcher<Text>(config);
            var contentCollector = new ContentCollectingObserver(new TextExtractor());
            searcher.Subscribe(contentCollector);

            var results = searcher.Search(texts, CreateSearchQuery(config));

            SelectResults(results.Cast<DrawingObject>().ToList());

            foreach (var content in contentCollector.MatchedContent)
                _cache.GetOrCreate(MATCHED_CONTENT_CACHE_KEY,
                    entry => new HashSet<string>(StringComparer.OrdinalIgnoreCase)).Add(content);

            return new SearchResult()
            {
                MatchCount = results.Count(),
                ElapsedMilliseconds = 0, // Set by caller
                SearchType = SearchType.Text
            };
        }

        private SearchResult ExecutePartMarkSearch(SearchConfiguration config, Tekla.Structures.Drawing.Drawing drawing)
        {
            var marks = GetFilteredObjects<Mark>(drawing);
            var searcher = CreateSearcher<Mark>(config);
            var contentCollector = new ContentCollectingObserver(new MarkExtractor());
            searcher.Subscribe(contentCollector);

            var results = searcher.Search(marks, CreateSearchQuery(config)).ToList();

            SelectResults(results.Cast<DrawingObject>().ToList());

            foreach (var content in contentCollector.MatchedContent)
                _cache.GetOrCreate(MATCHED_CONTENT_CACHE_KEY,
                    entry => new HashSet<string>(StringComparer.OrdinalIgnoreCase)).Add(content);

            return new SearchResult
            {
                MatchCount = results.Count,
                ElapsedMilliseconds = 0, // Set by caller
                SearchType = SearchType.PartMark
            };
        }

        private IEnumerable<T> GetFilteredObjects<T>(Tekla.Structures.Drawing.Drawing drawing) where T : DrawingObject
        {
            // always check if cache needs refresh
            if (_cacheInvalidated ||
                !_cache.TryGetValue($"{_currentDrawingId}_{DRAWING_OBJECTS_CACHE_KEY}",
                    out List<DrawingObject> _))
                RefreshCache(drawing);

            var cacheKey = $"{drawing.GetIdentifier()}_{typeof(T).Name}_filtered";
            return _cache.GetOrCreate(cacheKey, entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromMinutes(15);
                _logger.LogInformation($"Fetching {typeof(T).Name} objects from drawing...");

                var objects = new List<T>();
                var enumerator = drawing.GetSheet().GetAllObjects();
                while (enumerator.MoveNext())
                    if (enumerator.Current is T typedObject)
                        objects.Add(typedObject);

                _logger.LogInformation($"Fetched and cached {objects.Count} {typeof(T).Name} objects from drawing.");
                return objects;
            });
        }

        private List<T> GetCachedDrawingObjects<T>(string cacheKey)
        {
            var drawing = _drawingHandler.GetActiveDrawing();
            var drawingId = drawing.GetIdentifier().ToString();
            var cacheKeyWithDrawing = $"{drawingId}_{cacheKey}";

            lock (_lockObject)
            {
                _currentDrawingId = drawingId;
            }

            return _cache.Get<List<T>>(cacheKeyWithDrawing) ??
                   throw new InvalidOperationException("Cache unexpectedly empty. This should not happen.");
        }

        private List<ModelObject> GetModelObjects()
        {
            var drawing = _drawingHandler.GetActiveDrawing();
            var identifiers = _drawingHandler.GetModelObjectIdentifiers(drawing);
            return _model.FetchModelObjects(identifiers, false);
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
                ;
            }
        }

        private SearchQuery CreateSearchQuery(SearchConfiguration config)
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
                Type t when t == typeof(Mark) => new MarkExtractor(),
                Type t when t == typeof(Text) => new TextExtractor(),
                Type t when t == typeof(ModelObject) => new ModelObjectExtractor(),
                _ => throw new ArgumentException($"No extractor available for type {typeof(T)}")
            };
        }
    }
}