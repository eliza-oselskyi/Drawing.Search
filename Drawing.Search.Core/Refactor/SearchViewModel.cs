using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Drawing.Search.Core.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Tekla.Structures.Drawing;

namespace Drawing.Search.Core;

public class SearchViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;
    public RelayCommand SearchCommand { get; }

    private readonly SearchDriver _searchDriver;
    private string _searchTerm;
    private SearchType _selectedSearchType;
    private string _statusMessage;
    private bool _isCaseSensitive;

    public SearchViewModel()
    {
        _searchDriver = new SearchDriver(new MemoryCache(new MemoryCacheOptions()));
        SearchCommand = new RelayCommand(
            execute: ExecuteSearch,
            canExecute: CanExecuteSearch
        );
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

    private bool CanExecuteSearch() => !string.IsNullOrEmpty(SearchTerm);

    private void ExecuteSearch()
    {
        try
        {
            var config = new SearchConfiguration
            {
                SearchTerm = SearchTerm,
                Type = SelectedSearchType,
                SearchStrategies = GetSearchStrategies(),
            };
            
            var result = _searchDriver.ExecuteSearch(config);
            StatusMessage = $"Found {result.MatchCount} matches in {result.ElapsedMilliseconds} ms.";
        }
        catch (Exception e)
        {
            StatusMessage = $"Error: {e.Message}";
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

public class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool> _canExecute;
    
    public RelayCommand(Action execute, Func<bool> canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }
    public bool CanExecute(object parameter) => _canExecute?.Invoke() ?? true;

    public void Execute(object parameter) => _execute();

    public event EventHandler? CanExecuteChanged;
    
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}