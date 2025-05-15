namespace Drawing.Search.Core.Interfaces;

public interface IObserverableSearch
{
    void Subscribe(IObserver observer);
    void Unsubscribe(IObserver observer);
}