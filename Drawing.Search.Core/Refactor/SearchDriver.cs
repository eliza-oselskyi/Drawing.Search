using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Drawing.Search.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Tekla.Structures.Drawing;
using Tekla.Structures.DrawingInternal;
using Tekla.Structures.Model;
using Events = Tekla.Structures.Drawing.UI.Events;
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
        //_events.DrawingLoaded += _cache.Clear;
        _events.Register();
    }

    public SearchResult ExecuteSearch(SearchConfiguration config)
    {
        if (config == null) throw new ArgumentNullException();
        
        _logger.LogInformation($"Executing search with configuration: {config}.");
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = config.Type switch
            {
                SearchType.PartMark => ExecutePartMarkSearch(config),
                SearchType.Text => ExecuteTextSearch(config),
                SearchType.Assembly => ExecuteAssemblySearch(config),
                _ => throw new ArgumentException($"Unsupported search type: {config.Type}")
            };
            
            stopwatch.Stop();
            _logger.LogInformation($"Search completed in {stopwatch.ElapsedMilliseconds} ms.");

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
        throw new NotImplementedException();
    }

    private SearchResult ExecuteTextSearch(SearchConfiguration config)
    {
        throw new NotImplementedException();
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
        var cacheKeyWithDrawing = $"{drawing.GetIdentifier().ToString()}_{cacheKey}";

         return _cache.GetOrCreate(cacheKeyWithDrawing, entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromMinutes(15);
            return fetchFunc();
        });
    }

    private List<T> GetObjectsOfType<T>(Tekla.Structures.Drawing.Drawing drawing) where T : DrawingObject
    {
        var objects = new List<T>();
        var enumerator = drawing.GetSheet().GetAllObjects();

        while (enumerator.MoveNext())
        {
            if (enumerator.Current is T obj)
            {
                objects.Add(obj);
            }
        }

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
        _events.UnRegister();
        GC.SuppressFinalize(this);
    }
}

public class SearchLogger : ISearchLogger
{
    public void LogInformation(string message)
    {
        Debug.WriteLine($"INFO: {message}");
        Console.WriteLine($"INFO: {message}");
    }

    public void LogError(Exception exception, string message)
    {
        Debug.WriteLine($"ERROR: {message}");
        Debug.WriteLine($"Exception: {exception}");
        Console.WriteLine($"ERROR: {message}");
        Console.WriteLine($"Exception: {exception}");
    }

    public void DebugInfo(string message)
    {
        Debug.WriteLine($"DEBUG: {message}");
    }
}

internal interface ISearchLogger
{
    void LogInformation(string message);
    void LogError(Exception exception, string message);
    void DebugInfo(string message);
}