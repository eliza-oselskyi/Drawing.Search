using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Drawing.Search.Caching;
using Drawing.Search.Caching.Interfaces;
using Drawing.Search.CADIntegration;
using Drawing.Search.CADIntegration.History;
using Drawing.Search.Common.Enums;
using Drawing.Search.Common.Interfaces;
using Drawing.Search.Common.Observers;
using Drawing.Search.Common.SearchTypes;
using Drawing.Search.Core.CacheService;
using Tekla.Structures.Drawing;
using Tekla.Structures.DrawingInternal;
using ModelObject = Tekla.Structures.Model.ModelObject;

namespace Drawing.Search.Core;

public sealed class SearchViewModel : INotifyPropertyChanged
{
    private const string MATCHED_CONTENT_CACHE_KEY = "matched_content";
    private readonly ICacheService _cacheService;
    private readonly Events _drawingEvents = new();

    private readonly HashSet<string> _previousSearches = new(StringComparer.OrdinalIgnoreCase);
    private readonly SearchDriver _searchDriver;
    private readonly SearchService.SearchService _searchService;
    private readonly Tekla.Structures.Drawing.UI.Events _uiEvents = new();
    private ContentCollectingObserver? _contentCollector;
    private string _ghostSuggestion = ""; // for autocomplete
    private bool _isCaching;
    private bool _isCaseSensitive;

    private bool _isDarkMode;
    private bool _isSearching;
    private string _searchTerm = "";
    private SearchType _selectedSearchType;
    private SearchSettings? _settings;

    private bool _showAllAssemblyParts;
    private string _statusMessage = "";
    private string _version = "";
    private DrawingHistory _drawingHistory = new();

    public EventHandler<bool>? QuitRequested;
    private AsyncRelayCommand _focusCommand;

