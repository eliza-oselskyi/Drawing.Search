using System.Collections.Generic;
using System.Linq;
using Drawing.Search.Core.Interfaces;
using Tekla.Structures.Model;

namespace Drawing.Search.Core;

public class ObservableSearch<T>(ISearchStrategy<T>[] searchStrategies, IDataExtractor dataExtractor) : IObserverableSearch
{
    private readonly List<IObserver> _observers = [];

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
        return items
            .AsParallel()
            .Where((o) =>
            {
                var data = dataExtractor.ExtractSearchableString(o);
                if (!searchStrategies.Any(strategy => strategy.Match(data, query))) return false;
                NotifyObservers(o);
                return true;
            })
            .ToList();
    }
}
