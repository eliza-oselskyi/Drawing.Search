using System;
using System.Collections;
using System.Collections.Generic;
using Drawing.Search.Core.CacheService.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace Drawing.Search.Core.CacheService;

public class TeklaCacheService : ICacheService
{
    private readonly ISearchCache _searchCache;
    
    public TeklaCacheService(ISearchCache searchCache)
    {
        _searchCache = searchCache;
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
        throw new NotImplementedException();
    }

    public IEnumerable<object> GetRelatedObjects(string drawingKey, string objectId)
    {
        var tCache = _searchCache as TeklaSearchCache;
        return tCache.GetRelatedObjects(drawingKey, objectId);
    }
}