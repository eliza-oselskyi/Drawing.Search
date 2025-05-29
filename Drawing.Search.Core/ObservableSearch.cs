using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
public class ObservableSearch<T>(List<ISearchStrategy> searchStrategies, IDataExtractor dataExtractor) : IObserverableSearch
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
        foreach (var observer in _observers.ToList())
        {
            observer.OnMatchFound(obj);
        }
    }

    /// <summary>
    /// Search an enumerable list of <c>T</c> items against a search query.
    /// </summary>
    /// <param name="items"><c>IEnumerable</c> item list</param>
    /// <param name="query">Search query. <c>SearchQuery</c> instance. </param>
    /// <returns><c>IEnumerable</c> list of matches of the type <c>T</c> provided.</returns>
    public IEnumerable<T> Search(IEnumerable<T> items, SearchQuery query)
    {

        var cacheKey = $"{query.Term}_{query.CaseSensitive}";

        if (_cache.TryGetValue(query.Term, out IEnumerable<T> res))
        {
            return res ?? Enumerable.Empty<T>();
        }

        var compiledStrategies = searchStrategies.ToList();
        var matches = new ConcurrentBag<T>();
        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount
        };
        
        var itemsList = items.ToList();

        Parallel.ForEach(
            Partition(itemsList, 1000),
            parallelOptions,
            chunk =>
            {
                foreach (var item in chunk)
                {
                    var data = dataExtractor.ExtractSearchableString(item);
                    if (compiledStrategies.Any(strategy => strategy.Match(data, query)))
                    {
                        matches.Add(item);
                        NotifyObservers(item);
                    }
                }
            });
        
        res = matches.ToList();
        _cache.Set(cacheKey, res, TimeSpan.FromMinutes(15));
        
        /*
        res =  items
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
        */
        
        return res;
    }

    private static IEnumerable<List<T>> Partition(List<T> list, int chunkSize)
    {
        for (int i = 0; i < list.Count; i+=chunkSize)
        {
            yield return list.GetRange(
                i,
                Math.Min(chunkSize, list.Count - i));
        }
    }
}
