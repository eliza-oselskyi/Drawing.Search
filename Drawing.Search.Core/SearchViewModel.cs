using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Drawing.Search.Core.CacheService;
using Drawing.Search.Core.CacheService.Interfaces;
using Drawing.Search.Core.SearchService;
using Drawing.Search.Core.SearchService.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Tekla.Structures.Drawing;
using Tekla.Structures.DrawingInternal;
using ModelObject = Tekla.Structures.Model.ModelObject;

namespace Drawing.Search.Core;

public class SearchViewModel : INotifyPropertyChanged
{
    private readonly SearchService.SearchService _searchService;
    private readonly SearchDriver _searchDriver;
    private readonly ICacheService _cacheService;
    
    private const string MATCHED_CONTENT_CACHE_KEY = "matched_content";
    private ContentCollectingObserver _contentCollector;
    private string _ghostSuggestion; // for autocomplete
    private bool _isCaching;
    private bool _isCaseSensitive;
    private bool _isSearching;

    private readonly HashSet<string> _previousSearches = new(StringComparer.OrdinalIgnoreCase);
    private string _searchTerm;
    private SearchType _selectedSearchType;
    private string _statusMessage;
    private string _version;
    private readonly Events _drawingEvents = new();
    private readonly Tekla.Structures.Drawing.UI.Events _uiEvents = new();

