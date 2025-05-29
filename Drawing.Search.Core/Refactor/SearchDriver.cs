using System;
using System.Collections.Generic;
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
    private const string ASSEMBLY_CACHE_KEY = "assembly_objects";
    private const string DRAWING_OBJECTS_CACHE_KEY = "drawing_objects";
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
        ClearDrawingCache();
    }

    private void OnDrawingModified()
    {
        ClearDrawingCache();
    }

    private void ClearDrawingCache()
    {
        lock (_lockObject)
        {
            _cache.Remove($"{_currentDrawingId}_{DRAWING_OBJECTS_CACHE_KEY}");
            _logger.LogInformation($"Drawing cache cleared.");
        }
    }


    public SearchResult ExecuteSearch(SearchConfiguration config)
    {
        if (config == null) throw new ArgumentNullException();
        
        _logger.LogInformation($"Executing search with configuration: {config}.");

        try
        {
            var result = config.Type switch
            {
                SearchType.PartMark => ExecutePartMarkSearch(config),
                SearchType.Text => ExecuteTextSearch(config),
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
        var results = searcher.Search(modelObjects, CreateSearchQuery(config));
        
        SelectResults(results.Cast<ModelObject>().ToList());
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

    private SearchResult ExecuteTextSearch(SearchConfiguration config)
    {
        var texts = GetCachedDrawingObjects<Text>(
            cacheKey: "texts",
            fetchFunc: () => GetObjectsOfType<Text>(_drawingHandler.GetActiveDrawing()));
        
        var searcher = CreateSearcher<Text>(config);
        var results = searcher.Search(texts, CreateSearchQuery(config));
        
        SelectResults(results.Cast<DrawingObject>().ToList());

        return new SearchResult()
        {
            MatchCount = results.Count(),
            ElapsedMilliseconds = 0, // Set by caller
            SearchType = SearchType.Text
        };
    }

    private SearchResult ExecutePartMarkSearch(SearchConfiguration config)
    {
        var marks = GetCachedDrawingObjects<Mark>(
            cacheKey: "marks",
            fetchFunc: () => GetObjectsOfType<Mark>(_drawingHandler.GetActiveDrawing()));
        
        var searcher = CreateSearcher<Mark>(config);
        var results = searcher.Search(marks, CreateSearchQuery(config));
        
        SelectResults(results.Cast<DrawingObject>().ToList());

        return new SearchResult
        {
            MatchCount = results.Count(),
            ElapsedMilliseconds = 0, // Set by caller
            SearchType = SearchType.PartMark
        };
    }

    private List<T> GetCachedDrawingObjects<T>(string cacheKey, Func<List<T>> fetchFunc)
    {
        var drawing = _drawingHandler.GetActiveDrawing();
        var drawingId  = drawing.GetIdentifier().ToString();
        var cacheKeyWithDrawing = $"{drawingId}_{cacheKey}";

        lock (_lockObject)
        {
            _currentDrawingId = drawingId;
        }

        return _cache.GetOrCreate(cacheKeyWithDrawing, entry =>
        {
            _logger.LogInformation($"Cache miss for {typeof(T).Name} objects. Fetching from Tekla.");
            entry.SlidingExpiration = TimeSpan.FromMinutes(15);
            var result =  fetchFunc();
            _logger.LogInformation($"Cached {result.Count} {typeof(T).Name} objects.");
            return result;
        });
    }

    private List<T> GetObjectsOfType<T>(Tekla.Structures.Drawing.Drawing drawing) where T : DrawingObject
    {
        var objects = new List<T>();


        var allObjects = GetCachedDrawingObjects<DrawingObject>(
            DRAWING_OBJECTS_CACHE_KEY,
            (() =>
            {
                var list = new List<DrawingObject>();
                var enumerator = drawing.GetSheet().GetAllObjects();
                while (enumerator.MoveNext())
                {
                    if (enumerator.Current is T obj)
                    {
                        list.Add(obj);
                    }
                }

                return list;
            }));
        objects.AddRange(allObjects.OfType<T>());
        return objects;
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