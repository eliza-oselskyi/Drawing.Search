using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Drawing.Search.Domain.Interfaces;

namespace Drawing.Search.Application.Features.Search;

/// <summary>
///     A simple logger implementation used to log search-related information,
///     errors, and debug messages in the application.
/// </summary>
public sealed class SearchLogger : ISearchLogger
{
    private readonly List<LogEntry> _logEntries = [];
    
    public IReadOnlyList<LogEntry> LogEntries => _logEntries;

    /// <summary>
    ///     Logs informational messages such as updates or general events.
    /// </summary>
    /// <param name="message">The informational message to log.</param>
    /// <example>
    ///     <code>
    /// var logger = new SearchLogger();
    /// logger.LogInformation("Search started");
    /// </code>
    /// </example>
    public void LogInformation(string message)
    {
        var compiled = $"INFO: {message}";
        var logEntry = new LogEntry(compiled, null);
        _logEntries.Add(logEntry);
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LogEntries)));
    }

    /// <summary>
    ///     Logs error messages along with optional exception details.
    /// </summary>
    /// <param name="exception">The exception that caused the error (optional).</param>
    /// <param name="message">The error message to log.</param>
    /// <example>
    ///     <code>
    /// var logger = new SearchLogger();
    /// try
    /// {
    ///     // Simulated operation
    ///     throw new InvalidOperationException("Simulated exception");
    /// }
    /// catch (Exception ex)
    /// {
    ///     logger.LogError(ex, "An error occurred during the search");
    /// }
    /// </code>
    /// </example>
    public void LogError(Exception exception, string message)
    {
        var logEntry = new LogEntry($"ERROR: {message}", exception);;
        _logEntries.Add(logEntry);
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LogEntries)));
    }

    /// <summary>
    ///     Logs debug-level messages primarily used for development and troubleshooting.
    /// </summary>
    /// <param name="message">The debug message to log.</param>
    /// <example>
    ///     <code>
    /// var logger = new SearchLogger();
    /// logger.DebugInfo("Error handling block executed");
    /// </code>
    /// </example>
    public void DebugInfo(string message)
    {
        var logEntry = new LogEntry($"DEBUG: {message}", null);
        _logEntries.Add(logEntry);
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LogEntries)));
    }

    public void ClearLog()
    {
        _logEntries.Clear();
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LogEntries)));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}