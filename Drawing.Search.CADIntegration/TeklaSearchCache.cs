using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Drawing.Search.Caching;
using Drawing.Search.Caching.Interfaces;
using Drawing.Search.Common.Interfaces;
using Tekla.Structures.Drawing;
using Tekla.Structures.DrawingInternal;
using Tekla.Structures.Model;
using ModelObject = Tekla.Structures.Model.ModelObject;
using Part = Tekla.Structures.Drawing.Part;

namespace Drawing.Search.CADIntegration;

/// <summary>
///     Provides in-memory caching for Tekla Structures drawing and model objects, aimed at optimizing
///     performance and enabling efficient retrieval of objects and their relationships.
/// </summary>
/// <example>
///     <code>
/// var cache = new TeklaSearchCache();
/// 
/// // Add a drawing and related objects to the cache
/// 
/// var drawing = DrawingHandler.Instance.GetActiveDrawing();
/// cache.WriteAllObjectsInDrawingToCache(drawing);
/// 
/// // Retrieve an object from the cache
/// 
/// var objectFromCache = cache.GetFromCache("mainKey", "objectKey");
/// 
/// // Add a relationship between two objects
/// 
/// cache.AddRelationship("mainKey", "objectKey1", "objectKey2");
/// 
/// // Fetch related objects
/// 
/// var relatedObjects = cache.GetRelatedObjects("mainKey", "objectKey");
/// 
/// // Invalidate the cache for a specific key
/// 
/// cache.InvalidateCache("mainKey");
/// </code>
/// </example>
public class TeklaSearchCache : ISearchCache
{
    private readonly Dictionary<string, HashSet<string>> _assemblyPositionsToIdentifiers = new();
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, object>> _cache = new();
    private readonly ConcurrentDictionary<string, bool> _dirtyDrawingsCache = new();
    private readonly object _lockObject = new();
    private readonly ISearchLogger _logger;

    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, HashSet<string>>> _relationshipsCache =
        new();

    private bool _isCaching;
    private bool _isDirty;
    private bool _isInitialCachingDone;

    public TeklaSearchCache(ISearchLogger logger)
    {
        _logger = logger;
        _isInitialCachingDone = false;
    }

    public void CacheAssemblyPosition(string identifier, string assemblyPosition)
    {
        if (string.IsNullOrEmpty(assemblyPosition)) return;

        if (!_assemblyPositionsToIdentifiers.TryGetValue(assemblyPosition, out var identifiers))
        {
            identifiers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _assemblyPositionsToIdentifiers[assemblyPosition] = identifiers;
        }

        identifiers.Add(identifier);
    }

    public IEnumerable<string> FindByAssemblyPosition(string drawingId, string assemblyPosition)
    {
        var dwgKey = new CacheKeyBuilder(drawingId).UseDrawingKey().AppendObjectId().Build();
        var allIds = DumpIdentifiers(dwgKey);
        var allAssmeblyIds = DumpAssemblyPositions();
        var match = allIds.Find(m => m.Contains(assemblyPosition));
        return _assemblyPositionsToIdentifiers.TryGetValue(match, out var identifiers)
            ? identifiers
            : Enumerable.Empty<string>();
    }

    /// <summary>
    ///     Fully clears and resets all cached data if the cache has been flagged as "dirty."
    /// </summary>
    public void RefreshCache()
    {
        _isDirty = true;
        if (_isDirty)
        {
            SetIsCaching(true);
            lock (_lockObject)
            {
                if (_isInitialCachingDone)
                {
                    ClearDrawingObjectsOnly();
                }
                else
                {
                    _cache.Clear();
                    _relationshipsCache.Clear();
                }
            }

            _isDirty = false;
            SetIsCaching(false);
        }
    }


    /// <summary>
    ///     Marks the entire cache or a specific cache entry as "dirty," indicating it needs to be refreshed.
    /// </summary>
    /// <param name="key">The specific cache key to mark as dirty.</param>
    public void InvalidateCache(string key)
    {
        _isDirty = true;
    }

