using System;
using System.Collections.Generic;

namespace Drawing.Search.Caching.Interfaces;

public interface ICacheService
{
    void RefreshCache(string drawingKey);
    void InvalidateCacheByKey(string key);
    void AddMainKeyToCache(string mainKey);
    void AddEntryByMainKey(string mainKey, string entryKey, object entryValue);
    void RemoveMainKeyFromCache(string mainKey);
    void RemoveEntryByMainKey(string mainKey, string entryKey);
    object GetFromCache(string mainKey, string key);
    List<string> DumpIdentifiers(string drawingKey);
    public event EventHandler<bool>? IsCachingChanged; 
}
