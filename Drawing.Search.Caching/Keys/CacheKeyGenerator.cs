using Drawing.Search.Caching.Interfaces;

namespace Drawing.Search.Caching.Keys;

public class CacheKeyGenerator : ICacheKeyGenerator
{
    public string GenerateDrawingKey(string drawingId)
    {
        return new CacheKeyBuilder(drawingId)
            .UseDrawingKey().AppendObjectId().Build();
        //return new CacheKeyBuilder(drawingId).CreateDrawingCacheKey();
    }

    public string GenerateDrawingObjectKey(string drawingId, string objectId)
    {
        var drawingKey = GenerateDrawingKey(drawingId);
        return new CacheKeyBuilder(objectId)
            .Append(drawingKey)
            .UseDrawingObjectKey()
            .AppendObjectId()
            .Build();
    }

    public string GenerateAssemblyKey(string drawingId, string assemblyId)
    {
        var drawingKey = GenerateDrawingKey(drawingId);
        return new CacheKeyBuilder(assemblyId)
            .Append(drawingKey)
            .UseAssemblyObjectKey()
            .AppendObjectId()
            .Build();
    }
}