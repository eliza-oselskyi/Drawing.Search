using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Drawing.Search.Domain.Interfaces;
using Drawing.Search.Infrastructure;

namespace Drawing.Search.Application.Features.Search;

/// <summary>
///     Generic search class that can be observed using IObserver objects.
/// </summary>
/// <typeparam name="T">Type of object to search on.</typeparam>
public class ObservableSearch<T> : IObserverableSearch
{
    private readonly List<IObserver> _observers = [];
    private readonly List<ISearchStrategy> _searchStrategies;
    private readonly IDataExtractor _dataExtractor;
    private readonly ITestModeService _testModeService;
    private readonly ISearchLogger _logger;

    /// <summary>
    ///     Generic search class that can be observed using IObserver objects.
    /// </summary>
    /// <param name="searchStrategies">Array of search strategies to use.</param>
    /// <param name="dataExtractor">Data extractor to use.</param>
    /// <typeparam name="T">Type of object to search on.</typeparam>
    public ObservableSearch(List<ISearchStrategy> searchStrategies, IDataExtractor dataExtractor)
    {
        _searchStrategies = searchStrategies;
        _dataExtractor = dataExtractor;
        _testModeService = TestModeServiceLocator.Current;
        _logger = SearchLoggerServiceLocator.Current;
    }

    /// <summary>
    ///     Subscribes an observer to the instance.
    /// </summary>
    /// <param name="observer">IObserver observer object instance.</param>
    /// <returns></returns>
    public void Subscribe(IObserver observer)
    {
        _observers.Add(observer);
    }

    /// <summary>
    ///     Unsubscribes an observer to the instance.
    /// </summary>
    /// <param name="observer">IObserver observer object instance.</param>
    /// <returns></returns>
    public void Unsubscribe(IObserver observer)
    {
        _observers.Remove(observer);
    }

    /// <summary>
    ///     Notifies all observers and passes an object to them.
    ///     Used for passing successful matches to an observer.
    /// </summary>
    /// <param name="obj">An object.</param>
    /// <returns></returns>
    private void NotifyObservers(object obj)
    {
        foreach (var observer in _observers.ToList()) observer.OnMatchFound(obj);
    }

    /// <summary>
    ///     Search an enumerable list of <c>T</c> items against a search query.
    /// </summary>
    /// <param name="items"><c>IEnumerable</c> item list</param>
    /// <param name="query">Search query. <c>SearchQuery</c> instance. </param>
    /// <returns><c>IEnumerable</c> list of matches of the type <c>T</c> provided.</returns>
    public IEnumerable<T?> Search(IEnumerable<T?> items, ISearchQuery query)
    {
        var compiledStrategies = _searchStrategies.ToList();
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
                    if (item == null) continue;
                    var data = _dataExtractor.ExtractSearchableString(item);
                    if (_testModeService.IsTestMode) _logger.LogInformation($"Searching item: {item}, extracted: {data}".Trim());
                    if (!compiledStrategies.Any(strategy => strategy.Match(data, query))) continue;
                    if (_testModeService.IsTestMode) _logger.LogInformation($"Matched item: {item}, to : {data}".Trim());
                    matches.Add(item);
                    NotifyObservers(item);
                    IsMatchFound?.Invoke(item, true);
                }
            });

        return matches.ToList();
    }
    public event EventHandler<bool>? IsMatchFound;

    private static IEnumerable<List<T?>> Partition(List<T?> list, int chunkSize)
    {
        for (var i = 0; i < list.Count; i += chunkSize)
            yield return list.GetRange(
                i,
                Math.Min(chunkSize, list.Count - i));
    }
}