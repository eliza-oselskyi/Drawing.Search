using System.Collections.Generic;

namespace Drawing.Search.Infrastructure.Caching.Models;

/// <summary>
///     Provides a utility for constructing cache keys in a modular and structured manner.
///     Enables easy reuse of cache keys.
///     Cache keys are constructed incrementally by chaining utility methods. The key parts are added in sequence and
///     combined into a single string at the end, separated by underscores.
/// </summary>
/// <example>
///     Building a cache key for a specific drawing with relationships
///     <code>
/// var builder = new CacheKeyBuilder("12345");
/// string cacheKey = builder
///     .UseDrawingObjectKey()
///     .AddRelationshipTo("67890")
///     .Append("customTag")
///     .AppendObjectId()
///     .Build();
/// 
/// Console.WriteLine(cacheKey); // Output: drawing_object_relationship_12345:67890_customTag_12345
/// 
/// var builder = new CacheKeyBuilder("12345");
/// builder.UseDrawingObjectKey().UseMatchedContentKey().Append("some_custom_key");
/// var key = builder.Build(); // The resulting key will be: drawing_object_matched_content_12345_some_custom_key
/// </code>
/// </example>
public class CacheKeyBuilder
{
    /// <summary>
    ///     Cache keys for different types of objects.
    /// </summary>
    private const string DRAWING_OBJECT_CACHE_KEY = "drawing_object";

    private const string ASSEMBLY_OBJECT_CACHE_KEY = "assembly_object";
    private const string MATCHED_CONTENT_CACHE_KEY = "matched_content";
    private const string RELATIONSHIP_CACHE_KEY = "relationship";
    private const string DRAWING_CACHE_KEY = "drawing";

    private readonly List<string> _keyComponents = new();
    private readonly string _objectId;

    /// <summary>
    ///     Initializes a new instance of the <see cref="CacheKeyBuilder" /> class with the specified object id.
    /// </summary>
    /// <param name="objectId"> The unique identifier of the primary object for which the cache key is being built</param>
    public CacheKeyBuilder(string objectId)
    {
        _objectId = objectId;
    }

    /// <summary>
    ///     Adds the drawing_object cache key to the key components.
    /// </summary>
    /// <returns></returns>
    public CacheKeyBuilder UseDrawingObjectKey()
    {
        _keyComponents.Add(DRAWING_OBJECT_CACHE_KEY);
        return this;
    }

    /// <summary>
    ///     Adds the assembly_object cache key to the key components.
    /// </summary>
    /// <returns></returns>
    public CacheKeyBuilder UseAssemblyObjectKey()
    {
        _keyComponents.Add(ASSEMBLY_OBJECT_CACHE_KEY);
        return this;
    }

    /// <summary>
    ///     Adds the matched_content cache key to the key components.
    /// </summary>
    /// <returns></returns>
    public CacheKeyBuilder UseMatchedContentKey()
    {
        _keyComponents.Add(MATCHED_CONTENT_CACHE_KEY);
        return this;
    }

    /// <summary>
    ///     Adds the drawing cache key to the key components.
    /// </summary>
    /// <returns></returns>
    public CacheKeyBuilder UseDrawingKey()
    {
        _keyComponents.Add(DRAWING_CACHE_KEY);
        return this;
    }

    /// <summary>
    ///     Appends a relationship components to the key in the format <c>relationship_{objectId}:{relatedObjectId}</c>
    /// </summary>
    /// <param name="relatedObjectId">The identifier of the related object.</param>
    /// <returns></returns>
    public CacheKeyBuilder AddRelationshipTo(string relatedObjectId)
    {
        _keyComponents.Add($"{RELATIONSHIP_CACHE_KEY}_{_objectId}:{relatedObjectId}");
        return this;
    }

    /// <summary>
    ///     Appends the objectId (specified during initialization) to the cache key.
    /// </summary>
    /// <returns></returns>
    public CacheKeyBuilder AppendObjectId()
    {
        _keyComponents.Add(_objectId);
        return this;
    }

    /// <summary>
    ///     Appends a custom string to the cache key.
    /// </summary>
    /// <param name="key">The string to append.</param>
    /// <returns></returns>
    public CacheKeyBuilder Append(string key)
    {
        _keyComponents.Add(key);
        return this;
    }

    public CacheKeyBuilder IsMainPart()
    {
        _keyComponents.Add("main");
        return this;
    }

    public string CreateDrawingCacheKey()
    {
        return UseDrawingKey().AppendObjectId().Build();
    }

    /// <summary>
    ///     Combines all the key components into a single cache key string and returns it.
    /// </summary>
    /// <returns>The final cache key.</returns>
    public string Build()
    {
        return string.Join("_", _keyComponents);
    }
}