    public void ClearCache()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     Adds a new main key to the cache, creating an empty entry for it.
    /// </summary>
    /// <param name="mainKey">The main key to add to the cache.</param>
    public void AddMainKeyToCache(string mainKey)
    {
        SetIsCaching(true);
        lock (_lockObject)
        {
            var dict = new ConcurrentDictionary<string, object>();
            _cache.TryAdd(mainKey, dict);
        }

        SetIsCaching(false);
    }


    /// <summary>
    ///     Adds an object to the cache under the specified main key and object key.
    /// </summary>
    /// <param name="mainKey">The main cache key for grouping related objects.</param>
    /// <param name="objectKey">The unique key for the object within the main key group.</param>
    /// <param name="value">The object to add to the cache.</param>
    public void AddEntryByMainKey(string mainKey, string objectKey, object value)
    {
        SetIsCaching(true);
        lock (_lockObject)
        {
            _cache.TryGetValue(mainKey, out var objects);
            if (objects != null) objects.TryAdd(objectKey, value);
        }

        SetIsCaching(false);
    }


    /// <summary>
    ///     Retrieves an object from the cache based on the main key and object key.
    /// </summary>
    /// <param name="mainKey">The main key corresponding to the cached group.</param>
    /// <param name="objKey">The specific key of the object within the cache.</param>
    /// <returns>The cached object, or <c>null</c> if not found.</returns>
    public object GetFromCache(string mainKey, string objKey)
    {
        lock (_lockObject)
        {
            _cache.TryGetValue(mainKey, out var entry);
            entry.TryGetValue(objKey, out var value);
            return value;
        }
    }

    /// <summary>
    ///     Removes a main cache entry and all associated relationships and objects.
    /// </summary>
    /// <param name="mainKey">The main cache key to remove.</param>
    public void RemoveMainKeyFromCache(string mainKey)
    {
        SetIsCaching(true);
        lock (_lockObject)
        {
            var rels = DumpRelationships(mainKey);
            if (rels.Any(k => k.Contains(mainKey)))
                foreach (var relKey in rels)
                {
                    _relationshipsCache.TryGetValue(relKey, out var relSet);
                    if (relSet != null)
                        foreach (var cacheKey in relSet)
                        {
                            _cache.TryGetValue(mainKey, out var objects);
                            if (objects != null) objects.TryRemove(cacheKey.Key, out _);
                        }

                    _relationshipsCache.TryRemove(relKey, out _);
                }
            else
                _cache.TryRemove(mainKey, out _);
        }

        SetIsCaching(false);
    }


    /// <summary>
    ///     Removes a specific entry under a main cache key and its associated relationships.
    /// </summary>
    /// <param name="mainKey">The main cache key.</param>
    /// <param name="entryKey">The key of the entry to remove.</param>
    public void RemoveEntryByMainKey(string mainKey, string entryKey)
    {
        SetIsCaching(true);
        lock (_lockObject)
        {
            var rels = DumpRelationships(mainKey);
            _relationshipsCache.TryGetValue(mainKey, out var relSet);
            if (rels.Any(k => k.Contains(entryKey)))
            {
                var relKeys = rels.FindAll(k => k.Contains(entryKey));
                foreach (var relKey in relKeys)
                    if (relSet != null)
                    {
                        relSet.TryGetValue(relKey, out var keys);
                        if (keys != null)
                            foreach (var cacheKey in keys)
                            {
                                _cache.TryGetValue(mainKey, out var objects);
                                if (objects != null) objects.TryRemove(cacheKey, out _);
                            }

                        relSet.TryRemove(relKey, out _);
                    }
            }
            else
            {
                _cache.TryGetValue(mainKey, out var objects);
                if (objects != null) objects.TryRemove(entryKey, out _);
            }
        }

        SetIsCaching(false);
    }


