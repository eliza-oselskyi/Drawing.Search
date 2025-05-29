using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Drawing.Search.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Tekla.Structures.Drawing;
using Tekla.Structures.DrawingInternal;
using Tekla.Structures.Model;
using Events = Tekla.Structures.Drawing.Events;
using MemoryCache = System.Runtime.Caching.MemoryCache;
using ModelObject = Tekla.Structures.Model.ModelObject;
using System.Runtime.Caching;
using Microsoft.Extensions.Caching.Memory;

namespace Drawing.Search.Core;

public class SearchDriver : IDisposable
{
    private readonly DrawingHandler _drawingHandler;
    private readonly Model _model;
    private readonly Events _events;
    private readonly IMemoryCache _cache;
    private readonly ISearchLogger _logger;
    private string _currentDrawingId;
     const string ASSEMBLY_CACHE_KEY = "assembly_objects";
    private const string DRAWING_OBJECTS_CACHE_KEY = "drawing_objects";
    private const string MATCHED_CONTENT_CACHE_KEY = "matched_content";
    private bool _cacheInvalidated = true; // track if cache needs refresh
    private readonly object _lockObject = new object();

    public SearchDriver(IMemoryCache cache)
    {
        _drawingHandler = new DrawingHandler();
        _model = new Model();
        _events = new Events();
        _cache = cache;
        _logger = new SearchLogger();

        if (!_model.GetConnectionStatus())
        {
            throw new ApplicationException("Tekla connection not established.");
        }

        InitializeEvents();
    }

    private void InitializeEvents()
    {
        _events.DrawingChanged += OnDrawingModified;
        _events.DrawingUpdated += OnDrawingUpdated;
        _events.Register();
    }

    private void OnDrawingUpdated(Tekla.Structures.Drawing.Drawing drawing, Events.DrawingUpdateTypeEnum type)
    {
        InvalidateCache();
    }

    private void OnDrawingModified()
    {
        InvalidateCache();
    }

    private void InvalidateCache()
    {
        lock (_lockObject)
        {
            _cache.Remove($"{_currentDrawingId}_{DRAWING_OBJECTS_CACHE_KEY}");
            _cacheInvalidated = true;
            _logger.LogInformation($"Drawing cache invalidated.");
        }
    }

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

    private SearchResult ExecuteAssemblySearch(SearchConfiguration config)
    {
        var modelObjects = GetCachedAssemblyObjects();
        
        var searcher = CreateSearcher<ModelObject>(config);
        var contentCollector = new ContentCollectingObserver(new ModelObjectExtractor());
        searcher.Subscribe(contentCollector);
        
        var results = searcher.Search(modelObjects, CreateSearchQuery(config));
        
        SelectResults(results.Cast<ModelObject>().ToList());
        foreach (var content in contentCollector.MatchedContent)
        {
            _cache.GetOrCreate(MATCHED_CONTENT_CACHE_KEY, entry => new HashSet<string>(StringComparer.OrdinalIgnoreCase)).Add(content);
        }
        
        return new SearchResult()
        {
            MatchCount = results.Count(),
            ElapsedMilliseconds = 0, // Set by caller
            SearchType = SearchType.Assembly
        };
    }

    private List<ModelObject> GetCachedAssemblyObjects()
    {
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
        {
            _cache.GetOrCreate(MATCHED_CONTENT_CACHE_KEY, entry => new HashSet<string>(StringComparer.OrdinalIgnoreCase)).Add(content);
        }

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
        {
            _cache.GetOrCreate(MATCHED_CONTENT_CACHE_KEY, entry => new HashSet<string>(StringComparer.OrdinalIgnoreCase)).Add(content);
        }

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
        {
            RefreshCache(drawing);
        }
        
        var cacheKey = $"{drawing.GetIdentifier()}_{typeof(T).Name}_filtered";
        return _cache.GetOrCreate(cacheKey, entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromMinutes(15);
            _logger.LogInformation($"Fetching {typeof(T).Name} objects from drawing...");

            var objects = new List<T>();
            var enumerator = drawing.GetSheet().GetAllObjects();
            while (enumerator.MoveNext())
            {
                if (enumerator.Current is T typedObject)
                {
                    objects.Add(typedObject);
                }
            }

            _logger.LogInformation($"Fetched and cached {objects.Count} {typeof(T).Name} objects from drawing.");
            return objects;
        });
    }

    private List<T> GetCachedDrawingObjects<T>(string cacheKey)
    {
        var drawing = _drawingHandler.GetActiveDrawing();
        var drawingId  = drawing.GetIdentifier().ToString();
        var cacheKeyWithDrawing = $"{drawingId}_{cacheKey}";

        lock (_lockObject)
        {
            _currentDrawingId = drawingId;
        }

        return _cache.Get<List<T>>(cacheKeyWithDrawing) ?? throw new InvalidOperationException("Cache unexpectedly empty. This should not happen.");
    }

    private void RefreshCache(Tekla.Structures.Drawing.Drawing drawing)
    {
        lock (_lockObject)
        {
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
                _logger.LogInformation($"Drawing objects cache refreshed with {objects.Count} objects. ");
                _logger.DebugInfo($"Cache refreshed in {stopwatch.ElapsedMilliseconds} ms."); 
            }
        }
    }

    private List<DrawingObject> FetchAllDrawingObjects(Tekla.Structures.Drawing.Drawing drawing)
    {
        var list = new List<DrawingObject>();
        var enumerator = drawing.GetSheet().GetAllObjects();
        while (enumerator.MoveNext())
        {
            if (enumerator.Current is not null)
            {
                list.Add(enumerator.Current);
            }
        }

        return list;
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
            TeklaWrapper.DrawingObjectListToSelection(results.Cast<DrawingObject>().ToList(), drawing);;
        }
    }

    private SearchQuery CreateSearchQuery(SearchConfiguration config) =>
        new(config.SearchTerm)
        {
            CaseSensitive = config.StringComparison
        };

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

    public void Dispose()
    {
        _events.DrawingChanged -= OnDrawingModified;
        _events.DrawingUpdated -= OnDrawingUpdated;
        _events.UnRegister();
        GC.SuppressFinalize(this);
    }
}