    public SearchViewModel(SearchService.SearchService searchService, SearchDriver searchDriver, ICacheService cacheService)
    {
        _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
        _searchDriver = searchDriver ?? throw new ArgumentNullException(nameof(searchDriver));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        
        _drawingEvents.DrawingChanged += OnDrawingModified;
        _drawingEvents.DrawingUpdated += OnDrawingUpdated;
        _uiEvents.DrawingLoaded += UiEventsOnDrawingLoaded;
        _drawingEvents.Register();
        _uiEvents.Register();
        _cacheService.IsCachingChanged += (_cacheService, isCaching) => { IsCaching = isCaching; };

        var uiContext = SynchronizationContext.Current ??
                        throw new InvalidOperationException("SearchViewModel must be created on UI thread.");
        _searchDriver.CacheObserver.StatusMessageChanged += (sender, message) => { StatusMessage = message; };
        SearchCommand = new AsyncRelayCommand(
            ExecuteSearchAsync,
            CanExecuteSearch
        );
        Version = $"v{Assembly.GetExecutingAssembly().GetName().Version}";


        PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(SearchTerm)) UpdateGhostSuggestion(SearchTerm);
        };
    }

    private void UiEventsOnDrawingLoaded()
    {
        var dwgKey = new CacheKeyBuilder(DrawingHandler.Instance.GetActiveDrawing()
                .GetIdentifier()
                .ToString()).UseDrawingKey()
            .AppendObjectId()
            .Build();

        var dwgKeys = ((TeklaCacheService)_cacheService).DumpIdentifiers();
        IsCaching = true;
        _cacheService.RefreshCache(dwgKey);
        IsCaching = false;
    }

    // TODO: Figure out why IsCaching binding not working for search button. Should be disabled when caching is active.
    private void OnDrawingUpdated(Tekla.Structures.Drawing.Drawing drawing, Events.DrawingUpdateTypeEnum type)
    {
        IsCaching = true;
        StatusMessage = "Caching drawing objects...";
        var dwgKey = new CacheKeyBuilder(drawing.GetIdentifier().ToString()).UseDrawingKey().AppendObjectId().Build();
        ((TeklaCacheService)_cacheService).RefreshCache(dwgKey, drawing);
        StatusMessage = "Ready";
        IsCaching = false;
    }

    private void OnDrawingModified()
    {
        IsCaching = true;
        StatusMessage = "Caching drawing objects...";
        var dwgId = DrawingHandler.Instance.GetActiveDrawing().GetIdentifier().ToString();
        var dwgKey = new CacheKeyBuilder(dwgId).UseDrawingKey().AppendObjectId().Build();
        ((TeklaCacheService)_cacheService).RefreshCache(dwgKey, DrawingHandler.Instance.GetActiveDrawing());
        StatusMessage = "Ready";
        IsCaching = false;
    }

    public string Version
    {
        get => _version;
        set
        {
            _version = value;
            OnPropertyChanged();
        }
    }

    public AsyncRelayCommand SearchCommand { get; }

    public string GhostSuggestion
    {
        get => _ghostSuggestion;
        set
        {
            _ghostSuggestion = value;
            OnPropertyChanged();
        }
    }

    public bool IsSearching
    {
        get => _isSearching;
        set
        {
            _isSearching = value;
            OnPropertyChanged();
        }
    }

    public bool IsCaching
    {
        get => _isCaching;
        set
        {
            _isCaching = value;
            OnPropertyChanged();
        }
    }

    public string SearchTerm
    {
        get => _searchTerm;
        set
        {
            _searchTerm = value;
            OnPropertyChanged();
            SearchCommand.RaiseCanExecuteChanged();
        }
    }

    public SearchType SelectedSearchType
    {
        get => _selectedSearchType;
        set
        {
            _selectedSearchType = value;
            OnPropertyChanged();
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            _statusMessage = value;
            OnPropertyChanged();
        }
    }

    public bool IsCaseSensitive
    {
        get => _isCaseSensitive;
        set
        {
            _isCaseSensitive = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    public event EventHandler SearchCompleted;

    private void UpdateGhostSuggestion(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            GhostSuggestion = "";
            return;
        }

        // TODO: Implement matched_content caching to work with new cache system
       // Debug.Assert(_cache != null, nameof(_cache) + " != null");
  //      _cache.TryGetValue(MATCHED_CONTENT_CACHE_KEY, out HashSet<string> cachedContent);
  var cachedContent = new HashSet<string>();
  cachedContent.Add("NOT IMPLEMENTED");

        cachedContent ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);


        var allSuggestions = _previousSearches.Union(cachedContent);

        var suggestion = allSuggestions
            .Where(s => s.StartsWith(input, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(s => _previousSearches.Contains(s)) // Prioritize previous searches
            .ThenByDescending(s => cachedContent.Contains(s)) // then cached content
            .ThenBy(s => s.Length) // Prefer shorter matches
            .FirstOrDefault();

        Console.WriteLine($"Input: {input}, Suggestion: {suggestion}"); // Add this line
        GhostSuggestion = suggestion ?? "";
    }

    private bool CanExecuteSearch()
    {
        return !string.IsNullOrEmpty(SearchTerm) && !IsSearching && !IsCaching;
    }

    private async Task ExecuteSearchAsync()
    {
        if (IsCaching)
        {
            StatusMessage = "Cannot search while caching is active.";
            return;
        }
        try
        {
            IsSearching = true;
            StatusMessage = "Searching...";

            var stopwatch = Stopwatch.StartNew();

            _contentCollector = new ContentCollectingObserver(GetExtractor(SelectedSearchType));


            var result = await Task.Run(() =>
            {
                var config = new SearchConfiguration
                {
                    SearchTerm = SearchTerm,
                    Type = SelectedSearchType,
                    SearchStrategies = GetSearchStrategies(),
                    Observer = _contentCollector
                };

                return _searchDriver.ExecuteSearch(config);
            });
            stopwatch.Stop();
            result.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            StatusMessage = $"Found {result.MatchCount} matches in {result.ElapsedMilliseconds} ms.";

            // After successful search, add to previous searches list
            if (result.MatchCount > 0)
            {
                if (!string.IsNullOrEmpty(SearchTerm)) _previousSearches.Add(SearchTerm);

                foreach (var content in _contentCollector.MatchedContent) _previousSearches.Add(content);
            }
        }
        catch (Exception e)
        {
            StatusMessage = $"Error: {e.Message}";
        }
        finally
        {
            IsSearching = false;
            SearchCompleted?.Invoke(this, EventArgs.Empty);
        }
    }

    private IDataExtractor GetExtractor(SearchType type)
    {
        return type switch
        {
            SearchType.PartMark => new MarkExtractor(),
            SearchType.Text => new TextExtractor(),
            SearchType.Assembly => new ModelObjectExtractor(),
            _ => throw new ArgumentException($"No extractor available for: {type}")
        };
    }

    private List<ISearchStrategy> GetSearchStrategies()
    {
        return SelectedSearchType switch
        {
            SearchType.PartMark => new List<ISearchStrategy>
            {
                new RegexMatchStrategy<Mark>()
            },
            SearchType.Text => new List<ISearchStrategy>
            {
                new RegexMatchStrategy<Text>()
            },
            SearchType.Assembly => new List<ISearchStrategy>
            {
                new RegexMatchStrategy<ModelObject>()
            },
            _ => throw new ArgumentException($"Unsupported search type: {SelectedSearchType}")
        };
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

public class AsyncRelayCommand : ICommand
{
    private readonly Func<bool> _canExecute;
    private readonly Func<Task> _execute;
    private bool _isExecuting;

    public AsyncRelayCommand(Func<Task> execute, Func<bool> canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public bool CanExecute(object parameter)
    {
        return !_isExecuting && (_canExecute?.Invoke() ?? true);
    }

    public async void Execute(object parameter)
    {
        if (_isExecuting) return;

        try
        {
            _isExecuting = true;
            RaiseCanExecuteChanged();
            await _execute();
        }
        finally
        {
            _isExecuting = false;
            RaiseCanExecuteChanged();
        }
    }

    public event EventHandler? CanExecuteChanged;

    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}