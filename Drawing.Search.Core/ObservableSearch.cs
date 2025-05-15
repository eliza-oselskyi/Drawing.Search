using System;
using System.Collections.Generic;
using System.Linq;
using Drawing.Search.Core.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Tekla.Structures.Model;

namespace Drawing.Search.Core;

public class ObservableSearch<T>(ISearchStrategy<T>[] searchStrategies, IDataExtractor dataExtractor) : IObserverableSearch
{
    private readonly List<IObserver> _observers = [];
    private readonly MemoryCache _cache = new MemoryCache(new MemoryCacheOptions());

    public void Subscribe(IObserver observer)
    {
        _observers.Add(observer);
    }

    public void Unsubscribe(IObserver observer)
    {
        _observers.Remove(observer);
    }

    private void NotifyObservers(object obj)
    {
        foreach (var observer in _observers.ToList())
        {
            observer.OnMatchFound(obj);
        }
    }

    public IEnumerable<T> Search(IEnumerable<T> items, SearchQuery query)
    {

        if (_cache.TryGetValue(query.Term, out IEnumerable<T> res))
        {
            if (res != null) return res;
        }
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
        
        return res;
    }
}
