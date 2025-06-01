namespace Drawing.Search.Core.SearchService.Interfaces;

/// <summary>
///     Declares methods for implementing observers into a search class.
/// </summary>
// TODO: Fix typo in 'Observable'
public interface IObserverableSearch
{
    void Subscribe(IObserver observer);
    void Unsubscribe(IObserver observer);
}