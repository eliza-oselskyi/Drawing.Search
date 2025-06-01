using System;
using System.Threading;
using Drawing.Search.Core.SearchService.Interfaces;

namespace Drawing.Search.Core;

public class CachingObserver : IObserver
{
    private readonly SynchronizationContext _synchronizationContext;

    public CachingObserver(SynchronizationContext synchronizationContext)
    {
        _synchronizationContext =
            synchronizationContext ?? throw new ArgumentNullException(nameof(synchronizationContext));
    }

    private bool IsCaching { get; set; }
    public string StatusMessage { get; private set; } = "";

    public void OnMatchFound(object obj)
    {
        if (obj is bool flag)
        {
            IsCaching = flag;
            StatusMessage = IsCaching ? "Caching updated drawing..." : "";
            // Post to the UI thread using SynchronizationContext
            _synchronizationContext.Post(_ => { StatusMessageChanged?.Invoke(this, StatusMessage); }, null);
        }
    }

    public event EventHandler<string> StatusMessageChanged;
}