    public SearchViewModel(SearchService.SearchService searchService, SearchDriver searchDriver,
        ICacheService cacheService)
    {
        _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
        _searchDriver = searchDriver ?? throw new ArgumentNullException(nameof(searchDriver));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));

        LoadSearchSettings();
        InitializeEvents();
        Task.Run(UiEventsOnDrawingLoaded); // Initial caching
        UpdateDrawingState();

        var uiContext = SynchronizationContext.Current ??
                        throw new InvalidOperationException("SearchViewModel must be created on UI thread.");
        _searchDriver.CacheObserver.StatusMessageChanged += (_, message) => { StatusMessage = message; };
        SearchCommand = new AsyncRelayCommand(
            ExecuteSearchAsync,
            CanExecuteSearch
        );
        _focusCommand = new AsyncRelayCommand(() =>
        {
            FocusRequested?.Invoke(this, EventArgs.Empty);
            return Task.CompletedTask;
        });
        Version = $"v{Assembly.GetExecutingAssembly().GetName().Version}" + "_TESTING";


        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SearchTerm)) UpdateGhostSuggestion(SearchTerm);
        };
    }

    private void UpdateDrawingState()
    {
        _drawingHistory.Save(new DrawingObjectsOriginator(DrawingHandler.Instance.GetActiveDrawing()).CreateDrawingState());
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

    public AsyncRelayCommand FocusCommand => _focusCommand;

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

    public bool IsDarkMode
    {
        get => _isDarkMode;
        set
        {
            if (_isDarkMode == value) return;
            _isDarkMode = value;
            if (_settings != null)
            {
                _settings.IsDarkMode = value;
                _settings.Save();
            }

            ApplyTheme(value);
            OnPropertyChanged(nameof(IsDarkMode));
        }
    }

    public bool UseRegexSearch
    {
        get => _settings is { WildcardSearch: false };
        set
        {
            if (_settings != null && _settings.WildcardSearch == !value) return;
            if (_settings != null)
            {
                _settings.WildcardSearch = !value;
                _settings.Save();
            }

            OnPropertyChanged(nameof(UseRegexSearch));
        }
    }

    public bool ShowAllAssemblyParts
    {
        get => _showAllAssemblyParts;
        set
        {
            if (_showAllAssemblyParts == value) return;
            _showAllAssemblyParts = value;
            if (_settings != null)
            {
                _settings.ShowAllAssemblyPositions = value;
                _settings.Save();
            }

            OnPropertyChanged(nameof(ShowAllAssemblyParts));
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    private void LoadSearchSettings()
    {
        _settings = SearchSettings.Load();
        _showAllAssemblyParts = _settings.ShowAllAssemblyPositions;
        _isDarkMode = _settings.IsDarkMode;

        ApplyTheme(_isDarkMode);
    }

    private static void ApplyTheme(bool isDark)
    {
        var theme = isDark ? "Dark" : "Light";

        // Find and remove only the theme dictionary
        var themeDict = Application.Current.Resources.MergedDictionaries
            .FirstOrDefault(d => d.Source?.ToString().Contains("/Themes/Dark.xaml") == true
                                 || d.Source?.ToString().Contains("/Themes/Light.xaml") == true);

        if (themeDict != null) Application.Current.Resources.MergedDictionaries.Remove(themeDict);

        // Add the new theme dictionary
        Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri($"/Drawing.Search;component/Themes/{theme}.xaml", UriKind.Relative)
        });
    }

    private void InitializeEvents()
    {
        _drawingEvents.DrawingChanged += OnDrawingModified;
        _drawingEvents.DrawingUpdated += OnDrawingUpdated;
        _uiEvents.DrawingLoaded += UiEventsOnDrawingLoaded;
        _uiEvents.DrawingEditorClosed += () => { QuitRequested?.Invoke(this, false); };
        _drawingEvents.Register();
        _uiEvents.Register();
        _cacheService.IsCachingChanged += (_, isCaching) => { IsCaching = isCaching; };
    }

    private void UiEventsOnDrawingLoaded()
    {
        var cancellationTokenSource = new CancellationTokenSource(2000);
        var cancellationToken = cancellationTokenSource.Token;
        IsCaching = ((TeklaCacheService)_cacheService).IsCaching;
        if (IsCaching)
        {
            cancellationTokenSource.Cancel();
            IsCaching = false;
            var dwgKey = new CacheKeyBuilder(DrawingHandler.Instance.GetActiveDrawing().GetIdentifier().ToString())
                .UseDrawingKey().AppendObjectId().Build();
            _cacheService.InvalidateCacheByKey(dwgKey);
        }

        try
        {
            if (!((TeklaCacheService)_cacheService).HasCachedBefore(DrawingHandler.Instance.GetActiveDrawing().GetIdentifier().ID.ToString()))
            {
                StatusMessage = StatusMessages.CACHE_NewDrawing;
            }
            else
            {
                StatusMessage = StatusMessages.CACHE_RefreshObjects;
            }
            _cacheService.RefreshCache(DrawingHandler.Instance.GetActiveDrawing(), _drawingHistory.ViewHasDifference, cancellationToken);
            StatusMessage = StatusMessages.READY;
        }
        catch (OperationCanceledException)
        {
            var message = StatusMessages.CACHE_Cancelled;
            Console.WriteLine(message);
            StatusMessage = message;
            UiEventsOnDrawingLoaded();
        }
    }

    // TODO: Figure out why IsCaching binding not working for search button. Should be disabled when caching is active.
    private void OnDrawingUpdated(Tekla.Structures.Drawing.Drawing drawing, Events.DrawingUpdateTypeEnum type)
    {
        UpdateDrawingState();
        if (!_drawingHistory.HasDifference && !_drawingHistory.ViewHasDifference) return;
        IsCaching = true;
        StatusMessage = StatusMessages.CACHE_RecacheOnModify;
        var dwgKey = new CacheKeyBuilder(drawing.GetIdentifier().ToString()).CreateDrawingCacheKey();
        _cacheService.InvalidateCacheByKey(dwgKey);
        ((TeklaCacheService)_cacheService).RefreshCache(dwgKey, drawing, _drawingHistory.ViewHasDifference, CancellationToken.None);
        StatusMessage = StatusMessages.READY;
        IsCaching = false;
    }

    private void OnDrawingModified()
    {
        UpdateDrawingState();
        if (!_drawingHistory.HasDifference && !_drawingHistory.ViewHasDifference) return;


        IsCaching = true;
        var dwgId = DrawingHandler.Instance.GetActiveDrawing().GetIdentifier().ToString();
        var dwgKey = new CacheKeyBuilder(dwgId).UseDrawingKey().AppendObjectId().Build();
        StatusMessage = StatusMessages.CACHE_RecacheOnModify;
        _cacheService.InvalidateCacheByKey(dwgKey);
        ((TeklaCacheService)_cacheService).RefreshCache(dwgKey, DrawingHandler.Instance.GetActiveDrawing(), _drawingHistory.ViewHasDifference, CancellationToken.None);
        StatusMessage = StatusMessages.READY;
        IsCaching = false;
    }

    public event EventHandler? SearchCompleted;
    public event EventHandler? FocusRequested;

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
            StatusMessage = StatusMessages.SEARCH_ERR_CannotSearchWhileCaching;
            return;
        }

        try
        {
            IsSearching = true;
            StatusMessage = StatusMessages.SEARCH_Searching;

            var stopwatch = Stopwatch.StartNew();

            _contentCollector = new ContentCollectingObserver(GetExtractor(SelectedSearchType));


            var result = await Task.Run(() =>
            {
                var config = CreateSearchConfiguration();

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

    private SearchConfiguration CreateSearchConfiguration()
    {
        var config = new SearchConfiguration
        {
            SearchTerm = SearchTerm,
            Type = SelectedSearchType,
            SearchStrategies = GetSearchStrategies(),
            Observer = _contentCollector,
            ShowAllAssemblyParts = ShowAllAssemblyParts,
            Wildcard = _settings is { WildcardSearch: true }
        };
        return config;
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
                _settings is { WildcardSearch: true }
                    ? new WildcardMatchStrategy<Mark>()
                    : new RegexMatchStrategy<Mark>()
            },
            SearchType.Text => new List<ISearchStrategy>
            {
                _settings is { WildcardSearch: true }
                    ? new WildcardMatchStrategy<Text>()
                    : new RegexMatchStrategy<Text>()
            },
            SearchType.Assembly => new List<ISearchStrategy>
            {
                _settings is { WildcardSearch: true }
                    ? new WildcardMatchStrategy<ModelObject>()
                    : new RegexMatchStrategy<ModelObject>()
            },
            _ => throw new ArgumentException($"Unsupported search type: {SelectedSearchType}")
        };
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

public class AsyncRelayCommand : ICommand
{
    private readonly Func<bool>? _canExecute;
    private readonly Func<Task> _execute;
    private bool _isExecuting;

    public AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
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