    /// <summary>
    ///     Gets all identifiers (subkeys) under a specific main key in the cache.
    /// </summary>
    /// <param name="mainKey">The main cache key to inspect.</param>
    /// <returns>A list of object keys under the specified main key.</returns>
    public List<string> DumpIdentifiers(string mainKey)
    {
        var ids = new List<string>();
        lock (_lockObject)
        {
            _cache.TryGetValue(mainKey, out var objects);
            if (objects != null) ids.AddRange(objects.Keys);
        }

        return ids;
    }

    /// <summary>
    ///     Gets all top-level identifiers (main keys) in the cache.
    /// </summary>
    /// <returns>A list of all main cache keys.</returns>
    public List<string> DumpIdentifiers()
    {
        var ids = new List<string>();
        lock (_lockObject)
        {
            ids.AddRange(_cache.Keys);
        }

        return ids;
    }


    public event EventHandler<bool>? IsCachingChanged;

    public bool IsCaching()
    {
        return _isCaching;
    }

    public IEnumerable<object> FetchAssemblyPosition(string assemblyPosition)
    {
        _assemblyPositionsToIdentifiers.TryGetValue(assemblyPosition, out var identifiers);
        return identifiers;
    }

    public List<string> DumpAssemblyPositions()
    {
        var ids = new List<string>();
        lock (_lockObject)
        {
            foreach (var id in _assemblyPositionsToIdentifiers) ids.Add(id.Key);
        }

        return ids;
    }

    private void ClearDrawingObjectsOnly()
    {
        var relCacheKeyList = _relationshipsCache.Keys.ToList();
        var cacheKeyListMain = _cache.Keys.ToList();
        foreach (var cacheKey in cacheKeyListMain)
        {
            _cache.TryGetValue(cacheKey, out var objects);
            _relationshipsCache.TryGetValue(cacheKey, out var relSet);

            if (objects == null) continue;
            var cacheKeyListSecondary = (objects.Keys ?? []).ToList();
            if (relSet != null)
            {
                var relKeyListSecondary = relSet.Keys.ToList();
                foreach (var k in cacheKeyListSecondary)
                    if (!relKeyListSecondary.Contains(k))
                        _cache[cacheKey].TryRemove(k, out _);
            }
        }
    }

    /// <summary>
    ///     Refreshes specific cache keys within the cache.
    /// </summary>
    /// <param name="keys">A list of keys representing cache entries to refresh.</param>
    public void RefreshCache(List<string> keys)
    // TODO: Implement this method.
    {
        foreach (var key in keys)
            if (_dirtyDrawingsCache.ContainsKey(key))
            {
                var foundKey = _cache.Keys.FirstOrDefault(k => k.Contains(key));
                if (_cache.ContainsKey(foundKey))
                {
                }
            }
    }

    private static ModelObject GetRelatedModelObjectFromPart(Part part, out bool isMainPart)
    {
        var model = new Model();
        var id = part.ModelIdentifier;
        var modelObjectList = model.FetchModelObjects([id], false);
        var modelObject = modelObjectList.FirstOrDefault();
        if (modelObject is Tekla.Structures.Model.Part moPart)
        {
            var mainPart = moPart.GetAssembly().GetMainPart().Identifier.ID;
            isMainPart = modelObject.Identifier.ID == mainPart;
            return modelObject;
        }
        else
        {
            throw new Exception("Could not find model object for part");
        }
    }


    /// <summary>
    ///     Writes all objects from a Tekla drawing to the cache, mapping their identifiers
    ///     to Tekla model objects and storing their relationships.
    /// </summary>
    /// <param name="drawing">The Tekla drawing whose objects will be written to the cache.</param>
    public void WriteAllObjectsInDrawingToCache(Tekla.Structures.Drawing.Drawing drawing)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        var dwgKey = new CacheKeyBuilder(drawing.GetIdentifier().ToString()).UseDrawingKey().AppendObjectId().Build();
        var objects = drawing.GetSheet().GetAllObjects();
        _isInitialCachingDone = _cache.ContainsKey(dwgKey);
        SetIsCaching(true);
        AddMainKeyToCache(dwgKey);

