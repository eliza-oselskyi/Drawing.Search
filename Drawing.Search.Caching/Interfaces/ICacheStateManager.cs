using System;

namespace Drawing.Search.Caching.Interfaces;

public interface ICacheStateManager
{
    bool IsCaching { get; }
    event EventHandler<bool> IsCachingChanged;
    void SetCachingState(bool isCaching);
}