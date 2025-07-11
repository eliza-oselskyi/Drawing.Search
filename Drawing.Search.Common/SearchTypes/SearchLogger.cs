using System;
using System.Diagnostics;
using Drawing.Search.Common.Interfaces;

namespace Drawing.Search.Common.SearchTypes;

/// <summary>
///     A simple logger implementation used to log search-related information,
///     errors, and debug messages in the application.
/// </summary>
/// <remarks>
///     This logger uses both <see cref="Debug.WriteLine" /> and <see cref="Console.WriteLine" />
///     to log the messages, enabling logging in both development and runtime environments.
/// </remarks>
public class SearchLogger : ISearchLogger
{
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
        Debug.WriteLine($"INFO: {message}");
        Console.WriteLine($"INFO: {message}");
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
        Debug.WriteLine($"ERROR: {message}");
        Debug.WriteLine($"Exception: {exception}");
        Console.WriteLine($"ERROR: {message}");
        Console.WriteLine($"Exception: {exception}");
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
        Debug.WriteLine($"DEBUG: {message}");
    }
}