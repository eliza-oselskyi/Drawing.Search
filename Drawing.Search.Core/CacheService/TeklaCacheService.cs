using System;
using System.Collections;
using System.Collections.Generic;
using Drawing.Search.Core.CacheService.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Tekla.Structures.Drawing;

namespace Drawing.Search.Core.CacheService;

public class TeklaCacheService : ICacheService
{
    private readonly ISearchCache _searchCache;
    
    public event EventHandler<bool> IsCachingChanged;
    
    public TeklaCacheService(ISearchCache searchCache)
    {
        _searchCache = searchCache;

        if (_searchCache is TeklaSearchCache teklaSearchCache)
        {
            teklaSearchCache.IsCachingChanged += (sender, isCaching) =>
            {
                IsCachingChanged?.Invoke(this, isCaching);
            };
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
        _searchCache.InvalidateCache(key);
    }

    public void WriteAllObjectsInDrawingToCache(Tekla.Structures.Drawing.Drawing drawing)
    {
        var tCache = _searchCache as TeklaSearchCache;
        
        tCache.WriteAllObjectsInDrawingToCache(drawing);
    }

    public ArrayList GetSelectablePartsFromCache(string drawingKey, List<string> ids)
    {
        var tCache = _searchCache as TeklaSearchCache;
        
        return tCache.GetSelectablePartsFromCache(drawingKey, ids);
    }

    public List<string> DumpIdentifiers()
    {
        var tCache = _searchCache as TeklaSearchCache;
        
        return tCache.DumpIdentifiers();
    }
    public List<string> DumpIdentifiers(string drawingKey)
    {
        var tCache = _searchCache as TeklaSearchCache;
        
        return tCache.DumpIdentifiers(drawingKey);
    }

    public object GetFromCache(string mainKey, string key)
    {
        return _searchCache.GetFromCache(mainKey, key);
    }

    public void RefreshCache(string drawingKey)
    {
        _searchCache.RemoveMainKeyFromCache(drawingKey);
        var drawing = DrawingHandler.Instance.GetActiveDrawing();
        WriteAllObjectsInDrawingToCache(drawing);
    }

    public void RefreshCache(string drawingKey, Tekla.Structures.Drawing.Drawing drawing)
    {
        SearchService.SearchService.GetLoggerInstance().LogInformation("Refreshing cache");
        _searchCache.RefreshCache();
        //_searchCache.RemoveMainKeyFromCache(drawingKey);
        WriteAllObjectsInDrawingToCache(drawing);
        
    }

    public IEnumerable<object> GetRelatedObjects(string drawingKey, string objectId)
    {
        var tCache = _searchCache as TeklaSearchCache;
        return tCache.GetRelatedObjects(drawingKey, objectId);
    }

    public bool IsCaching()
    {
        return _searchCache.IsCaching();
    }
}