        lock (_lockObject)
        {
            foreach (DrawingObject o in objects)
            {
                var key = new CacheKeyBuilder(o.GetIdentifier().ToString())
                    .Append(dwgKey)
                    .UseDrawingObjectKey()
                    .AppendObjectId()
                    .Build();

                if (o != null) AddEntryByMainKey(dwgKey, key, o);
                if (o is Part p && !_isInitialCachingDone)
                {
                    var modelObject = GetRelatedModelObjectFromPart(p, out var isMainPart);
                    var moKeyRaw = new CacheKeyBuilder(modelObject.Identifier.ToString())
                        .Append(dwgKey)
                        .UseAssemblyObjectKey()
                        .AppendObjectId();
                    if (isMainPart) moKeyRaw.IsMainPart();

                    var moKey = moKeyRaw.Build();

                    AddEntryByMainKey(dwgKey, moKey, modelObject);
                    AddRelationship(dwgKey, key, moKey);

                    var assemblyPostion = "";
                    modelObject.GetReportProperty("ASSEMBLY_POS", ref assemblyPostion);
                    var assemblyPosKey = new CacheKeyBuilder(moKey)
                        .AppendObjectId();
                    
                    // Cache the assembly position
                    if (isMainPart)
                    {
                        if (!string.IsNullOrEmpty(assemblyPostion))
                        {
                            assemblyPosKey = assemblyPosKey
                                .Append($"ASSEMBLY_POS_{assemblyPostion}");
                        }
                    }
                    else
                    {
                        var partPosition = "";
                        modelObject.GetReportProperty("PART_POS", ref partPosition);
                        assemblyPostion = assemblyPostion + "_" + partPosition;
                        if (!string.IsNullOrEmpty(assemblyPostion))
                        {
                            assemblyPosKey = assemblyPosKey
                                .Append($"PART_POS_{assemblyPostion}");
                        }
                    }

                    // Finish building key
                    var assemblyPosKeyString = assemblyPosKey
                        .AppendObjectId()
                        .Build();
                    
                    CacheAssemblyPosition(moKey, assemblyPostion);
                    AddEntryByMainKey(dwgKey, assemblyPosKeyString, assemblyPostion);
                }
            }

            _isInitialCachingDone = true;
        }

