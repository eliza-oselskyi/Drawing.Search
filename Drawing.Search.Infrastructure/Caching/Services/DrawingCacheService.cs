using System;
using System.Collections.Generic;
using System.Threading;
using Drawing.Search.Domain.Interfaces;
using Drawing.Search.Infrastructure.Caching.Models;
using Tekla.Structures.DrawingInternal;

namespace Drawing.Search.Infrastructure.Caching.Services;

public class DrawingCacheService : IDrawingCache, IAssemblyCache, ICacheStateManager
{
    
    private readonly object _cacheLock = new();
    private readonly ISearchCache _searchCache;
    private readonly ISearchLogger _searchLogger;
    private readonly ICacheKeyGenerator _keyGenerator;
    private bool _isCaching;

    public DrawingCacheService(ISearchCache searchCache, ISearchLogger searchLogger, ICacheKeyGenerator keyGenerator)
    {
        _searchCache = searchCache ?? throw new ArgumentNullException(nameof(searchCache));
        _searchLogger = searchLogger ?? throw new ArgumentNullException(nameof(searchLogger));
        _keyGenerator = keyGenerator ?? throw new ArgumentNullException(nameof(keyGenerator));
        
        _searchCache.IsCachingChanged += (_, isCaching) => SetCachingState(isCaching);
    }
    
    
    
    public void CacheDrawing(string drawingId, object drawing, bool viewUpdated)
    {
        if (drawing is not Tekla.Structures.Drawing.Drawing teklaDrawing)
            throw new ArgumentException("Invalid drawing type", nameof(drawing));

        lock (_cacheLock)
        {
            try
            {
                SetCachingState(true);
                var drawingKey = _keyGenerator.GenerateDrawingKey(drawingId);
                _searchLogger.LogInformation($"Caching drawing {drawingId}");

                var teklaCache = _searchCache as TeklaSearchCache;
                teklaCache?.WriteAllObjectsInDrawingToCache(teklaDrawing, viewUpdated);
            }
            finally
            {
                SetCachingState(false);
            }
        }
    }

    public List<string> GetDrawingIdentifiers(string drawingId)
    {
        var drawingKey = _keyGenerator.GenerateDrawingKey(drawingId);
        return _searchCache.DumpIdentifiers(drawingKey);
    }

    public object GetDrawingObject(string drawingKey, string objectId)
    {
        //var drawingKey = _keyGenerator.GenerateDrawingKey(drawingId);
        return _searchCache.GetFromCache(drawingKey, objectId);
    }

    public void InvalidateDrawing(string drawingId)
    {
        var drawingKey = _keyGenerator.GenerateDrawingKey(drawingId);
        _searchCache.InvalidateCache(drawingKey);
    }

    public void RefreshCache(object drawing, bool viewUpdated, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            Console.WriteLine($"Cancellation Requested from refresh.");
            _isCaching = false;
            cancellationToken.ThrowIfCancellationRequested();
        }

        lock (_cacheLock)
        {
            try
            {
                _isCaching = true;
                var teklaDrawing = drawing as Tekla.Structures.Drawing.Drawing;
                var drawingKey = _keyGenerator.GenerateDrawingKey(teklaDrawing.GetIdentifier().ToString());
                _searchLogger.LogInformation($"Start refresh cache: Drawing ID: {teklaDrawing.GetIdentifier().ToString()}");
                //_searchCache.RemoveMainKeyFromCache(drawingKey);
                var teklaCache = _searchCache as TeklaSearchCache;
                if (teklaDrawing != null) teklaCache?.WriteAllObjectsInDrawingToCache(teklaDrawing, viewUpdated);
            }
            finally
            {
                _isCaching = false;
            }
        }
    }

    public bool HasDrawingBeenCached(string drawingId)
    {
        var drawingKey = _keyGenerator.GenerateDrawingKey(drawingId);
        var keys = _searchCache.DumpIdentifiers();
        return keys.Contains(drawingKey);
    }

    public void CacheAssemblyPosition(string identifier, string assemblyPosition)
    {
        _searchCache.CacheAssemblyPosition(identifier, assemblyPosition);
    }

    public IEnumerable<string> FindByAssemblyPosition(string drawingId, string assemblyPosition)
    {
        return _searchCache.FindByAssemblyPosition(drawingId, assemblyPosition);
    }

    public IEnumerable<object> GetAssemblyObjects(string assemblyPosition)
    {
        return _searchCache.FetchAssemblyPosition(assemblyPosition);
    }

    public IEnumerable<string> GetAllAssemblyPositions()
    {
        var teklaCache = _searchCache as TeklaSearchCache;
        return teklaCache?.DumpAssemblyPositions() ?? new List<string>();
    }
    
    public IEnumerable<object> GetRelatedObjects(string drawingId, string objectId)
    {
        var teklaCache = _searchCache as TeklaSearchCache;
        var dwgKey = _keyGenerator.GenerateDrawingKey(drawingId);
        return teklaCache?.GetRelatedObjects(dwgKey, objectId) ?? Array.Empty<object>();
    }

    public bool IsCaching => _isCaching;
    public event EventHandler<bool>? IsCachingChanged;
    public void SetCachingState(bool isCaching)
    {
        if (_isCaching == isCaching) return;
        _isCaching = isCaching;
        IsCachingChanged?.Invoke(this, isCaching);
    }
}