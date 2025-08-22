using System;
using System.Collections.Generic;
using System.Threading;

namespace Drawing.Search.Caching.Interfaces;

public interface ISearchCache
{
    void CacheAssemblyPosition(string identifier, string assemblyPosition);
    IEnumerable<string> FindByAssemblyPosition(string drawingId, string assemblyPosition);
    void RefreshCache(CancellationToken cancellationToken);
    void ClearCache();
    void AddMainKeyToCache(string mainKey);
    void AddEntryByMainKey(string mainKey, string entryKey, object entryValue);

    void RemoveMainKeyFromCache(string mainKey);
    void RemoveEntryByMainKey(string mainKey, string entryKey);
    void InvalidateCache(string key);

    object GetFromCache(string mainKey, string key);
    IEnumerable<object> FetchAssemblyPosition(string assemblyPosition);
    List<string> DumpIdentifiers();
    List<string> DumpIdentifiers(string mainKey);
    bool IsCaching();
    event EventHandler<bool> IsCachingChanged;
}