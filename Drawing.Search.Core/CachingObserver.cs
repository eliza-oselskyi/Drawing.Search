using System;
using Drawing.Search.Core.Interfaces;
using System.Threading;

namespace Drawing.Search.Core;

public class CachingObserver : IObserver
{
    private readonly SynchronizationContext _synchronizationContext;
    private bool IsCaching { get; set; } = false;
    public string StatusMessage { get; private set; } = "";
    
    public event EventHandler<string> StatusMessageChanged;

    public CachingObserver(SynchronizationContext synchronizationContext)
    {
        _synchronizationContext =
            synchronizationContext ?? throw new ArgumentNullException(nameof(synchronizationContext));
    }

    public void OnMatchFound(object obj)
    {
        if (obj is bool flag)
        {
            IsCaching = flag;
            StatusMessage = IsCaching ? "Caching updated drawing..." : "";
            // Post to the UI thread using SynchronizationContext
            _synchronizationContext.Post(_ =>
            {
                StatusMessageChanged?.Invoke(this, StatusMessage);
            }, null);
        }
    }
}