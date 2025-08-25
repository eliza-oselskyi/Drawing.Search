using System;
using System.Collections.Generic;
using System.Threading;

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
    void WriteAllObjectsInDrawingToCache(object activeDrawing, bool viewUpdated);
    void RefreshCache(string drawingKey, object activeDrawing, bool viewUpdated, CancellationToken cancellationToken);
    IEnumerable<object> GetRelatedObjects(string drawingId, string objectId);
    void RefreshCache(object getActiveDrawing, bool viewUpdated, CancellationToken cancellationToken);
    IEnumerable<string> FindByAssemblyPosition(string drawingId, string assemblyPos);
    IEnumerable<string> DumpAssemblyPositions();
    IEnumerable<object> FetchAssemblyPosition(string assemblyPosition);
}