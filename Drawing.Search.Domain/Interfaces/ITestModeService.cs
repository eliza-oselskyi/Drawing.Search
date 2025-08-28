using System;

namespace Drawing.Search.Domain.Interfaces;

public interface ITestModeService
{
    public bool IsTestMode { get; }
    void SetTestMode(bool isTestMode);
    event EventHandler<bool> IsTestModeChanged;
}