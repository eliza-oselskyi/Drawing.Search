﻿using System;
using System.Collections;
using System.Collections.Generic;
using Drawing.Search.Caching;
using Drawing.Search.Caching.Interfaces;
using Drawing.Search.CADIntegration;
using Tekla.Structures.DrawingInternal;

namespace Drawing.Search.Core.CacheService;

public class TeklaCacheService : ICacheService
{
    private readonly object _cacheLock = new();
    private readonly ISearchCache _searchCache;
    private bool _isCaching;

    public TeklaCacheService(ISearchCache searchCache)
    {
        _searchCache = searchCache ?? throw new ArgumentNullException(nameof(searchCache));

        _searchCache.IsCachingChanged += (_, isCaching) => { OnCachingStateChanged(isCaching); };
    }

    public bool IsCaching
    {
        get => _isCaching;
        set
        {
            if (_isCaching != value)
            {
                _isCaching = value;
                OnCachingStateChanged(_isCaching);
            }
        }
    }

    public event EventHandler<bool>? IsCachingChanged;


    public void AddMainKeyToCache(string mainKey)
    {
        _searchCache.AddMainKeyToCache(mainKey);
    }

    public void AddEntryByMainKey(string mainKey, string entryKey, object entryValue)
    {
        _searchCache.AddEntryByMainKey(mainKey, entryKey, entryValue);
    }

    public void RemoveMainKeyFromCache(string mainKey)
    {
        _searchCache.RemoveMainKeyFromCache(mainKey);
    }

    public void RemoveEntryByMainKey(string mainKey, string entryKey)
    {
        _searchCache.RemoveEntryByMainKey(mainKey, entryKey);
    }

    public void InvalidateCacheByKey(string key)
    {
        lock (_cacheLock)
        {
            LogCacheAction("Invalidate Cache", key);
            _searchCache.InvalidateCache(key);
        }
    }

    public object GetFromCache(string mainKey, string key)
    {
        lock (_cacheLock)
        {
            return _searchCache.GetFromCache(mainKey, key);
        }
    }

    public List<string> DumpIdentifiers(string drawingId)
    {
        var tCache = _searchCache as TeklaSearchCache;
        var drawingKey = GenerateDrawingCacheKey(drawingId);
        return tCache?.DumpIdentifiers(drawingKey) ?? new List<string>();
    }

    public IEnumerable<object> GetRelatedObjects(string drawingId, string objectId)
    {
        var tCache = _searchCache as TeklaSearchCache;
        var dwgKey = GenerateDrawingCacheKey(drawingId);
        return tCache?.GetRelatedObjects(dwgKey, objectId) ?? Array.Empty<object>();
    }

    public void WriteAllObjectsInDrawingToCache(object drawing)
    {
        if (drawing is not Tekla.Structures.Drawing.Drawing teklaDrawing)
            throw new ArgumentNullException(nameof(teklaDrawing));

        lock (_cacheLock)
        {
            try
            {
                IsCaching = true;
                LogCacheAction("Start write cache", $"Drawing ID: {teklaDrawing.GetIdentifier().ToString()}");

                ((TeklaSearchCache)_searchCache).WriteAllObjectsInDrawingToCache(teklaDrawing);
            }
            finally
            {
                IsCaching = false;
            }
        }
    }

    public void RefreshCache(string drawingKey)
    {
        throw new NotImplementedException();
    }

    public void RefreshCache(string drawingKey, object drawing)
    {
        var teklaDrawing = drawing as Tekla.Structures.Drawing.Drawing;
        SearchService.SearchService.GetLoggerInstance().LogInformation("Refreshing cache");
        _searchCache.RefreshCache();
        //_searchCache.RemoveMainKeyFromCache(drawingKey);
        if (teklaDrawing != null) WriteAllObjectsInDrawingToCache(teklaDrawing);
    }

    public void RefreshCache(object drawing)
    {
        lock (_cacheLock)
        {
            try
            {
                IsCaching = true;
                var teklaDrawing = drawing as Tekla.Structures.Drawing.Drawing;
                var drawingKey = GenerateDrawingCacheKey(teklaDrawing.GetIdentifier().ToString());
                LogCacheAction("Start refresh cache", $"Drawing ID: {drawingKey}");
                //_searchCache.RemoveMainKeyFromCache(drawingKey);
                if (teklaDrawing != null) WriteAllObjectsInDrawingToCache(teklaDrawing);
            }
            finally
            {
                IsCaching = false;
            }
        }
    }

    public IEnumerable<string> FindByAssemblyPosition(string drawingId, string assemblyPos)
    {
        return _searchCache.FindByAssemblyPosition(drawingId, assemblyPos);
    }

    public IEnumerable<string> DumpAssemblyPositions()
    {
        if (_searchCache is TeklaSearchCache tCache) return tCache.DumpAssemblyPositions();
        else
            throw new ArgumentNullException(nameof(tCache), "Cache not initialized.");
    }

    public IEnumerable<object> FetchAssemblyPosition(string assemblyPos)
    {
        return _searchCache.FetchAssemblyPosition(assemblyPos);
    }

    public List<string> DumpIdentifiers()
    {
        var tCache = _searchCache as TeklaSearchCache;

        return tCache?.DumpIdentifiers() ?? new List<string>();
    }

    public ArrayList GetSelectablePartsFromCache(string drawingKey, List<string> ids)
    {
        var tCache = _searchCache as TeklaSearchCache;

        return tCache?.GetSelectablePartsFromCache(drawingKey, ids) ?? new ArrayList();
    }


    private static string GenerateDrawingCacheKey(string drawingId)
    {
        return new CacheKeyBuilder(drawingId).UseDrawingKey().AppendObjectId().Build();
    }

    private string GenerateDrawingObjectCacheKey(string drawingId, string objectId)
    {
        var dwgKey = GenerateDrawingCacheKey(drawingId);
        return new CacheKeyBuilder(objectId).Append(dwgKey).UseDrawingObjectKey().AppendObjectId().Build();
    }

    private string GenerateAssemblyCacheKey(string drawingId, string assemblyId)
    {
        var dwgKey = GenerateDrawingCacheKey(drawingId);
        return new CacheKeyBuilder(assemblyId).Append(dwgKey).UseAssemblyObjectKey().AppendObjectId().Build();
    }

    private void OnCachingStateChanged(bool isCaching)
    {
        IsCachingChanged?.Invoke(this, isCaching);
    }

    private static void LogCacheAction(string action, string key)
    {
        var logger = SearchService.SearchService.GetLoggerInstance();
        logger.LogInformation($"Cache action: {action} with key {key}");
    }
}