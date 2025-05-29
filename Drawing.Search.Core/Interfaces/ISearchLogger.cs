using System;

namespace Drawing.Search.Core.Interfaces;

public interface ISearchLogger
{
    void LogInformation(string message);
    void LogError(Exception exception, string message);
    void DebugInfo(string message);
}