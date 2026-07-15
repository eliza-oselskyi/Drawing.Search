using System;
using System.Collections.Generic;
using System.Text;

namespace Drawing.Search.Domain.Interfaces;

public interface ISearchLogger
{
    IReadOnlyList<LogEntry> LogEntries { get; }
    void LogInformation(string message);
    void LogError(Exception exception, string message);
    void DebugInfo(string message);
}

public sealed record LogEntry(string Message, Exception? Exception)
{
    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine(Message);
        if (Exception != null) sb.AppendLine(Exception.GetType().Name + " | " + Exception.Message.ToString());
        return sb.ToString();
    }
}
