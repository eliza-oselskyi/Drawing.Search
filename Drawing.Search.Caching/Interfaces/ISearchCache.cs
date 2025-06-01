using System;
using System.Collections.Generic;

namespace Drawing.Search.Caching.Interfaces;

public interface ISearchCache
{
    void RefreshCache();
    void ClearCache();
    void AddMainKeyToCache(string mainKey);
    void AddEntryByMainKey(string mainKey, string entryKey, object entryValue);

    void RemoveMainKeyFromCache(string mainKey);
    void RemoveEntryByMainKey(string mainKey, string entryKey);
    void InvalidateCache(string key);

    object GetFromCache(string mainKey, string key);
    List<string> DumpIdentifiers();
    List<string> DumpIdentifiers(string mainKey);
    bool IsCaching();
    event EventHandler<bool> IsCachingChanged;
}