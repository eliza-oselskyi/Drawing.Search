using System;
using System.Threading;
using Drawing.Search.Common.Interfaces;

namespace Drawing.Search.Common.Observers;

/// <summary>
///     An observer that tracks the caching process and updates its status message accordingly.
///     Note: This class is planned to be deprecated as the caching service will be converted
///     into a singleton and dependency injected, removing the need for a separate observer.
/// </summary>
public class CachingObserver : IObserver
{
    private readonly SynchronizationContext _synchronizationContext;

    /// <summary>
    ///     Initializes a new instance of the <see cref="CachingObserver" /> class with a specified synchronization context.
    /// </summary>
    /// <param name="synchronizationContext">
    ///     A <see cref="SynchronizationContext" /> for posting updates to the UI thread.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when the provided <paramref name="synchronizationContext" /> is <c>null</c>.
    /// </exception>
    public CachingObserver(SynchronizationContext synchronizationContext)
    {
        _synchronizationContext =
            synchronizationContext ?? throw new ArgumentNullException(nameof(synchronizationContext));
    }

    /// <summary>
    ///     Gets or sets a value indicating whether caching is currently in progress.
    /// </summary>
    private bool IsCaching { get; set; }

    /// <summary>
    ///     Gets the current status message for the caching process.
    /// </summary>
    public string StatusMessage { get; private set; } = "";

    /// <summary>
    ///     Invoked when a matching item is found during the caching process.
    ///     Updates the caching status and raises the <see cref="StatusMessageChanged" /> event.
    /// </summary>
    /// <param name="obj">
    ///     An object representing data about the match.
    ///     Expected to be of type <see cref="bool" />, representing whether caching is active.
    /// </param>
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

    /// <summary>
    ///     An event raised whenever the <see cref="StatusMessage" /> is updated.
    /// </summary>
    public event EventHandler<string>? StatusMessageChanged;
}