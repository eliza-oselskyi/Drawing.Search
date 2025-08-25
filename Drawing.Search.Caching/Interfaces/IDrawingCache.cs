using System.Collections.Generic;
using System.Threading;

namespace Drawing.Search.Caching.Interfaces;

public interface IDrawingCache
{
    void CacheDrawing(string drawingId, object drawing, bool viewUpdated);
    List<string> GetDrawingIdentifiers(string drawingKey);
    object GetDrawingObject(string drawingKey, string objectId);
    IEnumerable<object> GetRelatedObjects(string drawingId, string objectId);
    void InvalidateDrawing(string drawingId);
    void RefreshCache(object drawing, bool viewUpdated, CancellationToken cancellationToken);
    bool HasDrawingBeenCached(string drawingId);
}