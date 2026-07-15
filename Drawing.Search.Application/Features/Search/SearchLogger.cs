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
/// <remarks>
///     This logger uses both <see cref="Debug.WriteLine" /> and <see cref="Console.WriteLine" />
///     to log the messages, enabling logging in both development and runtime environments.
/// </remarks>
public sealed class SearchLogger : ISearchLogger
{
    public readonly List<LogEntry> LogEntriesInternal = [];
    
    public IReadOnlyList<LogEntry> LogEntries => LogEntriesInternal;

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
        LogEntriesInternal.Add(logEntry);
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LogEntries)));
        
        Debug.WriteLine(compiled);
        Console.WriteLine(compiled);
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
        LogEntriesInternal.Add(logEntry);
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LogEntries)));
        
        Debug.WriteLine(logEntry);
        Console.WriteLine(logEntry);
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
        LogEntriesInternal.Add(logEntry);
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LogEntries)));
        
        Debug.WriteLine(logEntry);
    }

    public void ClearLog()
    {
        LogEntriesInternal.Clear();
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LogEntries)));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}