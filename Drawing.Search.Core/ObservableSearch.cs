using System;
using System.Collections.Generic;
using System.Linq;
using Drawing.Search.Core.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Tekla.Structures.Model;

namespace Drawing.Search.Core;

/// <summary>
/// Generic search class, that can be observed using IObserver objects.
/// </summary>
/// <param name="searchStrategies">Array of search strategies to use.</param>
/// <param name="dataExtractor">Data extractor to use.</param>
/// <typeparam name="T">Type of object to search on.</typeparam>
public class ObservableSearch<T>(ISearchStrategy<T>[] searchStrategies, IDataExtractor dataExtractor)
    : IObserverableSearch
{
    private readonly List<IObserver> _observers = [];
    private readonly MemoryCache _cache = new MemoryCache(new MemoryCacheOptions());

    /// <summary>
    /// Subscribes an observer to the instance.
    /// </summary>
    /// <param name="observer">IObserver observer object instance.</param>
    /// <returns></returns>
    public void Subscribe(IObserver observer)
    {
        _observers.Add(observer);
    }

    /// <summary>
    /// Unsubscribes an observer to the instance.
    /// </summary>
    /// <param name="observer">IObserver observer object instance.</param>
    /// <returns></returns>
    public void Unsubscribe(IObserver observer)
    {
        _observers.Remove(observer);
    }

    /// <summary>
    /// Notifies all observers and passes an object to them.
    /// Used for passing successful matches to an observer.
    /// </summary>
    /// <param name="obj">An object.</param>
    /// <returns></returns>
    private void NotifyObservers(object obj)
    {
        foreach (var observer in _observers.ToList()) observer.OnMatchFound(obj);
    }

    /// <summary>
    /// Search an enumerable list of <c>T</c> items against a search query.
    /// </summary>
    /// <param name="items"><c>IEnumerable</c> item list</param>
    /// <param name="query">Search query. <c>SearchQuery</c> instance. </param>
    /// <returns><c>IEnumerable</c> list of matches of the type <c>T</c> provided.</returns>
    public IEnumerable<T> Search(IEnumerable<T> items, SearchQuery query)
    {
        if (_cache.TryGetValue(query.Term, out IEnumerable<T> res))
            if (res != null)
                return res;

        res = items
            .AsParallel()
            .Where((o) =>
            {
                var data = dataExtractor.ExtractSearchableString(o);
                if (!searchStrategies.AsParallel().Any(strategy => strategy.Match(data, query))) return false;
                NotifyObservers(o);
                return true;
            })
            .ToList();

        _cache.Set(query.Term, res, TimeSpan.FromMinutes(15));

        return res;
    }
}