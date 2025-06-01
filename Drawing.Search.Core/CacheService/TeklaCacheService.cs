using System;
using System.Collections;
using System.Collections.Generic;
using Drawing.Search.Caching;
using Drawing.Search.Caching.Interfaces;
using Drawing.Search.CADIntegration;
using Drawing.Search.Core.CacheService.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Tekla.Structures.Drawing;
using Tekla.Structures.DrawingInternal;

namespace Drawing.Search.Core.CacheService;

public class TeklaCacheService : ICacheService
{
    private readonly TeklaSearchCache _searchCache;
    private readonly object _cacheLock = new();
    private bool _isCaching;
    public event EventHandler<bool> IsCachingChanged;
    
    public TeklaCacheService(TeklaSearchCache searchCache)
    {
        _searchCache = searchCache ?? throw new ArgumentNullException(nameof(searchCache));

            _searchCache.IsCachingChanged += (sender, isCaching) =>
            {
                OnCachingStateChanged(isCaching);
            };
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
    
    public List<string> DumpIdentifiers()
    {
        var tCache = _searchCache as TeklaSearchCache;
        
        return tCache.DumpIdentifiers();
    }
    public List<string> DumpIdentifiers(string drawingId)
    {
        var tCache = _searchCache as TeklaSearchCache;
        var drawingKey = GenerateDrawingCacheKey(drawingId);
        return tCache.DumpIdentifiers(drawingKey);
    }
    
    public IEnumerable<object> GetRelatedObjects(string drawingId, string objectId)
    {
        var tCache = _searchCache as TeklaSearchCache;
        var dwgKey = GenerateDrawingCacheKey(drawingId);
        return tCache.GetRelatedObjects(dwgKey, objectId);
    }

    public void WriteAllObjectsInDrawingToCache(Tekla.Structures.Drawing.Drawing drawing)
    {
        if (drawing == null) throw new ArgumentNullException(nameof(drawing));

        lock (_cacheLock)
        {
            try
            {
                IsCaching = true;
                LogCacheAction("Start write cache", $"Drawing ID: {drawing.GetIdentifier().ToString()}");
                
                _searchCache.WriteAllObjectsInDrawingToCache(drawing);
                LogCacheAction("Start write cache", $"Drawing ID: {drawing.GetIdentifier().ToString()}");
                
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

    public void RefreshCache(Tekla.Structures.Drawing.Drawing drawing)
    {
        lock (_cacheLock)
        {
            try
            {
                IsCaching = true;
                var drawingKey = GenerateDrawingCacheKey(drawing.GetIdentifier().ToString());
                LogCacheAction("Start refresh cache", $"Drawing ID: {drawingKey}");
                _searchCache.RemoveMainKeyFromCache(drawingKey);
                WriteAllObjectsInDrawingToCache(drawing);
            }
            finally
            {
                IsCaching = false;
            }
        }
    }

    public ArrayList GetSelectablePartsFromCache(string drawingKey, List<string> ids)
    {
        var tCache = _searchCache as TeklaSearchCache;
        
        return tCache.GetSelectablePartsFromCache(drawingKey, ids);
    }
    
    public void RefreshCache(string drawingKey, Tekla.Structures.Drawing.Drawing drawing)
    {
        SearchService.SearchService.GetLoggerInstance().LogInformation("Refreshing cache");
        _searchCache.RefreshCache();
        //_searchCache.RemoveMainKeyFromCache(drawingKey);
        WriteAllObjectsInDrawingToCache(drawing);
        
    }


    private string GenerateDrawingCacheKey(string drawingId)
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

    private void LogCacheAction(string action, string key)
    {
        var logger = SearchService.SearchService.GetLoggerInstance();
        logger.LogInformation($"Cache action: {action} with key {key}");
    }
}