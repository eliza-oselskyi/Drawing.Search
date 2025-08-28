using System;
using Drawing.Search.Domain.Interfaces;

namespace Drawing.Search.Infrastructure;

public class TestModeService : ITestModeService
{
    public bool IsTestMode { get; private set; }
    
    public TestModeService()
    {
        var settings = SearchSettings.Load();
        IsTestMode = settings.IsTestMode;
        
        
    }
    public void SetTestMode(bool isTestMode)
    {
        var settings = SearchSettings.Load();
        settings.IsTestMode = isTestMode;
        settings.Save();
        IsTestMode = isTestMode;
        IsTestModeChanged?.Invoke(this, isTestMode);
    }

    public event EventHandler<bool>? IsTestModeChanged;
}