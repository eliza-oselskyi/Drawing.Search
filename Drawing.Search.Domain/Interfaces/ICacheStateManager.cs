using System;

namespace Drawing.Search.Domain.Interfaces;

public interface ICacheStateManager
{
    bool IsCaching { get; }
    event EventHandler<bool> IsCachingChanged;
    void SetCachingState(bool isCaching);
}