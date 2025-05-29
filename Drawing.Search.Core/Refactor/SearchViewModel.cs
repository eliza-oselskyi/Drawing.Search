using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.Mime;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Drawing.Search.Core.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Tekla.Structures.Drawing;

namespace Drawing.Search.Core;

public class SearchViewModel : INotifyPropertyChanged
{
    private string _version;
    public string Version
    {
        get => _version;
        set
        {
            _version = value;
            OnPropertyChanged();
        }
    }
    public event PropertyChangedEventHandler PropertyChanged;
    public event EventHandler SearchCompleted;
    public AsyncRelayCommand SearchCommand { get; }

    private readonly SearchDriver _searchDriver;
    private string _searchTerm;
    private SearchType _selectedSearchType;
    private string _statusMessage;
    private bool _isCaseSensitive;
    private bool _isSearching;

    public SearchViewModel()
    {
        _searchDriver = new SearchDriver(new MemoryCache(new MemoryCacheOptions()));
        SearchCommand = new AsyncRelayCommand(
            execute: ExecuteSearchAsync,
            canExecute: CanExecuteSearch
        );
        Version = $"v{System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}";
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

    private bool CanExecuteSearch() => !string.IsNullOrEmpty(SearchTerm) && !IsSearching;

    private async Task ExecuteSearchAsync()
    {
        try
        {
            IsSearching = true;
            StatusMessage = "Searching...";

            var stopwatch = Stopwatch.StartNew();
            var result = await Task.Run(() =>
            {
                var config = new SearchConfiguration
                {
                    SearchTerm = SearchTerm,
                    Type = SelectedSearchType,
                    SearchStrategies = GetSearchStrategies(),
                };

                return _searchDriver.ExecuteSearch(config);

            });
            stopwatch.Stop();
            result.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
                StatusMessage = $"Found {result.MatchCount} matches in {result.ElapsedMilliseconds} ms."; 
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
                new RegexMatchStrategy<Tekla.Structures.Drawing.Text>()
            },
            SearchType.Assembly => new List<ISearchStrategy>
            {
                new RegexMatchStrategy<Tekla.Structures.Model.ModelObject>()
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
    private readonly Func<Task> _execute;
    private readonly Func<bool> _canExecute;
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
    
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}