        SetIsCaching(false);
        stopwatch.Stop();
        _logger.LogInformation($"Reading and caching took {stopwatch.ElapsedMilliseconds} ms.");
    }

    /// <summary>
    ///     Retrieves all selectable <see cref="Part" /> objects based on the drawing's cache key
    ///     and a list of object identifiers.
    /// </summary>
    /// <param name="mainKey">The main key associated with the drawing in the cache.</param>
    /// <param name="objectIds">A list of object identifiers to query for.</param>
    /// <returns>
    ///     A collection of selectable parts matching the provided identifiers.
    /// </returns>
    public ArrayList GetSelectablePartsFromCache(string mainKey, List<string> objectIds)
    {
        var moList = new ArrayList();
        lock (_lockObject)
        {
            _cache.TryGetValue(mainKey, out var objects);

            if (objects != null)
                foreach (var id in objects)
                foreach (var _ in GetRelatedObjects(id.Key))
                {
                    var dwgObj = GetFromCache(mainKey, id.Key) as DrawingObject;
                    if (dwgObj is Part) moList.Add(dwgObj);
                }
        }

        return moList;
    }

    public void RefreshCache(string key)
    {
        lock (_lockObject)
        {
            _cache.TryRemove(key, out _);
        }
    }

    private string BuildRelationshipKey(string objectKey1, string objectKey2)
    {
        var keyBuilder = new CacheKeyBuilder(objectKey1);
        return keyBuilder.AddRelationshipTo(objectKey2).Build();
    }


    /// <summary>
    ///     Adds a relationship between two objects under the specified main cache key.
    /// </summary>
    /// <param name="mainKey">The main key corresponding to the cache group.</param>
    /// <param name="objectKey1">The first object key to relate.</param>
    /// <param name="objectKey2">The second object key to relate.</param>
    public void AddRelationship(string mainKey, string objectKey1, string objectKey2)
    {
        SetIsCaching(true);
        lock (_lockObject)
        {
            var relationshipKey = BuildRelationshipKey(objectKey1, objectKey2);
            if (!_relationshipsCache.ContainsKey(mainKey))
                _relationshipsCache.TryAdd(mainKey, new ConcurrentDictionary<string, HashSet<string>>());

            _relationshipsCache.TryGetValue(mainKey, out var relSet);
            if (relSet != null)
            {
                relSet.TryAdd(relationshipKey, new HashSet<string>());
                relSet[relationshipKey].Add(objectKey1);
                relSet[relationshipKey].Add(objectKey2);
            }
        }

        SetIsCaching(false);
    }


    /// <summary>
    ///     Retrieves related objects for a given main cache key and object key.
    /// </summary>
    /// <param name="mainKey">The main cache key.</param>
    /// <param name="objectKey">The key of the object whose related objects are desired.</param>
    /// <returns>A collection of objects related to the specified object key.</returns>
    public IEnumerable<object> GetRelatedObjects(string mainKey, string objectKey)
    {
        var relatedObjects = new List<object>();

        lock (_lockObject)
        {
            _relationshipsCache.TryGetValue(mainKey, out var relSet);
            if (relSet != null)
                foreach (var rel in relSet)
                    if (rel.Key.Contains(objectKey))
                        foreach (var relatedKey in rel.Value)
                        {
                            var relatedObject = GetFromCache(mainKey, relatedKey);
                            relatedObjects.Add(relatedObject);
                        }
        }

        return relatedObjects;
    }

    /// <summary>
    ///     Retrieves related objects for a specific object across all available cache entries.
    /// </summary>
    /// <param name="objectKey">The key of the object whose related objects are desired.</param>
    /// <returns>A collection of related objects.</returns>
    public IEnumerable<object> GetRelatedObjects(string objectKey)
    {
        var relatedObjects = new List<object>();

        lock (_lockObject)
        {
            var allRelationshipMainKeys = _relationshipsCache.Keys;

            foreach (var mainKey in allRelationshipMainKeys)
            {
                _relationshipsCache.TryGetValue(mainKey, out var relSet);
                if (relSet != null)
                    foreach (var rel in relSet)
                        if (rel.Key.Contains(objectKey))
                            foreach (var relatedKey in rel.Value)
                            {
                                var relatedObject = GetFromCache(mainKey, relatedKey);
                                relatedObjects.Add(relatedObject);
                            }
            }
        }

        return relatedObjects;
    }

    /// <summary>
    ///     Gets all relationship keys for a specific main cache key.
    /// </summary>
    /// <param name="mainKey">The main key for which to retrieve relationships.</param>
    /// <returns>A list of relationship keys under the specified main key.</returns>
    public List<string> DumpRelationships(string mainKey)
    {
        var ids = new List<string>();
        lock (_lockObject)
        {
            _relationshipsCache.TryGetValue(mainKey, out var relSet);
            if (relSet != null) ids.AddRange(relSet.Keys);
        }

        return ids;
    }

    /// <summary>
    ///     Gets all relationship keys across all cached entries.
    /// </summary>
    /// <returns>A list of relationship keys across all main keys.</returns>
    public List<string> DumpRelationships()
    {
        var ids = new List<string>();
        lock (_lockObject)
        {
            var keys = _relationshipsCache.Keys;
            foreach (var key in keys)
            {
                _relationshipsCache.TryGetValue(key, out var relSet);
                if (relSet != null) ids.AddRange(relSet.Keys);
            }
        }

        return ids;
    }

    public void SetIsCaching(bool value)
    {
        if (_isCaching != value)
        {
            _isCaching = value;
            IsCachingChanged?.Invoke(this, _isCaching);
        }
    }
}