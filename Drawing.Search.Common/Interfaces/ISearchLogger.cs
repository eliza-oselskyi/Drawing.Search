using System;

namespace Drawing.Search.Common.Interfaces;

public interface ISearchLogger
{
    void LogInformation(string message);
    void LogError(Exception exception, string message);
    void DebugInfo(string message);
}